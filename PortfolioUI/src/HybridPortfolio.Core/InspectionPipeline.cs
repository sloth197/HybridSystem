using HybridPortfolio.Core.Decision;
using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core;

public sealed class InspectionPipeline
{
    private readonly ICameraSimulator _camera;
    private readonly IBarcodeReader _barcodeReader;
    private readonly IShapeInspector _shapeInspector;
    private readonly IPlcStateMachine _plc;
    private readonly DecisionEngine _decisionEngine;

    public InspectionPipeline(
        ICameraSimulator camera,
        IBarcodeReader barcodeReader,
        IShapeInspector shapeInspector,
        IPlcStateMachine plc,
        DecisionEngine decisionEngine)
    {
        _camera = camera;
        _barcodeReader = barcodeReader;
        _shapeInspector = shapeInspector;
        _plc = plc;
        _decisionEngine = decisionEngine;
    }

    public async Task<InspectionResult> ProcessOneCycleAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.Now;

        _plc.TriggerInspect();
        await Task.Delay(220, cancellationToken);

        var frame = _camera.CaptureFrame();
        var barcode = _barcodeReader.Read(frame);
        await Task.Delay(120, cancellationToken);

        var shape = _shapeInspector.Inspect(frame);
        var decision = _decisionEngine.Decide(new DecisionInput(barcode, shape));

        var plcSignal = decision.FinalResult == InspectionFinalResult.Ok ? "OK" : "NG";
        _plc.SetResult(plcSignal);
        await Task.Delay(220, cancellationToken);

        _plc.ReturnToIdle();

        return new InspectionResult
        {
            StartedAt = startedAt,
            CompletedAt = DateTime.Now,
            Frame = frame,
            Barcode = barcode,
            Shape = shape,
            FinalResult = decision.FinalResult,
            Reason = decision.Reason,
            PlcResultSignal = plcSignal
        };
    }
}
