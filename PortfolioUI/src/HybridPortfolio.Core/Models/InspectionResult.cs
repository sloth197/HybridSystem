using HybridPortfolio.Core.Enums;

namespace HybridPortfolio.Core.Models;

public sealed class InspectionResult
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public VirtualObjectFrame Frame { get; set; } = new();
    public BarcodeReadResult Barcode { get; set; } = new();
    public ShapeInspectionResult Shape { get; set; } = new();
    public InspectionFinalResult FinalResult { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PlcResultSignal { get; set; } = string.Empty;
}
