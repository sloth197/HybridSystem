using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Sim;

public sealed class CameraSimulator : ICameraSimulator
{
    private static readonly ShapeKind[] Shapes =
    {
        ShapeKind.SmallBox,
        ShapeKind.MediumBox,
        ShapeKind.LargeBox
    };

    private static readonly string[] DefectTypes =
    {
        "Scratch",
        "Crack",
        "Stain",
        "Missing"
    };

    private readonly Random _random = new();
    private int _sequence = 1;

    public VirtualObjectFrame CaptureFrame()
    {
        var shape = Shapes[_random.Next(Shapes.Length)];
        var hasDefect = _random.NextDouble() < 0.30;
        var defectType = hasDefect ? DefectTypes[_random.Next(DefectTypes.Length)] : "None";

        var barcodeFail = _random.NextDouble() < 0.15;
        var lotNumber = _random.Next(1000, 9999);
        var barcode = $"PRD-{DateTime.Now:yyMMdd}-{lotNumber:D4}-{(barcodeFail ? "FAIL" : "OK")}";

        var objectId = $"{DateTime.Now:HHmmss}-{_sequence:D4}";
        _sequence++;

        return new VirtualObjectFrame
        {
            ObjectId = objectId,
            ShapeKind = shape,
            BarcodeText = barcode,
            HasDefect = hasDefect,
            DefectType = defectType,
            CapturedAt = DateTime.Now
        };
    }
}
