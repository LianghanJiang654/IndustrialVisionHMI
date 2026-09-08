using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FactorialApp.Core;
using FactorialApp.Models;
using FactorialApp.Services;

namespace FactorialApp;

public sealed class MainViewModel : ObservableObject
{
    private readonly AppConfig _cfg; private readonly ICameraService _camera; private readonly IPlcService _plc; private readonly IVisionService _vision; private readonly HistoryService _history; private readonly AlarmService _alarms; private readonly RecipeService _recipes; private readonly AuthService _auth; private readonly CsvExportService _csv; private readonly InspectionCycleService _cycle; private readonly WatchdogService _watchdog;
    private CancellationTokenSource? _autoCts;
    public ObservableCollection<InspectionRecord> InspectionHistory { get; }=new(); public ObservableCollection<AlarmItem> AlarmHistory { get; }=new(); public ObservableCollection<RecipeRecord> Recipes { get; }=new(); public ObservableCollection<string> Logs { get; }=new();
    public string[] ResultFilters { get; }={"ALL","PASS","NG"};

    private UserAccount? _currentUser; public UserAccount? CurrentUser{get=>_currentUser;private set{Set(ref _currentUser,value);Raise(nameof(IsLoggedIn));Raise(nameof(IsEngineer));Raise(nameof(IsAdmin));Raise(nameof(UserText));}}
    public bool IsLoggedIn=>CurrentUser!=null; public bool IsEngineer=>CurrentUser?.Role>=UserRole.Engineer; public bool IsAdmin=>CurrentUser?.Role>=UserRole.Admin; public string UserText=>CurrentUser is null?"NOT LOGGED IN":$"{CurrentUser.Username} / {CurrentUser.Role}";
    private MachineState _machineState=MachineState.Offline; public MachineState MachineState{get=>_machineState;set=>Set(ref _machineState,value);} private string _stateDetail="STARTING";public string StateDetail{get=>_stateDetail;set=>Set(ref _stateDetail,value);}
    private bool _cameraOk;public bool CameraOk{get=>_cameraOk;set{Set(ref _cameraOk,value);Raise(nameof(CameraText));}}private bool _plcOk;public bool PlcOk{get=>_plcOk;set{Set(ref _plcOk,value);Raise(nameof(PlcText));}}private bool _visionOk;public bool VisionOk{get=>_visionOk;set{Set(ref _visionOk,value);Raise(nameof(VisionText));}}
    public string CameraText=>CameraOk?$"ONLINE {_camera.Name}":"OFFLINE";public string PlcText=>PlcOk?$"ONLINE {_plc.Name}":"OFFLINE";public string VisionText=>VisionOk?"ONLINE":"OFFLINE";
    private RecipeRecord _selectedRecipe=new();public RecipeRecord SelectedRecipe{get=>_selectedRecipe;set{if(value!=null){Set(ref _selectedRecipe,value);ApplyRecipeToCamera();}}}
    private string _filterProduct="";public string FilterProduct{get=>_filterProduct;set=>Set(ref _filterProduct,value);}private string _filterRecipe="";public string FilterRecipe{get=>_filterRecipe;set=>Set(ref _filterRecipe,value);}private string _filterNgCode="";public string FilterNgCode{get=>_filterNgCode;set=>Set(ref _filterNgCode,value);}private string _filterSerial="";public string FilterSerial{get=>_filterSerial;set=>Set(ref _filterSerial,value);}private string _filterResult="ALL";public string FilterResult{get=>_filterResult;set=>Set(ref _filterResult,value);}private DateTime? _filterFrom=DateTime.Today;public DateTime? FilterFrom{get=>_filterFrom;set=>Set(ref _filterFrom,value);}private DateTime? _filterTo=DateTime.Today.AddDays(1);public DateTime? FilterTo{get=>_filterTo;set=>Set(ref _filterTo,value);}
    private long _total;public long Total{get=>_total;set=>Set(ref _total,value);}private long _pass;public long Pass{get=>_pass;set=>Set(ref _pass,value);}private long _ng;public long Ng{get=>_ng;set=>Set(ref _ng,value);}public double Yield=>Total==0?0:Pass*100.0/Total;
    private InspectionRecord? _lastInspection;public InspectionRecord? LastInspection{get=>_lastInspection;set{Set(ref _lastInspection,value);Raise(nameof(LastResultText));Raise(nameof(HasLastInspection));}}public string LastResultText=>LastInspection is null?"WAITING":LastInspection.Passed?"PASS":$"NG {LastInspection.NgCode}"; public bool HasLastInspection=>LastInspection!=null;
    private BitmapImage? _lastInspectionImage; public BitmapImage? LastInspectionImage{get=>_lastInspectionImage;private set=>Set(ref _lastInspectionImage,value);}
    private bool _isAuto;public bool IsAuto{get=>_isAuto;set=>Set(ref _isAuto,value);}

