using HybridPortfolio.Core.Interfaces;
using HybridPortfolio.Core.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace HybridPortfolio.Vision;

public sealed class OnnxOpenCvShapeInspector : IShapeInspector, IDisposable
{
    private static readonly string[] DefaultLabels = ["Normal", "Scratch", "Crack", "Stain", "Missing"];
    private static readonly string[] DefectKeywords = ["scratch", "crack", "stain", "missing", "defect", "ng", "fail"];

    private readonly InferenceSession? _session;
    private readonly string[] _labels;
    private readonly string? _inputName;
    private readonly string? _outputName;

    public OnnxOpenCvShapeInspector(string modelPath, string? labelsPath = null)
    {
        _labels = LoadLabels(labelsPath);

        try
        {
            if (File.Exists(modelPath))
            {
                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC
                };
                _session = new InferenceSession(modelPath, options);
                _inputName = _session.InputMetadata.Keys.FirstOrDefault();
                _outputName = _session.OutputMetadata.Keys.FirstOrDefault();
            }
        }
        catch
        {
            _session = null;
        }
    }

    public ShapeInspectionResult Inspect(VirtualObjectFrame frame)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(frame.ImagePath) && File.Exists(frame.ImagePath))
            {
                using var mat = Cv2.ImRead(frame.ImagePath, ImreadModes.Color);
                if (!mat.Empty())
                {
                    if (_session is not null && _inputName is not null)
                    {
                        var onnxResult = InspectWithOnnx(mat);
                        if (onnxResult is not null)
                        {
                            return onnxResult;
                        }
                    }

                    return InspectWithOpenCvHeuristic(mat);
                }
            }
        }
        catch
        {
            // Fallback to simulated truth below.
        }

        if (frame.HasDefect)
        {
            return new ShapeInspectionResult
            {
                IsDefect = true,
                Label = frame.DefectType,
                Score = 0.80
            };
        }

        return new ShapeInspectionResult
        {
            IsDefect = false,
            Label = "Normal",
            Score = 0.12
        };
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    private ShapeInspectionResult? InspectWithOnnx(Mat bgr)
    {
        if (_session is null || _inputName is null)
        {
            return null;
        }

        using var rgb = new Mat();
        Cv2.CvtColor(bgr, rgb, ColorConversionCodes.BGR2RGB);
        using var resized = new Mat();
        Cv2.Resize(rgb, resized, new Size(224, 224));

        var input = new float[1 * 3 * 224 * 224];
        for (var y = 0; y < 224; y++)
        {
            for (var x = 0; x < 224; x++)
            {
                var pixel = resized.At<Vec3b>(y, x);
                var baseIndex = y * 224 + x;
                input[0 * 224 * 224 + baseIndex] = pixel.Item0 / 255.0f;
                input[1 * 224 * 224 + baseIndex] = pixel.Item1 / 255.0f;
                input[2 * 224 * 224 + baseIndex] = pixel.Item2 / 255.0f;
            }
        }

        var tensor = new DenseTensor<float>(input, new[] { 1, 3, 224, 224 });
        var container = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor)
        };

        using var results = _session.Run(container);
        var output = _outputName is not null
            ? results.FirstOrDefault(x => x.Name == _outputName)?.AsEnumerable<float>().ToArray()
            : results.FirstOrDefault()?.AsEnumerable<float>().ToArray();

        if (output is null || output.Length == 0)
        {
            return null;
        }

        var probs = Softmax(output);
        var bestIndex = 0;
        var bestProb = probs[0];
        for (var i = 1; i < probs.Length; i++)
        {
            if (probs[i] > bestProb)
            {
                bestProb = probs[i];
                bestIndex = i;
            }
        }

        var label = bestIndex < _labels.Length ? _labels[bestIndex] : $"Class_{bestIndex}";
        var isDefect = IsDefectLabel(label);
        return new ShapeInspectionResult
        {
            IsDefect = isDefect,
            Label = label,
            Score = bestProb
        };
    }

    private static ShapeInspectionResult InspectWithOpenCvHeuristic(Mat bgr)
    {
        using var gray = new Mat();
        Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

        using var lap = new Mat();
        Cv2.Laplacian(gray, lap, MatType.CV_64F);
        Cv2.MeanStdDev(lap, out _, out var stddev);

        var textureScore = stddev.Val0;
        var defectScore = Math.Clamp(textureScore / 60.0, 0.0, 1.0);
        var isDefect = defectScore >= 0.55;
        return new ShapeInspectionResult
        {
            IsDefect = isDefect,
            Label = isDefect ? "SurfaceAnomaly" : "Normal",
            Score = defectScore
        };
    }

    private static float[] Softmax(float[] values)
    {
        var max = values.Max();
        var exps = new double[values.Length];
        var sum = 0.0;
        for (var i = 0; i < values.Length; i++)
        {
            exps[i] = Math.Exp(values[i] - max);
            sum += exps[i];
        }

        var result = new float[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = (float)(exps[i] / sum);
        }

        return result;
    }

    private static bool IsDefectLabel(string label)
    {
        return DefectKeywords.Any(x => label.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] LoadLabels(string? labelsPath)
    {
        if (!string.IsNullOrWhiteSpace(labelsPath) && File.Exists(labelsPath))
        {
            return File.ReadAllLines(labelsPath)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();
        }

        return DefaultLabels;
    }
}
