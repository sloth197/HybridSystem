using System.Globalization;
using System.Text.Json;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Reporting;

public sealed class InspectionReportWriter
{
    private readonly object _sync = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string BaseDirectory { get; }
    public string CsvPath { get; }
    public string JsonLinesPath { get; }

    public InspectionReportWriter(string? baseDirectory = null)
    {
        BaseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "reports")
            : baseDirectory;

        Directory.CreateDirectory(BaseDirectory);

        var stamp = DateTime.Now.ToString("yyyyMMdd");
        CsvPath = Path.Combine(BaseDirectory, $"inspection_{stamp}.csv");
        JsonLinesPath = Path.Combine(BaseDirectory, $"inspection_{stamp}.jsonl");

        EnsureCsvHeader();
    }

    public void Append(InspectionResult result)
    {
        var csvLine = string.Join(",",
            EscapeCsv(result.StartedAt.ToString("yyyy-MM-dd HH:mm:ss")),
            EscapeCsv(result.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss")),
            EscapeCsv(result.Frame.ObjectId),
            EscapeCsv(result.Frame.ShapeKind.ToString()),
            EscapeCsv(result.Frame.HasDefect ? result.Frame.DefectType : "None"),
            EscapeCsv(result.Barcode.Success ? "true" : "false"),
            EscapeCsv(result.Barcode.Value),
            EscapeCsv(result.Barcode.Confidence.ToString("F4", CultureInfo.InvariantCulture)),
            EscapeCsv(result.Shape.IsDefect ? "true" : "false"),
            EscapeCsv(result.Shape.Label),
            EscapeCsv(result.Shape.Score.ToString("F4", CultureInfo.InvariantCulture)),
            EscapeCsv(result.FinalResult.ToString()),
            EscapeCsv(result.Reason),
            EscapeCsv(result.PlcResultSignal));

        var jsonLine = JsonSerializer.Serialize(new
        {
            startedAt = result.StartedAt,
            completedAt = result.CompletedAt,
            objectId = result.Frame.ObjectId,
            shapeKind = result.Frame.ShapeKind.ToString(),
            groundTruthDefect = result.Frame.HasDefect ? result.Frame.DefectType : "None",
            barcode = new
            {
                success = result.Barcode.Success,
                value = result.Barcode.Value,
                confidence = result.Barcode.Confidence
            },
            shapeInspection = new
            {
                isDefect = result.Shape.IsDefect,
                label = result.Shape.Label,
                score = result.Shape.Score
            },
            finalResult = result.FinalResult.ToString(),
            reason = result.Reason,
            plcSignal = result.PlcResultSignal
        }, _jsonOptions);

        lock (_sync)
        {
            File.AppendAllText(CsvPath, csvLine + Environment.NewLine);
            File.AppendAllText(JsonLinesPath, jsonLine + Environment.NewLine);
        }
    }

    private void EnsureCsvHeader()
    {
        if (File.Exists(CsvPath) && new FileInfo(CsvPath).Length > 0)
        {
            return;
        }

        const string header =
            "started_at,completed_at,object_id,shape_kind,ground_truth_defect,barcode_success,barcode_value,barcode_confidence,shape_is_defect,shape_label,shape_score,final_result,reason,plc_signal";
        File.AppendAllText(CsvPath, header + Environment.NewLine);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