    public ICommand RunCycleCommand{get;} public ICommand AutoStartCommand{get;} public ICommand AutoStopCommand{get;} public ICommand RefreshHistoryCommand{get;} public ICommand ExportCsvCommand{get;} public ICommand OpenImageCommand{get;} public ICommand AckAlarmCommand{get;} public ICommand RefreshAlarmsCommand{get;} public ICommand SaveRecipeCommand{get;} public ICommand NewRecipeCommand{get;} public ICommand LogoutCommand{get;}

    public MainViewModel(AppConfig cfg,ICameraService camera,IPlcService plc,IVisionService vision,HistoryService history,AlarmService alarms,RecipeService recipes,AuthService auth,CsvExportService csv,InspectionCycleService cycle,WatchdogService watchdog)
    {
        _cfg=cfg;_camera=camera;_plc=plc;_vision=vision;_history=history;_alarms=alarms;_recipes=recipes;_auth=auth;_csv=csv;_cycle=cycle;_watchdog=watchdog;
        RunCycleCommand=new AsyncRelayCommand(RunOneAsync,()=>IsLoggedIn&&!IsAuto);AutoStartCommand=new AsyncRelayCommand(StartAutoAsync,()=>IsLoggedIn&&!IsAuto);AutoStopCommand=new RelayCommand(StopAuto,()=>IsAuto);RefreshHistoryCommand=new RelayCommand(RefreshHistory);ExportCsvCommand=new RelayCommand(ExportCsv);OpenImageCommand=new RelayCommand<InspectionRecord>(OpenImage);AckAlarmCommand=new RelayCommand<AlarmItem>(AckAlarm);RefreshAlarmsCommand=new RelayCommand(RefreshAlarms);SaveRecipeCommand=new RelayCommand(SaveRecipe,()=>IsEngineer);NewRecipeCommand=new RelayCommand(NewRecipe,()=>IsEngineer);LogoutCommand=new RelayCommand(()=>CurrentUser=null);
        _cycle.StateChanged+=(s,d)=>System.Windows.Application.Current.Dispatcher.Invoke(()=>{MachineState=s;StateDetail=d;Log($"STATE {s}: {d}");});
        _watchdog.HealthChanged+=(c,p,v)=>System.Windows.Application.Current.Dispatcher.Invoke(()=>{CameraOk=c;PlcOk=p;VisionOk=v;});
        LoadRecipes();RefreshHistory();RefreshAlarms();
    }

