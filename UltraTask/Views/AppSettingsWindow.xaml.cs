using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using UltraTask.Models;

namespace UltraTask.Views;

public partial class AppSettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<string> _onFileChanged;
    private readonly Action _onLayoutChanged;

    // Snapshot para revert
    private readonly string _origTheme;
    private readonly string _origLayout;

    // Arquivo selecionado nesta sessão (null = não alterado)
    private string? _pendingFilePath;

    public AppSettingsWindow(AppSettings settings, Action<string> onFileChanged, Action onLayoutChanged)
    {
        InitializeComponent();
        _settings = settings;
        _onFileChanged = onFileChanged;
        _onLayoutChanged = onLayoutChanged;
        _origTheme  = settings.Theme;
        _origLayout = settings.LayoutMode;

        FilePathLabel.Text = string.IsNullOrEmpty(settings.TaskFilePath)
            ? "(nenhum arquivo selecionado)"
            : settings.TaskFilePath;

        SelectComboItem(ThemeCombo,    settings.Theme);
        SelectComboItem(LayoutCombo,   settings.LayoutMode);
        SelectComboItem(TitlebarCombo, settings.TitlebarFormat);
        HighlightImportantCheck.IsChecked = settings.HighlightImportant;
    }

    private static void SelectComboItem(ComboBox cb, string tag)
    {
        foreach (ComboBoxItem item in cb.Items)
            if (item.Tag?.ToString() == tag) { cb.SelectedItem = item; return; }
        cb.SelectedIndex = 0;
    }

    private void OnSelectFile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Selecionar arquivo de tarefas",
            Filter = "JSON (*.json)|*.json|Todos os arquivos|*.*",
            CheckFileExists = false,
        };
        if (dlg.ShowDialog() != true) return;
        _pendingFilePath = dlg.FileName;
        FilePathLabel.Text = dlg.FileName;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Theme              = ((ThemeCombo.SelectedItem    as ComboBoxItem)?.Tag?.ToString()) ?? "dark";
        _settings.LayoutMode         = ((LayoutCombo.SelectedItem   as ComboBoxItem)?.Tag?.ToString()) ?? "compact";
        _settings.TitlebarFormat     = ((TitlebarCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()) ?? "app-list";
        _settings.HighlightImportant = HighlightImportantCheck.IsChecked == true;
        Services.ThemeService.HighlightImportant = _settings.HighlightImportant;
        Services.PersistenceService.SaveSettings(_settings);
        Services.ThemeService.Apply(_settings.Theme);
        Services.LayoutService.Apply(_settings.LayoutMode);
        _onLayoutChanged();

        if (_pendingFilePath is not null)
            _onFileChanged(_pendingFilePath);

        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _settings.Theme = _origTheme;
        _settings.LayoutMode = _origLayout;
        Close();
    }
}
