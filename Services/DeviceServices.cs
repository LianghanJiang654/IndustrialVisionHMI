using System.Net.Sockets;
using System.Reflection;
using FactorialApp.Models;
using System.IO;

namespace FactorialApp.Services;

public sealed class SimulatedCameraService : ICameraService
{
    private readonly CameraConfig _cfg; private readonly Random _random = new();
    public bool IsConnected { get; private set; } public string Name => "SIM-CAMERA";
    public double ExposureUs { get; set; } public double Gain { get; set; }
    public SimulatedCameraService(CameraConfig cfg) { _cfg = cfg; ExposureUs = cfg.ExposureUs; Gain = cfg.Gain; }
    public async Task ConnectAsync(CancellationToken ct = default) { await Task.Delay(100, ct); IsConnected = true; }
    public Task DisconnectAsync(CancellationToken ct = default) { IsConnected = false; return Task.CompletedTask; }
    public async Task<CameraFrame> CaptureAsync(CancellationToken ct = default)
    {
        if (!IsConnected) throw new InvalidOperationException("CAMERA NOT CONNECTED");
        await Task.Delay(80, ct);
        // 640x480 synthetic part image for end-to-end simulation.
        byte[] jpg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYFBgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/2wBDAQICAgICAgUDAwUKBwYHCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgr/wAARCAHgAoADASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD+f+iiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAK/f7/gxj/wCbov8AuSf/AHP1+ANfv9/wYx/83Rf9yT/7n6AP3+ooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigD8gf+D1b/lFl4B/7OA0r/0x65X8wNf0/f8AB6t/yiy8A/8AZwGlf+mPXK/mBoAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAr9/v8Agxj/AObov+5J/wDc/X4A1+/3/BjH/wA3Rf8Ack/+5+gD9/qKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooA/IH/g9W/5RZeAf+zgNK/8ATHrlfzA1/T9/werf8osvAP8A2cBpX/pj1yv5gaACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAK/f7/gxj/5ui/7kn/3P1+ANfv9/wAGMf8AzdF/3JP/ALn6AP3+ooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigD8gf+D1b/AJRZeAf+zgNK/wDTHrlfzA1/T9/werf8osvAP/ZwGlf+mPXK/mBoAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAr9/v+DGP/m6L/uSf/c/X4A1+/wB/wYx/83Rf9yT/AO5+gD9/qKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooA/IH/AIPVv+UWXgH/ALOA0r/0x65X8wNf0/f8Hq3/ACiy8A/9nAaV/wCmPXK/mBoAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAr9/v+DGP/AJui/wC5J/8Ac/X4A1+/3/BjH/zdF/3JP/ufoA/f6iiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKAPyB/4PVv+UWXgH/s4DSv/THrlfzA1/T9/wAHq3/KLLwD/wBnAaV/6Y9cr+YGgAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACv3+/wCDGP8A5ui/7kn/ANz9fgDX7/f8GMf/ADdF/wByT/7n6AP3+ooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigD8gf+D1b/lFl4B/7OA0r/wBMeuV/MDX9P3/B6t/yiy8A/wDZwGlf+mPXK/mBoAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAr9/v+DGP/m6L/uSf/c/X4A1+/3/AAYx/wDN0X/ck/8AufoA/f6iiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKAPyB/4PVv8AlFl4B/7OA0r/ANMeuV/MDX9P3/B6t/yiy8A/9nAaV/6Y9cr+YGgAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACv3+/4MY/+bov+5J/9z9fgDX7/AH/BjH/zdF/3JP8A7n6AP3+ooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigD8gf8Ag9W/5RZeAf8As4DSv/THrlfzA1/T9/werf8AKLLwD/2cBpX/AKY9cr+YGgAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACv3+/4MY/8Am6L/ALkn/wBz9fgDX7/f8GMf/N0X/ck/+5+gD9/qKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooA/IH/g9W/5RZeAf+zgNK/9MeuV/MDX9P3/AAerf8osvAP/AGcBpX/pj1yv5gaACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAK/f7/AIMY/wDm6L/uSf8A3P1+ANfv9/wYx/8AN0X/AHJP/ufoA/f6iiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKAPyB/4PVv+UWXgH/s4DSv/AEx65X8wNf0/f8Hq3/KLLwD/ANnAaV/6Y9cr+YGgAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACvQPgX+1j+1P+y/8A2p/wzR+0t8QPh3/bnkf23/wgvjK+0j+0PJ8zyfP+yyx+b5fmy7d2dvmvjG458/ooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9//AOHsX/BU3/pJZ+0B/wCHk1z/AOSqP+HsX/BU3/pJZ+0B/wCHk1z/AOSq8AooA9Q+Nf7bv7aH7SnhW38C/tGftd/FDx/olpqCX9ro3jXx/qOq2sN0qSRrcJFdTOiyhJZUDgbgsjjOGOfL6KKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigD//2Q==");
        return new CameraFrame(jpg, DateTime.Now, ".jpg");
    }
    public Task<bool> PingAsync(CancellationToken ct = default) => Task.FromResult(IsConnected);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Runtime adapter: compiles without the Basler NuGet package. Install Basler pylon and point BaslerAssemblyPath to Basler.Pylon.dll.
// This adapter targets the common pylon 7 API; verify parameter names against the exact camera/SDK version before production deployment.
public sealed class BaslerCameraService : ICameraService
{
    private readonly CameraConfig _cfg; private Assembly? _asm; private dynamic? _camera;
    public bool IsConnected { get; private set; } public string Name { get; private set; } = "BASLER";
    public double ExposureUs { get; set; } public double Gain { get; set; }
    public BaslerCameraService(CameraConfig cfg) { _cfg = cfg; ExposureUs = cfg.ExposureUs; Gain = cfg.Gain; }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return Task.CompletedTask;
        if (!File.Exists(_cfg.BaslerAssemblyPath)) throw new FileNotFoundException("Basler.Pylon.dll not found", _cfg.BaslerAssemblyPath);
        _asm = Assembly.LoadFrom(_cfg.BaslerAssemblyPath);
        var finder = _asm.GetType("Basler.Pylon.CameraFinder") ?? throw new InvalidOperationException("CameraFinder type missing");
        var enumerate = finder.GetMethod("Enumerate", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("CameraFinder.Enumerate missing");
        var list = enumerate.Invoke(null, null) as System.Collections.IEnumerable ?? throw new InvalidOperationException("No Basler camera list");
        object? info = null; foreach (var item in list) { info = item; break; }
        if (info is null) throw new InvalidOperationException("No Basler camera detected");
        var cameraType = _asm.GetType("Basler.Pylon.Camera") ?? throw new InvalidOperationException("Camera type missing");
        _camera = Activator.CreateInstance(cameraType, info) ?? throw new InvalidOperationException("Cannot create Basler camera");
        _camera.Open();
        TrySetParameter("ExposureTime", ExposureUs);
        TrySetParameter("Gain", Gain);
        Name = Convert.ToString(_camera.CameraInfo["ModelName"]) ?? "BASLER";
        IsConnected = true;
        return Task.CompletedTask;
    }