    public bool Login(string user,string password){var a=_auth.Authenticate(user,password);if(a is null){Log("LOGIN FAILED");return false;}CurrentUser=a;Log($"LOGIN {a.Username} / {a.Role}");return true;}
    public async Task InitializeAsync(){MachineState=MachineState.Offline;StateDetail="AUTO CONNECT";try{await _camera.ConnectAsync();CameraOk=true;}catch(Exception ex){Log("Camera connect: "+ex.Message);}try{await _plc.ConnectAsync();PlcOk=true;}catch(Exception ex){Log("PLC connect: "+ex.Message);}VisionOk=await _vision.PingAsync();_watchdog.Start();MachineState=MachineState.Idle;StateDetail="READY";}
    private async Task RunOneAsync(){if(CurrentUser is null)return;try{ApplyRecipeToCamera();LastInspection=await _cycle.RunAsync(SelectedRecipe,CurrentUser.Username,CancellationToken.None);LoadLastInspectionImage();RefreshStatsOnly();RefreshHistory();}catch(Exception ex){Log("CYCLE ERROR: "+ex.Message);} }
    private async Task StartAutoAsync(){if(CurrentUser is null||IsAuto)return;IsAuto=true;_autoCts=new CancellationTokenSource();Log("AUTO START");try{while(!_autoCts.IsCancellationRequested){ApplyRecipeToCamera();LastInspection=await _cycle.RunAsync(SelectedRecipe,CurrentUser.Username,_autoCts.Token);LoadLastInspectionImage();RefreshStatsOnly();await Task.Delay(_cfg.Cycle.InterCycleDelayMs,_autoCts.Token);}}catch(OperationCanceledException){}catch(Exception ex){Log("AUTO ERROR: "+ex.Message);}finally{IsAuto=false;Log("AUTO STOP");RefreshHistory();}}
    private void StopAuto()=>_autoCts?.Cancel();
    private void ApplyRecipeToCamera(){_camera.ExposureUs=SelectedRecipe.ExposureUs;_camera.Gain=SelectedRecipe.Gain;}
    private void LoadLastInspectionImage(){try{var x=LastInspection;if(x is null){LastInspectionImage=null;return;}string p=!string.IsNullOrWhiteSpace(x.ImagePath)?x.ImagePath:x.RawImagePath;if(string.IsNullOrWhiteSpace(p)||!File.Exists(p)){LastInspectionImage=null;return;}var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(Path.GetFullPath(p),UriKind.Absolute);image.EndInit();image.Freeze();LastInspectionImage=image;}catch(Exception ex){LastInspectionImage=null;Log("Preview image: "+ex.Message);}}
    private HistoryFilter Filter()=>new(){From=FilterFrom,To=FilterTo,Product=FilterProduct,Recipe=FilterRecipe,NgCode=FilterNgCode,SerialNumber=FilterSerial,Result=FilterResult};
    private void RefreshHistory(){InspectionHistory.Clear();foreach(var x in _history.Query(Filter()))InspectionHistory.Add(x);RefreshStatsOnly();}
    private void RefreshStatsOnly(){var s=_history.GetStats(Filter());Total=s.Total;Pass=s.Pass;Ng=s.Ng;Raise(nameof(Yield));}
    private void ExportCsv(){var dlg=new SaveFileDialog{Filter="CSV (*.csv)|*.csv",FileName=$"inspection_{DateTime.Now:yyyyMMdd_HHmmss}.csv"};if(dlg.ShowDialog()==true){_csv.Export(dlg.FileName,InspectionHistory);Log("CSV exported: "+dlg.FileName);}}
    private void OpenImage(InspectionRecord x){string p=!string.IsNullOrWhiteSpace(x.ImagePath)?x.ImagePath:x.RawImagePath;if(string.IsNullOrWhiteSpace(p)||!File.Exists(p)){Log("Image not found");return;}Process.Start(new ProcessStartInfo(p){UseShellExecute=true});}
    private void RefreshAlarms(){AlarmHistory.Clear();foreach(var a in _alarms.GetRecent())AlarmHistory.Add(a);}
    private void AckAlarm(AlarmItem a){if(!IsLoggedIn)return;_alarms.Acknowledge(a.Id);a.IsAcknowledged=true;Log($"ALARM ACK {a.Code} by {CurrentUser!.Username}");}
    private void LoadRecipes(){Recipes.Clear();foreach(var r in _recipes.GetLatestAll())Recipes.Add(r);SelectedRecipe=Recipes.FirstOrDefault()??new RecipeRecord();}
    private void SaveRecipe(){if(!IsEngineer)return;var copy=new RecipeRecord{Name=SelectedRecipe.Name,ProductCode=SelectedRecipe.ProductCode,Threshold=SelectedRecipe.Threshold,MinArea=SelectedRecipe.MinArea,MaxArea=SelectedRecipe.MaxArea,PositionTolerance=SelectedRecipe.PositionTolerance,AngleTolerance=SelectedRecipe.AngleTolerance,AreaTolerancePercent=SelectedRecipe.AreaTolerancePercent,ExposureUs=SelectedRecipe.ExposureUs,Gain=SelectedRecipe.Gain,ThresholdMode=SelectedRecipe.ThresholdMode};_recipes.SaveNewVersion(copy);LoadRecipes();SelectedRecipe=Recipes.First(x=>x.Name==copy.Name);Log($"Recipe saved {copy.Name} v{SelectedRecipe.Version}");}
    private void NewRecipe(){SelectedRecipe=new RecipeRecord{Name="NEW_RECIPE",ProductCode="PRODUCT-X"};}
    private void Log(string text){System.Windows.Application.Current.Dispatcher.Invoke(()=>{Logs.Insert(0,$"[{DateTime.Now:HH:mm:ss}] {text}");while(Logs.Count>200)Logs.RemoveAt(Logs.Count-1);});}
}
