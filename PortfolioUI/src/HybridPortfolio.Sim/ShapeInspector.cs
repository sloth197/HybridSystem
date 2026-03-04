using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Sim;

public sealed class ShapeInspector : IShapeInspector
{
    private static readonly string[] DefectLabels =
    {
        "Scratch",
        "Crack",
        "Stain",
        "Missing"
    };

    private readonly Random _random = new();

    public ShapeInspectionResult Inspect(VirtualObjectFrame frame)
    {
        if (frame.HasDefect)
        {
            var detected = _random.NextDouble() < 0.90;
            if (detected)
            {
                return new ShapeInspectionResult
                {
                    IsDefect = true,
                    Label = frame.DefectType,
                    Score = NextDouble(0.65, 0.98)
                };
            }

            return new ShapeInspectionResult
            {
                IsDefect = false,
                Label = "Normal",
                Score = NextDouble(0.32, 0.52)
            };
        }

        var falsePositive = _random.NextDouble() < 0.08;
        if (falsePositive)
        {
            return new ShapeInspectionResult
            {
                IsDefect = true,
                Label = DefectLabels[_random.Next(DefectLabels.Length)],
                Score = NextDouble(0.55, 0.74)
            };
        }

        return new ShapeInspectionResult
        {
            IsDefect = false,
            Label = "Normal",
            Score = NextDouble(0.03, 0.34)
        };
    }

    private double NextDouble(double min, double max)
    {
        return min + (_random.NextDouble() * (max - min));
    }
}
