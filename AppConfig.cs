using System.Text.Json;
using System.IO;
namespace FactorialApp;

public sealed class AppConfig
{
    public bool UseSimulator { get; set; } = true;
    public string DatabasePath { get; set; } = @"Data\industrial_vision.db";
    public string ImageArchivePath { get; set; } = "InspectionImages";
    public CameraConfig Camera { get; set; } = new();
    public PlcConfig Plc { get; set; } = new();
    public VisionConfig Vision { get; set; } = new();
    public CycleConfig Cycle { get; set; } = new();

    public static AppConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return new AppConfig();
        return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppConfig();
    }
}

public sealed class CameraConfig
{
    public string Provider { get; set; } = "Simulated";
    public string BaslerAssemblyPath { get; set; } = "";
    public double ExposureUs { get; set; } = 5000;
    public double Gain { get; set; } = 1;
    public int TimeoutMs { get; set; } = 2500;
}
public sealed class PlcConfig
{
    public string Provider { get; set; } = "Simulated";
    public string Host { get; set; } = "192.168.0.10";
    public int Port { get; set; } = 502;
    public byte UnitId { get; set; } = 1;
    public ushort StartRegister { get; set; } = 0;
    public ushort BusyRegister { get; set; } = 1;
    public ushort DoneRegister { get; set; } = 2;
    public ushort ResultRegister { get; set; } = 3;
    public ushort HeartbeatRegister { get; set; } = 4;
    public int TimeoutMs { get; set; } = 2000;
}
public sealed class VisionConfig { public string Host { get; set; } = "127.0.0.1"; public int Port { get; set; } = 5001; public int TimeoutMs { get; set; } = 4000; }
public sealed class CycleConfig { public int MaxRetries { get; set; } = 2; public int RetryDelayMs { get; set; } = 300; public int InterCycleDelayMs { get; set; } = 500; }
