using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Sim;

public sealed class BarcodeSimulator : IBarcodeReader
{
    private readonly Random _random = new();

    public BarcodeReadResult Read(VirtualObjectFrame frame)
    {
        var readFail = _random.NextDouble() < 0.08;
        if (readFail)
        {
            return new BarcodeReadResult
            {
                Success = false,
                Value = string.Empty,
                Confidence = NextDouble(0.20, 0.45)
            };
        }

        var misread = _random.NextDouble() < 0.05;
        var value = misread ? Corrupt(frame.BarcodeText) : frame.BarcodeText;

        return new BarcodeReadResult
        {
            Success = true,
            Value = value,
            Confidence = NextDouble(0.86, 0.99)
        };
    }

    private string Corrupt(string barcode)
    {
        if (barcode.Length < 3)
        {
            return barcode;
        }

        var index = _random.Next(1, barcode.Length - 1);
        return barcode[..index] + "?" + barcode[(index + 1)..];
    }

    private double NextDouble(double min, double max)
    {
        return min + ((_random.NextDouble()) * (max - min));
    }
}
