using HybridPortfolio.Core.Enums;

namespace HybridPortfolio.Core.Models;

public sealed class VirtualObjectFrame
{
    public string ObjectId { get; set; } = string.Empty;
    public ShapeKind ShapeKind { get; set; }
    public string BarcodeText { get; set; } = string.Empty;
    public bool HasDefect { get; set; }
    public string DefectType { get; set; } = "None";
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}
