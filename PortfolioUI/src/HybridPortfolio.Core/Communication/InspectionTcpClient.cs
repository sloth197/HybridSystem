using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Communication;

public sealed class InspectionTcpClient
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string Host { get; }
    public int Port { get; }

    public InspectionTcpClient(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public async Task SendAsync(InspectionResult result, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            timestamp = result.CompletedAt,
            objectId = result.Frame.ObjectId,
            shapeKind = result.Frame.ShapeKind.ToString(),
            barcode = result.Barcode.Value,
            barcodeSuccess = result.Barcode.Success,
            shapeResult = result.Shape.Label,
            shapeScore = result.Shape.Score,
            final = result.FinalResult.ToString(),
            reason = result.Reason,
            plcSignal = result.PlcResultSignal
        };

        var jsonLine = JsonSerializer.Serialize(payload, _jsonOptions) + "\n";
        var bytes = Encoding.UTF8.GetBytes(jsonLine);

        using var client = new TcpClient();
        await client.ConnectAsync(Host, Port, cancellationToken);
        await using var stream = client.GetStream();
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
