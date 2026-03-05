using HybridPortfolio.Core.Models;
using Microsoft.Data.Sqlite;

namespace HybridPortfolio.Core.Reporting;

public sealed class InspectionSqliteWriter
{
    private readonly object _sync = new();

    public string DbPath { get; }

    public InspectionSqliteWriter(string dbPath)
    {
        DbPath = dbPath;
        var dir = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        EnsureSchema();
    }

    public void Append(InspectionResult result)
    {
        lock (_sync)
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO inspection_results
                (
                    started_at, completed_at, object_id, shape_kind, image_path,
                    ground_truth_defect, barcode_success, barcode_value, barcode_confidence,
                    shape_is_defect, shape_label, shape_score, final_result, reason, plc_signal
                )
                VALUES
                (
                    $startedAt, $completedAt, $objectId, $shapeKind, $imagePath,
                    $groundTruthDefect, $barcodeSuccess, $barcodeValue, $barcodeConfidence,
                    $shapeIsDefect, $shapeLabel, $shapeScore, $finalResult, $reason, $plcSignal
                )
                """;

            cmd.Parameters.AddWithValue("$startedAt", result.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$completedAt", result.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$objectId", result.Frame.ObjectId);
            cmd.Parameters.AddWithValue("$shapeKind", result.Frame.ShapeKind.ToString());
            cmd.Parameters.AddWithValue("$imagePath", result.Frame.ImagePath);
            cmd.Parameters.AddWithValue("$groundTruthDefect", result.Frame.HasDefect ? result.Frame.DefectType : "None");
            cmd.Parameters.AddWithValue("$barcodeSuccess", result.Barcode.Success ? 1 : 0);
            cmd.Parameters.AddWithValue("$barcodeValue", result.Barcode.Value);
            cmd.Parameters.AddWithValue("$barcodeConfidence", result.Barcode.Confidence);
            cmd.Parameters.AddWithValue("$shapeIsDefect", result.Shape.IsDefect ? 1 : 0);
            cmd.Parameters.AddWithValue("$shapeLabel", result.Shape.Label);
            cmd.Parameters.AddWithValue("$shapeScore", result.Shape.Score);
            cmd.Parameters.AddWithValue("$finalResult", result.FinalResult.ToString());
            cmd.Parameters.AddWithValue("$reason", result.Reason);
            cmd.Parameters.AddWithValue("$plcSignal", result.PlcResultSignal);
            _ = cmd.ExecuteNonQuery();
        }
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS inspection_results
            (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                started_at TEXT NOT NULL,
                completed_at TEXT NOT NULL,
                object_id TEXT NOT NULL,
                shape_kind TEXT NOT NULL,
                image_path TEXT,
                ground_truth_defect TEXT NOT NULL,
                barcode_success INTEGER NOT NULL,
                barcode_value TEXT,
                barcode_confidence REAL NOT NULL,
                shape_is_defect INTEGER NOT NULL,
                shape_label TEXT NOT NULL,
                shape_score REAL NOT NULL,
                final_result TEXT NOT NULL,
                reason TEXT NOT NULL,
                plc_signal TEXT NOT NULL
            );
            """;
        _ = cmd.ExecuteNonQuery();
    }
}