    private void TrySetParameter(string key, double value)
    {
        try { dynamic p = _camera.Parameters[key]; p.SetValue(value); } catch { }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        try { _camera?.Close(); } catch { }
        _camera = null; IsConnected = false; return Task.CompletedTask;
    }

    public Task<CameraFrame> CaptureAsync(CancellationToken ct = default)
    {
        if (!IsConnected || _camera is null || _asm is null) throw new InvalidOperationException("CAMERA NOT CONNECTED");
        TrySetParameter("ExposureTime", ExposureUs); TrySetParameter("Gain", Gain);
        dynamic grabber = _camera.StreamGrabber;
        Type timeoutType = _asm.GetType("Basler.Pylon.TimeoutHandling")!;
        object throwException = Enum.Parse(timeoutType, "ThrowException");
        dynamic result = grabber.GrabOne(_cfg.TimeoutMs, throwException);
        try
        {
            if (!(bool)result.GrabSucceeded) throw new InvalidOperationException("Basler grab failed");
            string tmp = Path.Combine(Path.GetTempPath(), $"basler_{Guid.NewGuid():N}.jpg");
            var persistence = _asm.GetType("Basler.Pylon.ImagePersistence") ?? throw new InvalidOperationException("ImagePersistence missing");
            var formatType = _asm.GetType("Basler.Pylon.ImageFileFormat") ?? throw new InvalidOperationException("ImageFileFormat missing");
            object jpeg = Enum.Parse(formatType, "Jpeg");
            var save = persistence.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "Save" && m.GetParameters().Length == 3)
                       ?? throw new InvalidOperationException("ImagePersistence.Save missing");
            save.Invoke(null, new object[] { jpeg, tmp, result });
            byte[] bytes = File.ReadAllBytes(tmp); File.Delete(tmp);
            return Task.FromResult(new CameraFrame(bytes, DateTime.Now, ".jpg"));
        }
        finally { try { result.Dispose(); } catch { } }
    }

    public Task<bool> PingAsync(CancellationToken ct = default)
    {
        try { return Task.FromResult(IsConnected && _camera is not null && (bool)_camera.IsOpen); } catch { return Task.FromResult(false); }
    }
    public async ValueTask DisposeAsync() => await DisconnectAsync();
}

