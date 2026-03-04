using HybridPortfolio.Core;
using HybridPortfolio.Core.Decision;
using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Tests;

public sealed class InspectionPipelineTests
{
    [Fact]
    public async Task ProcessOneCycle_AllChecksPass_ReturnsOkAndIdle()
    {
        var frame = CreateFrame("PRD-240101-1000-OK");
        var barcode = new BarcodeReadResult { Success = true, Value = frame.BarcodeText, Confidence = 0.98 };
        var shape = new ShapeInspectionResult { IsDefect = false, Label = "Normal", Score = 0.10 };
        var pipeline = CreatePipeline(frame, barcode, shape, out var plc);

        var result = await pipeline.ProcessOneCycleAsync();

        Assert.Equal(InspectionFinalResult.Ok, result.FinalResult);
        Assert.Equal("OK", result.PlcResultSignal);
        Assert.Equal(PlcState.Idle, plc.CurrentState);
        Assert.True(result.CompletedAt >= result.StartedAt);
    }

    [Fact]
    public async Task ProcessOneCycle_BarcodeReadFail_ReturnsNgAndNgSignal()
    {
        var frame = CreateFrame("PRD-240101-1000-OK");
        var barcode = new BarcodeReadResult { Success = false, Value = string.Empty, Confidence = 0.30 };
        var shape = new ShapeInspectionResult { IsDefect = false, Label = "Normal", Score = 0.10 };
        var pipeline = CreatePipeline(frame, barcode, shape, out var plc);

        var result = await pipeline.ProcessOneCycleAsync();

        Assert.Equal(InspectionFinalResult.Ng, result.FinalResult);
        Assert.Equal("NG", result.PlcResultSignal);
        Assert.Equal("NG", plc.LastResultSignal);
    }

    [Fact]
    public async Task ProcessOneCycle_FailBarcode_ReturnsNg()
    {
        var frame = CreateFrame("PRD-240101-1000-FAIL");
        var barcode = new BarcodeReadResult { Success = true, Value = frame.BarcodeText, Confidence = 0.97 };
        var shape = new ShapeInspectionResult { IsDefect = false, Label = "Normal", Score = 0.10 };
        var pipeline = CreatePipeline(frame, barcode, shape, out _);

        var result = await pipeline.ProcessOneCycleAsync();

        Assert.Equal(InspectionFinalResult.Ng, result.FinalResult);
        Assert.Contains("FAIL", result.Reason);
    }

    [Fact]
    public async Task ProcessOneCycle_TracksPlcTransitionsInOrder()
    {
        var frame = CreateFrame("PRD-240101-1000-OK");
        var barcode = new BarcodeReadResult { Success = true, Value = frame.BarcodeText, Confidence = 0.95 };
        var shape = new ShapeInspectionResult { IsDefect = false, Label = "Normal", Score = 0.13 };
        var pipeline = CreatePipeline(frame, barcode, shape, out var plc);

        _ = await pipeline.ProcessOneCycleAsync();

        Assert.Equal(3, plc.TransitionHistory.Count);
        Assert.Equal(PlcState.Inspect, plc.TransitionHistory[0]);
        Assert.Equal(PlcState.Result, plc.TransitionHistory[1]);
        Assert.Equal(PlcState.Idle, plc.TransitionHistory[2]);
    }

    [Fact]
    public async Task ProcessOneCycle_WithCanceledToken_ThrowsOperationCanceled()
    {
        var frame = CreateFrame("PRD-240101-1000-OK");
        var barcode = new BarcodeReadResult { Success = true, Value = frame.BarcodeText, Confidence = 0.95 };
        var shape = new ShapeInspectionResult { IsDefect = false, Label = "Normal", Score = 0.13 };
        var pipeline = CreatePipeline(frame, barcode, shape, out _);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipeline.ProcessOneCycleAsync(cts.Token));
    }

    private static InspectionPipeline CreatePipeline(
        VirtualObjectFrame frame,
        BarcodeReadResult barcode,
        ShapeInspectionResult shape,
        out RecordingPlc plc)
    {
        var camera = new FixedCamera(frame);
        var barcodeReader = new FixedBarcodeReader(barcode);
        var shapeInspector = new FixedShapeInspector(shape);
        plc = new RecordingPlc();
        var decision = new DecisionEngine();
        return new InspectionPipeline(camera, barcodeReader, shapeInspector, plc, decision);
    }

    private static VirtualObjectFrame CreateFrame(string barcode)
    {
        return new VirtualObjectFrame
        {
            ObjectId = "T-0001",
            ShapeKind = ShapeKind.MediumBox,
            BarcodeText = barcode,
            HasDefect = false,
            DefectType = "None",
            CapturedAt = DateTime.Now
        };
    }

    private sealed class FixedCamera : ICameraSimulator
    {
        private readonly VirtualObjectFrame _frame;

        public FixedCamera(VirtualObjectFrame frame)
        {
            _frame = frame;
        }

        public VirtualObjectFrame CaptureFrame()
        {
            return _frame;
        }
    }

    private sealed class FixedBarcodeReader : IBarcodeReader
    {
        private readonly BarcodeReadResult _result;

        public FixedBarcodeReader(BarcodeReadResult result)
        {
            _result = result;
        }

        public BarcodeReadResult Read(VirtualObjectFrame frame)
        {
            return _result;
        }
    }

    private sealed class FixedShapeInspector : IShapeInspector
    {
        private readonly ShapeInspectionResult _result;

        public FixedShapeInspector(ShapeInspectionResult result)
        {
            _result = result;
        }

        public ShapeInspectionResult Inspect(VirtualObjectFrame frame)
        {
            return _result;
        }
    }

    public sealed class RecordingPlc : IPlcStateMachine
    {
        public PlcState CurrentState { get; private set; } = PlcState.Idle;
        public string LastResultSignal { get; private set; } = string.Empty;
        public List<PlcState> TransitionHistory { get; } = new();
        public event Action<PlcState>? StateChanged;

        public void TriggerInspect()
        {
            TransitionTo(PlcState.Inspect);
        }

        public void SetResult(string resultSignal)
        {
            LastResultSignal = resultSignal;
            TransitionTo(PlcState.Result);
        }

        public void ReturnToIdle()
        {
            TransitionTo(PlcState.Idle);
        }

        private void TransitionTo(PlcState state)
        {
            CurrentState = state;
            TransitionHistory.Add(state);
            StateChanged?.Invoke(state);
        }
    }
}
