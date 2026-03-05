using HybridPortfolio.Core.Enums;
using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;
using OpenCvSharp;

namespace HybridPortfolio.Vision;

public sealed class FolderImageCameraSimulator : ICameraSimulator
{
    private static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".bmp"];
    private static readonly string[] DefectWords = ["defect", "fail", "ng", "scratch", "crack", "stain", "missing"];

    private readonly string[] _files;
    private readonly Random _random = new();
    private int _cursor;
    private int _sequence = 1;

    public string RootDirectory { get; }
    public bool HasImages => _files.Length > 0;

    public FolderImageCameraSimulator(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        Directory.CreateDirectory(RootDirectory);

        _files = Directory
            .EnumerateFiles(RootDirectory, "*.*", SearchOption.AllDirectories)
            .Where(path => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path)
            .ToArray();
    }

    public VirtualObjectFrame CaptureFrame()
    {
        if (!HasImages)
        {
            return CaptureFallback();
        }

        var imagePath = _files[_cursor];
        _cursor = (_cursor + 1) % _files.Length;

        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        var defectType = ExtractDefectType(fileName);
        var hasDefect = !string.Equals(defectType, "None", StringComparison.OrdinalIgnoreCase);
        var barcodeSuffix = hasDefect ? "FAIL" : "OK";

        var shapeKind = InferShapeKind(imagePath);
        var objectId = $"V-{DateTime.Now:HHmmss}-{_sequence:D4}";
        _sequence++;

        return new VirtualObjectFrame
        {
            ObjectId = objectId,
            ShapeKind = shapeKind,
            BarcodeText = $"PRD-{DateTime.Now:yyMMdd}-{_random.Next(1000, 9999):D4}-{barcodeSuffix}",
            ImagePath = imagePath,
            HasDefect = hasDefect,
            DefectType = defectType,
            CapturedAt = DateTime.Now
        };
    }

    private VirtualObjectFrame CaptureFallback()
    {
        var shapeKinds = Enum.GetValues<ShapeKind>();
        var shapeKind = shapeKinds[_random.Next(shapeKinds.Length)];

        var hasDefect = _random.NextDouble() < 0.30;
        var defectType = hasDefect ? "Scratch" : "None";
        var barcodeSuffix = hasDefect ? "FAIL" : "OK";

        var objectId = $"V-{DateTime.Now:HHmmss}-{_sequence:D4}";
        _sequence++;

        return new VirtualObjectFrame
        {
            ObjectId = objectId,
            ShapeKind = shapeKind,
            BarcodeText = $"PRD-{DateTime.Now:yyMMdd}-{_random.Next(1000, 9999):D4}-{barcodeSuffix}",
            ImagePath = string.Empty,
            HasDefect = hasDefect,
            DefectType = defectType,
            CapturedAt = DateTime.Now
        };
    }

    private static ShapeKind InferShapeKind(string imagePath)
    {
        try
        {
            using var mat = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            if (mat.Empty())
            {
                return ShapeKind.MediumBox;
            }

            var area = mat.Width * mat.Height;
            if (area < 160_000)
            {
                return ShapeKind.SmallBox;
            }

            if (area < 640_000)
            {
                return ShapeKind.MediumBox;
            }

            return ShapeKind.LargeBox;
        }
        catch
        {
            return ShapeKind.MediumBox;
        }
    }

    private static string ExtractDefectType(string fileName)
    {
        foreach (var word in DefectWords)
        {
            if (fileName.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return char.ToUpper(word[0]) + word[1..].ToLowerInvariant();
            }
        }

        return "None";
    }
}
