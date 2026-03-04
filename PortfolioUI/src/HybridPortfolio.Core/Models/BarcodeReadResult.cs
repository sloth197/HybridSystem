namespace HybridPortfolio.Core.Models;

public sealed class BarcodeReadResult
{
    public bool Success { get; set; }
    public string Value { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
