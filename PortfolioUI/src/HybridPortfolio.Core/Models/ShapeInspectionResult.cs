namespace HybridPortfolio.Core.Models;

public sealed class ShapeInspectionResult
{
    public bool IsDefect { get; set; }
    public string Label { get; set; } = "Normal";
    public double Score { get; set; }
}
