using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using UltraTask.Services;
using UltraTask.ViewModels;
using UltraTask.Views;

namespace UltraTask;

public partial class App : Application
{
    [DllImport("shell32.dll")]
    private static extern void SetCurrentProcessExplicitAppUserModelID(
        [MarshalAs(UnmanagedType.LPWStr)] string appId);

    protected override void OnStartup(StartupEventArgs e)
    {
        SetCurrentProcessExplicitAppUserModelID("Ultrasoft.UltraTask");
        base.OnStartup(e);
        EventManager.RegisterClassHandler(
            typeof(Window),
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(OnGlobalPreviewKeyDown));
    }

    private static void OnGlobalPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.F11 or Key.F12)) return;

        var vm = (Current.MainWindow as MainWindow)?.DataContext as MainViewModel;
        if (vm is null) return;

        var theme = e.Key == Key.F11 ? "light" : "dark";
        ThemeService.Apply(theme);
        vm.Settings.Theme = theme;
        PersistenceService.SaveSettings(vm.Settings);
        e.Handled = true;
    }
}
