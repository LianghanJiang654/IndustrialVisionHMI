using System.IO;
using System.Text.Json;
using System.Windows;
using FactorialApp.Services;

namespace FactorialApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppConfig config = AppConfig.Load();
        Directory.CreateDirectory(Path.GetDirectoryName(config.DatabasePath) ?? ".");
        Directory.CreateDirectory(config.ImageArchivePath);

        var db = new DatabaseService(config.DatabasePath);
        db.Initialize();

        ICameraService camera = config.UseSimulator || !config.Camera.Provider.Equals("Basler", StringComparison.OrdinalIgnoreCase)
            ? new SimulatedCameraService(config.Camera)
            : new BaslerCameraService(config.Camera);

        IPlcService plc = config.UseSimulator || !config.Plc.Provider.Equals("ModbusTcp", StringComparison.OrdinalIgnoreCase)
            ? new SimulatedPlcService()
            : new ModbusTcpPlcService(config.Plc);

        IVisionService vision = new TcpVisionService(config.Vision);
        var history = new HistoryService(db, config.ImageArchivePath);
        var alarms = new AlarmService(db);
        var recipes = new RecipeService(db);
        var auth = new AuthService(db);
        var csv = new CsvExportService();
        var cycle = new InspectionCycleService(config, camera, plc, vision, history, alarms);
        var watchdog = new WatchdogService(camera, plc, vision, alarms);

        var vm = new MainViewModel(config, camera, plc, vision, history, alarms, recipes, auth, csv, cycle, watchdog);
        var window = new MainWindow(vm);
        MainWindow = window;
        window.Show();
    }
}
