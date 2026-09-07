using FactorialApp.Core;

namespace FactorialApp.Models;

public enum UserRole { Operator = 0, Engineer = 1, Admin = 2 }
public enum MachineState { Offline, Idle, WaitingPart, TriggerCamera, Acquiring, Inspecting, SendingResult, Completed, Alarm, Stopping }

public sealed class UserAccount { public int Id { get; set; } public string Username { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string Salt { get; set; } = ""; public UserRole Role { get; set; } }

public sealed class RecipeRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "DEFAULT";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string ProductCode { get; set; } = "PRODUCT-A";
    public int Threshold { get; set; } = 128;
    public double MinArea { get; set; } = 1000;
    public double MaxArea { get; set; } = 500000;
    public double PositionTolerance { get; set; } = 5;
    public double AngleTolerance { get; set; } = 3;
    public double AreaTolerancePercent { get; set; } = 10;
    public double ExposureUs { get; set; } = 5000;
    public double Gain { get; set; } = 1;
    public string ThresholdMode { get; set; } = "fixed";
}

public sealed class VisionMark { public double X { get; set; } public double Y { get; set; } public double Angle { get; set; } public double Area { get; set; } }
public sealed class VisionResult
{
    public bool Success { get; set; }
    public bool InspectionPass { get; set; }
    public int Count { get; set; }
    public string Message { get; set; } = "";
    public string NgCode { get; set; } = "";
    public string AnnotatedImageBase64 { get; set; } = "";
    public List<VisionMark> Marks { get; set; } = new();
}

public sealed class InspectionRecord
{
    public long Id { get; set; }
    public string SerialNumber { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string ProductCode { get; set; } = "";
    public string RecipeName { get; set; } = "";
    public int RecipeVersion { get; set; }
    public bool Passed { get; set; }
    public string NgCode { get; set; } = "";
    public string NgReason { get; set; } = "";
    public double X { get; set; } public double Y { get; set; } public double Angle { get; set; } public double Area { get; set; }
    public double ExposureUs { get; set; } public double Gain { get; set; }
    public string Operator { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string RawImagePath { get; set; } = "";
    public long CycleId { get; set; }
}

public sealed class AlarmItem : ObservableObject
{
    private bool _acknowledged;
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsAcknowledged { get => _acknowledged; set => Set(ref _acknowledged, value); }
    public string Source { get; set; } = "SYSTEM";
}

public sealed class HistoryFilter
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Product { get; set; } = "";
    public string Recipe { get; set; } = "";
    public string Result { get; set; } = "ALL";
    public string NgCode { get; set; } = "";
    public string SerialNumber { get; set; } = "";
}

public sealed class ProductionStats { public long Total { get; set; } public long Pass { get; set; } public long Ng { get; set; } public double Yield => Total == 0 ? 0 : Pass * 100.0 / Total; }
public sealed record CameraFrame(byte[] Bytes, DateTime CapturedAt, string Extension = ".jpg");
