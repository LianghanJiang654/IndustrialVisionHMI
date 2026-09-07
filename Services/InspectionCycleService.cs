using FactorialApp.Models;

namespace FactorialApp.Services;

public sealed class InspectionCycleService
{
    private readonly AppConfig _cfg; private readonly ICameraService _camera; private readonly IPlcService _plc; private readonly IVisionService _vision; private readonly HistoryService _history; private readonly AlarmService _alarms; private long _cycle;
    public event Action<MachineState,string>? StateChanged;
    public InspectionCycleService(AppConfig cfg,ICameraService camera,IPlcService plc,IVisionService vision,HistoryService history,AlarmService alarms){_cfg=cfg;_camera=camera;_plc=plc;_vision=vision;_history=history;_alarms=alarms;}
    private void Set(MachineState s,string detail) => StateChanged?.Invoke(s,detail);

    public async Task<InspectionRecord> RunAsync(RecipeRecord recipe,string operatorName,CancellationToken ct)
    {
        long cycleId=Interlocked.Increment(ref _cycle); string serial=$"{DateTime.Now:yyyyMMddHHmmssfff}-{cycleId:D6}"; CameraFrame? frame=null; VisionResult? result=null;
        try
        {
            Set(MachineState.WaitingPart,"WAITING PLC START"); await _plc.WaitForStartAsync(ct);
            await _plc.SetBusyAsync(true,ct); Set(MachineState.TriggerCamera,"TRIGGER CAMERA");
            frame=await Retry(()=>_camera.CaptureAsync(ct),"E002","CAMERA CAPTURE",ct); Set(MachineState.Acquiring,"IMAGE ACQUIRED");
            string rawPath=_history.SaveImage(frame.Bytes,recipe.ProductCode,serial,false,frame.Extension);
            Set(MachineState.Inspecting,"VISION INSPECTION"); result=await Retry(()=>_vision.InspectAsync(frame,recipe,ct),"V900","VISION SERVICE",ct);
            bool pass=result.Success&&result.InspectionPass; Set(MachineState.SendingResult,pass?"SEND PASS":"SEND NG"); await _plc.SetResultAsync(pass,ct);
            string annotated=""; if(!pass && !string.IsNullOrWhiteSpace(result.AnnotatedImageBase64)) annotated=_history.SaveImage(Convert.FromBase64String(result.AnnotatedImageBase64),recipe.ProductCode,serial,true,".jpg");
            var m=result.Marks.FirstOrDefault(); var rec=new InspectionRecord{SerialNumber=serial,Timestamp=DateTime.Now,ProductCode=recipe.ProductCode,RecipeName=recipe.Name,RecipeVersion=recipe.Version,Passed=pass,NgCode=pass?"":MapNgCode(result),NgReason=pass?"":result.Message,X=m?.X??0,Y=m?.Y??0,Angle=m?.Angle??0,Area=m?.Area??0,ExposureUs=_camera.ExposureUs,Gain=_camera.Gain,Operator=operatorName,ImagePath=annotated,RawImagePath=rawPath,CycleId=cycleId}; rec.Id=_history.Save(rec);
            if(!pass)_alarms.Raise(rec.NgCode,rec.NgReason,"VISION"); Set(MachineState.Completed,pass?"PASS":"NG"); await _plc.ResetCycleAsync(ct); Set(MachineState.Idle,"READY"); return rec;
        }
        catch(OperationCanceledException){Set(MachineState.Stopping,"STOPPED");try{await _plc.ResetCycleAsync(CancellationToken.None);}catch{} throw;}
        catch(Exception ex){_alarms.Raise("E999",ex.Message,"CYCLE");Set(MachineState.Alarm,ex.Message);try{await _plc.SetResultAsync(false,CancellationToken.None);}catch{}throw;}
    }
    private async Task<T> Retry<T>(Func<Task<T>> action,string code,string source,CancellationToken ct)
    {
        Exception? last=null;for(int i=0;i<=_cfg.Cycle.MaxRetries;i++){try{return await action();}catch(Exception ex) when(ex is not OperationCanceledException){last=ex;if(i<_cfg.Cycle.MaxRetries)await Task.Delay(_cfg.Cycle.RetryDelayMs,ct);}}
        _alarms.Raise(code,last?.Message??"Unknown error",source);throw last??new InvalidOperationException(source+" failed");
    }
    private static string MapNgCode(VisionResult r){string u=(r.Message??"").ToUpperInvariant();if(!string.IsNullOrWhiteSpace(r.NgCode))return r.NgCode;if(u.Contains("COUNT"))return"V101";if(u.Contains("POSITION"))return"V102";if(u.Contains("ANGLE"))return"V103";if(u.Contains("AREA"))return"V104";return"V199";}
}

public sealed class WatchdogService : IAsyncDisposable
{
    private readonly ICameraService _camera; private readonly IPlcService _plc; private readonly IVisionService _vision; private readonly AlarmService _alarms; private CancellationTokenSource? _cts; private Task? _task;
    public event Action<bool,bool,bool>? HealthChanged;
    public WatchdogService(ICameraService camera,IPlcService plc,IVisionService vision,AlarmService alarms){_camera=camera;_plc=plc;_vision=vision;_alarms=alarms;}
    public void Start(){if(_task!=null)return;_cts=new CancellationTokenSource();_task=Loop(_cts.Token);}
    private async Task Loop(CancellationToken ct){while(!ct.IsCancellationRequested){bool c=await _camera.PingAsync(ct);bool p=await _plc.HeartbeatAsync(ct);bool v=await _vision.PingAsync(ct);HealthChanged?.Invoke(c,p,v);if(!c)try{await _camera.ConnectAsync(ct);}catch{}if(!p)try{await _plc.ConnectAsync(ct);}catch{}await Task.Delay(2000,ct);}}
    public async ValueTask DisposeAsync(){_cts?.Cancel();if(_task!=null)try{await _task;}catch{} _cts?.Dispose();}
}
