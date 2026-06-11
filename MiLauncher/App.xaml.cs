using System.Windows;
using MiLauncher.launcher.tools;

namespace MiLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        string logDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
            "MiLauncher"
        );
        Logger.Initialize(logDir);
        Logger.Info("MiLauncher iniciado.");

        ThemeManager.ApplyTheme();
    }
}
