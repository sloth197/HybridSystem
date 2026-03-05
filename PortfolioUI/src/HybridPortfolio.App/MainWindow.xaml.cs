using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HybridPortfolio.Core;
using HybridPortfolio.Core.Communication;
using HybridPortfolio.Core.Decision;
using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;
using HybridPortfolio.Core.Reporting;
using HybridPortfolio.Sim;
using HybridPortfolio.Vision;
using IOPath = System.IO.Path;

namespace HybridPortfolio.App;

public partial class MainWindow : Window
{
    private readonly PlcStateMachine _plc;
    private readonly string _projectRoot;
    private readonly InspectionReportWriter _reportWriter;
    private readonly InspectionSqliteWriter _sqliteWriter;
    private readonly InspectionRestClient? _restClient;
    private readonly InspectionTcpClient? _tcpClient;
    private readonly ObservableCollection<InspectionHistoryRow> _history = new();

    private InspectionPipeline _pipeline = null!;
    private IDisposable? _shapeInspectorDisposable;
    private CancellationTokenSource? _autoRunCts;
    private bool _isAutoRunning;
    private int _totalCount;
    private int _okCount;
    private int _ngCount;
    private int _barcodeFailCount;

    public MainWindow()
    {
        InitializeComponent();

        _projectRoot = IOPath.GetFullPath(IOPath.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));

        _plc = new PlcStateMachine();
        _plc.StateChanged += PlcOnStateChanged;

        var reportDirectory = IOPath.Combine(_projectRoot, "reports");
        _reportWriter = new InspectionReportWriter(reportDirectory);
        _sqliteWriter = new InspectionSqliteWriter(IOPath.Combine(reportDirectory, "inspection.db"));

        var restEndpoint = Environment.GetEnvironmentVariable("HP_REST_ENDPOINT");
        if (Uri.TryCreate(restEndpoint, UriKind.Absolute, out var _))
        {
            _restClient = new InspectionRestClient(restEndpoint!);
        }

        var tcpHost = Environment.GetEnvironmentVariable("HP_TCP_HOST");
        var tcpPortText = Environment.GetEnvironmentVariable("HP_TCP_PORT");
        if (!string.IsNullOrWhiteSpace(tcpHost) &&
            int.TryParse(tcpPortText, out var tcpPort) &&
            tcpPort is > 0 and <= 65535)
        {
            _tcpClient = new InspectionTcpClient(tcpHost, tcpPort);
        }

        TxtReportFiles.Text =
            $"CSV: {IOPath.GetFileName(_reportWriter.CsvPath)} | JSONL: {IOPath.GetFileName(_reportWriter.JsonLinesPath)} | DB: inspection.db";
        TxtReportFiles.ToolTip =
            $"{_reportWriter.CsvPath}{Environment.NewLine}{_reportWriter.JsonLinesPath}{Environment.NewLine}{_sqliteWriter.DbPath}";

