using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace UltraTask.Views;

public partial class AboutWindow : Window
{
    public AboutWindow(string buildStamp)
    {
        InitializeComponent();
        VersionLabel.Text = "v2.0.0";
        BuildLabel.Text   = buildStamp;
    }

    private void OnRepoLinkClick(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