public sealed class SimulatedPlcService : IPlcService
{
    public bool IsConnected { get; private set; } public string Name => "SIM-PLC";
    public async Task ConnectAsync(CancellationToken ct = default) { await Task.Delay(80, ct); IsConnected = true; }
    public Task DisconnectAsync(CancellationToken ct = default) { IsConnected = false; return Task.CompletedTask; }
    public async Task WaitForStartAsync(CancellationToken ct) { if (!IsConnected) throw new InvalidOperationException("PLC NOT CONNECTED"); await Task.Delay(100, ct); }
    public Task SetBusyAsync(bool value, CancellationToken ct) => Task.CompletedTask;
    public Task SetResultAsync(bool pass, CancellationToken ct) => Task.CompletedTask;
    public Task ResetCycleAsync(CancellationToken ct) => Task.CompletedTask;
    public Task<bool> HeartbeatAsync(CancellationToken ct = default) => Task.FromResult(IsConnected);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class ModbusTcpPlcService : IPlcService
{
    private readonly PlcConfig _cfg; private TcpClient? _client; private NetworkStream? _stream; private ushort _tx;
    public bool IsConnected => _client?.Connected == true; public string Name => $"MODBUS {_cfg.Host}";
    public ModbusTcpPlcService(PlcConfig cfg) => _cfg = cfg;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return;
        _client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(_cfg.TimeoutMs);
        await _client.ConnectAsync(_cfg.Host, _cfg.Port, timeout.Token); _stream = _client.GetStream();
    }
    public Task DisconnectAsync(CancellationToken ct = default) { try { _stream?.Dispose(); _client?.Dispose(); } finally { _stream = null; _client = null; } return Task.CompletedTask; }

    public async Task WaitForStartAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (await ReadHoldingAsync(_cfg.StartRegister, ct) != 0) return;
            await Task.Delay(50, ct);
        }
    }
    public Task SetBusyAsync(bool value, CancellationToken ct) => WriteSingleAsync(_cfg.BusyRegister, (ushort)(value ? 1 : 0), ct);
    public async Task SetResultAsync(bool pass, CancellationToken ct)
    {
        await WriteSingleAsync(_cfg.ResultRegister, (ushort)(pass ? 1 : 2), ct);
        await WriteSingleAsync(_cfg.DoneRegister, 1, ct);
    }
    public async Task ResetCycleAsync(CancellationToken ct)
    {
        await WriteSingleAsync(_cfg.BusyRegister, 0, ct); await WriteSingleAsync(_cfg.DoneRegister, 0, ct); await WriteSingleAsync(_cfg.ResultRegister, 0, ct);
    }
    public async Task<bool> HeartbeatAsync(CancellationToken ct = default)
    {
        try { ushort v = await ReadHoldingAsync(_cfg.HeartbeatRegister, ct); await WriteSingleAsync(_cfg.HeartbeatRegister, (ushort)(v == ushort.MaxValue ? 0 : v + 1), ct); return true; } catch { return false; }
    }

    private async Task<ushort> ReadHoldingAsync(ushort address, CancellationToken ct)
    {
        await EnsureConnected(ct); ushort tx = ++_tx;
        byte[] req = { (byte)(tx >> 8), (byte)tx, 0,0, 0,6, _cfg.UnitId, 3, (byte)(address>>8), (byte)address, 0,1 };
        await _stream!.WriteAsync(req, ct); byte[] hdr = await ReadExact(9, ct);
        if (hdr[7] != 3 || hdr[8] != 2) throw new IOException("Invalid Modbus read response");
        byte[] data = await ReadExact(2, ct); return (ushort)((data[0] << 8) | data[1]);
    }
    private async Task WriteSingleAsync(ushort address, ushort value, CancellationToken ct)
    {
        await EnsureConnected(ct); ushort tx = ++_tx;
        byte[] req = { (byte)(tx >> 8), (byte)tx, 0,0, 0,6, _cfg.UnitId, 6, (byte)(address>>8), (byte)address, (byte)(value>>8), (byte)value };
        await _stream!.WriteAsync(req, ct); _ = await ReadExact(12, ct);
    }
    private async Task EnsureConnected(CancellationToken ct) { if (!IsConnected) await ConnectAsync(ct); }
    private async Task<byte[]> ReadExact(int count, CancellationToken ct)
    {
        byte[] b = new byte[count]; int o = 0; while (o < count) { int n = await _stream!.ReadAsync(b.AsMemory(o, count-o), ct); if (n == 0) throw new IOException("PLC connection closed"); o += n; } return b;
    }
    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