        HistoryGrid.ItemsSource = _history;
        BuildPipeline("Sim");
        UpdatePlcState(_plc.CurrentState);
        RefreshStatsDisplay();
        UpdateControlState();
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoRunCts?.Cancel();
        _plc.StateChanged -= PlcOnStateChanged;
        _shapeInspectorDisposable?.Dispose();
        _restClient?.Dispose();
        base.OnClosed(e);
    }

    private void CmbRunMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (CmbRunMode.SelectedItem is not ComboBoxItem item ||
            item.Content is not string mode)
        {
            return;
        }

        BuildPipeline(mode);
    }

    private async void BtnRunOne_Click(object sender, RoutedEventArgs e)
    {
        await RunOneCycleAsync(CancellationToken.None);
    }

    private async void BtnAutoStart_Click(object sender, RoutedEventArgs e)
    {
        if (_isAutoRunning)
        {
            return;
        }

        _isAutoRunning = true;
        _autoRunCts = new CancellationTokenSource();
        UpdateControlState();

        try
        {
            while (!_autoRunCts.IsCancellationRequested)
            {
                await RunOneCycleAsync(_autoRunCts.Token);
                await Task.Delay(450, _autoRunCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Auto mode cancellation is expected.
        }
        finally
        {
            _isAutoRunning = false;
            UpdateControlState();
        }
    }

    private void BtnAutoStop_Click(object sender, RoutedEventArgs e)
    {
        _autoRunCts?.Cancel();
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        _history.Clear();
        _totalCount = 0;
        _okCount = 0;
        _ngCount = 0;
        _barcodeFailCount = 0;
        RefreshStatsDisplay();
    }

    private async Task RunOneCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pipeline.ProcessOneCycleAsync(cancellationToken);
            _reportWriter.Append(result);
            _sqliteWriter.Append(result);
            await TrySendExternalAsync(result, cancellationToken);

            RenderVirtualObject(result.Frame);
            UpdateResultPanel(result);
            UpdateStats(result);
            AddHistory(result);
        }
        catch (OperationCanceledException)
        {
            // Ignore manual stop.
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Cycle failed: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task TrySendExternalAsync(InspectionResult result, CancellationToken cancellationToken)
    {
        if (_restClient is not null)
        {
            try
            {
                await _restClient.SendAsync(result, cancellationToken);
            }
            catch
            {
                // Ignore external endpoint errors in local simulation mode.
            }
        }

        if (_tcpClient is not null)
        {
            try
            {
                await _tcpClient.SendAsync(result, cancellationToken);
            }
            catch
            {
                // Ignore external endpoint errors in local simulation mode.
            }
        }
    }

    private void BuildPipeline(string mode)
    {
        _shapeInspectorDisposable?.Dispose();
        _shapeInspectorDisposable = null;

        ICameraSimulator camera;
        IShapeInspector shapeInspector;
        var barcodeReader = new BarcodeSimulator();
        var decision = new DecisionEngine();

        if (string.Equals(mode, "Vision", StringComparison.OrdinalIgnoreCase))
        {
            var imageRoot = IOPath.Combine(_projectRoot, "assets", "vision-input");
            var modelPath = IOPath.Combine(_projectRoot, "models", "defect_mobilenetv2.onnx");
            var labelsPath = IOPath.Combine(_projectRoot, "models", "labels.txt");

            var folderCamera = new FolderImageCameraSimulator(imageRoot);
            camera = folderCamera.HasImages ? folderCamera : new CameraSimulator();

            var visionInspector = new OnnxOpenCvShapeInspector(modelPath, labelsPath);
            shapeInspector = visionInspector;
            _shapeInspectorDisposable = visionInspector;

            TxtReason.Text = folderCamera.HasImages
                ? "Vision mode active: folder images + OpenCV/ONNX."
                : "Vision mode active: image folder empty, camera fallback to simulation.";
        }
        else
        {
            camera = new CameraSimulator();
            shapeInspector = new ShapeInspector();
            TxtReason.Text = "Simulation mode active.";
        }

        _pipeline = new InspectionPipeline(camera, barcodeReader, shapeInspector, _plc, decision);
    }

    private void AddHistory(InspectionResult result)
    {
        _history.Insert(0, new InspectionHistoryRow
        {
            Time = result.CompletedAt.ToString("HH:mm:ss"),
            ObjectId = result.Frame.ObjectId,
            Shape = result.Frame.ShapeKind.ToString(),
            Barcode = result.Barcode.Success ? result.Barcode.Value : "(read fail)",
            ShapeResult = $"{result.Shape.Label} ({result.Shape.Score:F2})",
            Final = result.FinalResult == InspectionFinalResult.Ok ? "OK" : "NG",
            Reason = result.Reason
        });

        if (_history.Count > 200)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private void UpdateResultPanel(InspectionResult result)
    {
        TxtObjectId.Text = result.Frame.ObjectId;
        TxtShapeKind.Text = result.Frame.ShapeKind.ToString();
        TxtGroundTruth.Text = result.Frame.HasDefect ? $"Defect: {result.Frame.DefectType}" : "Normal";
        TxtCapturedAt.Text = result.Frame.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss");

        TxtBarcodeValue.Text = result.Barcode.Success ? result.Barcode.Value : "READ_FAIL";
        TxtBarcodeConfidence.Text = result.Barcode.Confidence.ToString("F2");

        TxtShapeResult.Text = result.Shape.IsDefect ? $"Defect ({result.Shape.Label})" : "Normal";
        TxtShapeScore.Text = result.Shape.Score.ToString("F2");

        var isOk = result.FinalResult == InspectionFinalResult.Ok;
        TxtFinalResult.Text = isOk ? "OK" : "NG";
        TxtFinalResult.Foreground = isOk
            ? new SolidColorBrush(Color.FromRgb(22, 101, 52))
            : new SolidColorBrush(Color.FromRgb(153, 27, 27));
        TxtReason.Text = result.Reason;

        FinalResultCard.Background = isOk
            ? new SolidColorBrush(Color.FromRgb(220, 252, 231))
            : new SolidColorBrush(Color.FromRgb(254, 226, 226));
        FinalResultCard.BorderBrush = isOk
            ? new SolidColorBrush(Color.FromRgb(134, 239, 172))
            : new SolidColorBrush(Color.FromRgb(252, 165, 165));
    }

    private void RenderVirtualObject(VirtualObjectFrame frame)
    {
        ObjectCanvas.Children.Clear();

        var canvasWidth = ObjectCanvas.Width;
        var canvasHeight = ObjectCanvas.Height;

        var background = new Rectangle
        {
            Width = canvasWidth,
            Height = canvasHeight,
            Fill = new SolidColorBrush(Color.FromRgb(249, 250, 251))
        };
        ObjectCanvas.Children.Add(background);

        var boxBounds = DrawBox(frame.ShapeKind);

        if (frame.HasDefect)
        {
            DrawDefect(frame.DefectType, boxBounds);
        }
    }

    private Rect DrawBox(ShapeKind kind)
    {
        double width;
        double height;

        switch (kind)
        {
            case ShapeKind.SmallBox:
                width = 140;
                height = 90;
                break;
            case ShapeKind.MediumBox:
                width = 210;
                height = 130;
                break;
            case ShapeKind.LargeBox:
                width = 280;
                height = 170;
                break;
            default:
                width = 210;
                height = 130;
                break;
        }

        var left = (ObjectCanvas.Width - width) / 2.0;
        var top = (ObjectCanvas.Height - height) / 2.0;

        var box = new Rectangle
        {
            Width = width,
            Height = height,
            RadiusX = 8,
            RadiusY = 8,
            Fill = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
            Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
            StrokeThickness = 3
        };
        Canvas.SetLeft(box, left);
        Canvas.SetTop(box, top);
        ObjectCanvas.Children.Add(box);
        return new Rect(left, top, width, height);
    }

    private void DrawDefect(string defectType, Rect boxBounds)
    {
        var marginX = Math.Max(8.0, boxBounds.Width * 0.2);
        var marginY = Math.Max(8.0, boxBounds.Height * 0.2);

        var x1 = boxBounds.Left + marginX;
        var y1 = boxBounds.Top + marginY;
        var x2 = boxBounds.Right - marginX;
        var y2 = boxBounds.Bottom - marginY;

        var lineA = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            StrokeThickness = 4
        };
        var lineB = new Line
        {
            X1 = x2,
            Y1 = y1,
            X2 = x1,
            Y2 = y2,
            Stroke = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            StrokeThickness = 4
        };

        ObjectCanvas.Children.Add(lineA);
        ObjectCanvas.Children.Add(lineB);

        var label = new TextBlock
        {
            Text = defectType,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
            Background = new SolidColorBrush(Color.FromArgb(180, 254, 242, 242)),
            Padding = new Thickness(6, 2, 6, 2)
        };
        Canvas.SetLeft(label, boxBounds.Left + 8);
        Canvas.SetTop(label, boxBounds.Bottom + 6);
        ObjectCanvas.Children.Add(label);
    }

    private void PlcOnStateChanged(PlcState state)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => UpdatePlcState(state));
            return;
        }

        UpdatePlcState(state);
    }

    private void UpdatePlcState(PlcState state)
    {
        TxtPlcState.Text = state.ToString().ToUpperInvariant();

        switch (state)
        {
            case PlcState.Idle:
                PlcStateBadge.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                TxtPlcState.Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                break;
            case PlcState.Inspect:
                PlcStateBadge.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254));
                TxtPlcState.Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175));
                break;
            case PlcState.Result:
                PlcStateBadge.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                TxtPlcState.Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14));
                break;
        }
    }

    private void UpdateControlState()
    {
        BtnRunOne.IsEnabled = !_isAutoRunning;
        BtnAutoStart.IsEnabled = !_isAutoRunning;
        BtnAutoStop.IsEnabled = _isAutoRunning;
    }

    private void UpdateStats(InspectionResult result)
    {
        _totalCount++;

        if (result.FinalResult == InspectionFinalResult.Ok)
        {
            _okCount++;
        }
        else
        {
            _ngCount++;
        }

        if (!result.Barcode.Success)
        {
            _barcodeFailCount++;
        }

        RefreshStatsDisplay();
    }

    private void RefreshStatsDisplay()
    {
        TxtTotalCount.Text = _totalCount.ToString();

        if (_totalCount == 0)
        {
            TxtOkRate.Text = "0.0%";
            TxtNgRate.Text = "0.0%";
            TxtBarcodeFailRate.Text = "0.0%";
            return;
        }

        var okRate = (100.0 * _okCount) / _totalCount;
        var ngRate = (100.0 * _ngCount) / _totalCount;
        var barcodeFailRate = (100.0 * _barcodeFailCount) / _totalCount;

        TxtOkRate.Text = $"{okRate:F1}%";
        TxtNgRate.Text = $"{ngRate:F1}%";
        TxtBarcodeFailRate.Text = $"{barcodeFailRate:F1}%";
    }
}
