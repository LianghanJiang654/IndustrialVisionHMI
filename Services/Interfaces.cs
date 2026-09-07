using FactorialApp.Models;

namespace FactorialApp.Services;

public interface ICameraService : IAsyncDisposable
{
    bool IsConnected { get; }
    string Name { get; }
    double ExposureUs { get; set; }
    double Gain { get; set; }
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<CameraFrame> CaptureAsync(CancellationToken ct = default);
    Task<bool> PingAsync(CancellationToken ct = default);
}

public interface IPlcService : IAsyncDisposable
{
    bool IsConnected { get; }
    string Name { get; }
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task WaitForStartAsync(CancellationToken ct);
    Task SetBusyAsync(bool value, CancellationToken ct);
    Task SetResultAsync(bool pass, CancellationToken ct);
    Task ResetCycleAsync(CancellationToken ct);
    Task<bool> HeartbeatAsync(CancellationToken ct = default);
}

public interface IVisionService
{
    Task<VisionResult> InspectAsync(CameraFrame frame, RecipeRecord recipe, CancellationToken ct);
    Task<bool> PingAsync(CancellationToken ct = default);
}
