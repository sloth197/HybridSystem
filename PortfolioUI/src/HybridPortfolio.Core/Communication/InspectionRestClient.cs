using System.Net.Http.Json;
using System.Text.Json;
using HybridPortfolio.Core.Models;

namespace HybridPortfolio.Core.Communication;

public sealed class InspectionRestClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Uri Endpoint { get; }

    public InspectionRestClient(string endpointUrl, TimeSpan? timeout = null)
    {
        Endpoint = new Uri(endpointUrl, UriKind.Absolute);
        _httpClient = new HttpClient
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(3)
        };
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

        using var response = await _httpClient.PostAsJsonAsync(
            Endpoint,
            payload,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
