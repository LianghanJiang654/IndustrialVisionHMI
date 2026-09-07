using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FactorialApp.Models;
using System.IO;

namespace FactorialApp.Services;

public sealed class TcpVisionService : IVisionService
{
    private readonly VisionConfig _cfg;
    public TcpVisionService(VisionConfig cfg) => _cfg = cfg;

    public async Task<VisionResult> InspectAsync(CameraFrame frame, RecipeRecord recipe, CancellationToken ct)
    {
        var request = new
        {
            command = "inspect",
            image_base64 = Convert.ToBase64String(frame.Bytes),
            recipe = new
            {
                threshold_mode = recipe.ThresholdMode,
                threshold = recipe.Threshold,
                min_area = recipe.MinArea,
                max_area = recipe.MaxArea,
                position_tolerance = recipe.PositionTolerance,
                angle_tolerance = recipe.AngleTolerance,
                area_tolerance_percent = recipe.AreaTolerancePercent
            }
        };
        string json = JsonSerializer.Serialize(request) + "\n";
        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(_cfg.TimeoutMs);
        await client.ConnectAsync(_cfg.Host, _cfg.Port, timeout.Token);
        using NetworkStream stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(json), timeout.Token);
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: true);
        string? line = await reader.ReadLineAsync(timeout.Token);
        if (string.IsNullOrWhiteSpace(line)) throw new IOException("Vision server returned no data");
        return JsonSerializer.Deserialize<VisionResult>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new IOException("Invalid vision JSON");
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();

            using var timeout =
                CancellationTokenSource.CreateLinkedTokenSource(ct);

            timeout.CancelAfter(1000);

            await client.ConnectAsync(
                _cfg.Host,
                _cfg.Port,
                timeout.Token);

            using NetworkStream stream = client.GetStream();

            byte[] request =
                Encoding.UTF8.GetBytes("{\"command\":\"ping\"}\n");

            await stream.WriteAsync(request, timeout.Token);

            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                false,
                4096,
                leaveOpen: true);

            string? response =
                await reader.ReadLineAsync(timeout.Token);

            if (string.IsNullOrWhiteSpace(response))
                return false;

            using JsonDocument doc =
                JsonDocument.Parse(response);

            return doc.RootElement.TryGetProperty(
                       "success",
                       out var success)
                   && success.GetBoolean();
        }
        catch
        {
            return false;
        }
    }
}
