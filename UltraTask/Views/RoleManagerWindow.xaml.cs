using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UltraTask.Controls;
using UltraTask.Models;

namespace UltraTask.Views;

// Janela de configuração dos papéis Contato e Designado.
public partial class RoleManagerWindow : Window
{
    private readonly RoleConfig _config;
    private readonly Action _onChanged;
    private readonly DispatcherTimer _debounce;
    private string _activeRole = "contact";
    private RoleEntry _contactSnapshot = new();
    private RoleEntry _assigneeSnapshot = new();
    private RoleEntry _pendenciaSnapshot = new();

    public RoleManagerWindow(RoleConfig config, Action onChanged)
    {
        InitializeComponent();
        _config = config;
        _onChanged = onChanged;
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); _onChanged(); };
        _contactSnapshot = config.Contact.Clone();
        _assigneeSnapshot = config.Assignee.Clone();
        _pendenciaSnapshot = config.Pendencia.Clone();
        BuildPanel(ContactPanel, config.Contact);
        BuildPanel(AssigneePanel, config.Assignee);
        BuildPanel(PendenciaPanel, config.Pendencia);
        UpdatePreview();
    }

    // Monta os campos de edição de um papel dentro do painel indicado.
    private void BuildPanel(StackPanel panel, RoleEntry role)
    {
        panel.Children.Clear();

        AddRow(panel, "Cor:", MakeColorRow(role, () => { ScheduleChanged(); UpdatePreview(); }));
        AddRow(panel, "Estilo:", MakeStyleCombo(role, () => { ScheduleChanged(); UpdatePreview(); }));
        AddRow(panel, "Prefixo:", MakeTextInput(role.Prefix, v => { role.Prefix = v; ScheduleChanged(); UpdatePreview(); }));
        AddRow(panel, "Fonte:", MakeFontCombo(role, () => { ScheduleChanged(); UpdatePreview(); }));
        AddRow(panel, "Tamanho (chars):", MakeTextInput(role.Size, v => { role.Size = v; ScheduleChanged(); UpdatePreview(); }, width: 60));
    }

    private static void AddRow(StackPanel parent, string label, UIElement control)
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
        };
        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(control, 1);
        row.Children.Add(lbl);
        row.Children.Add(control);
        parent.Children.Add(row);
    }

    private UIElement MakeColorRow(RoleEntry role, Action onChange)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var swatch = new Border
        {
            Width = 24, Height = 24, CornerRadius = new CornerRadius(3),
            Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 6, 0),
        };
        try { swatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(role.Color)); }
        catch { swatch.Background = Brushes.Gray; }

        var hex = new TextBlock
        {
            Text = role.Color, Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
            VerticalAlignment = VerticalAlignment.Center, FontSize = 11,
        };

        swatch.MouseLeftButtonDown += (_, _) =>
        {
            var picked = ColorPickerDialog.Pick(role.Color, this);
            if (picked is not null)
            {
                role.Color = picked;
                hex.Text = picked;
                try { swatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(picked)); }
                catch { }
                onChange();
            }
        };

        panel.Children.Add(swatch);
        panel.Children.Add(hex);
        return panel;
    }

    private static ComboBox MakeStyleCombo(RoleEntry role, Action onChange)
    {
        var cb = new ComboBox
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51)),
            FontSize = 11, Height = 24,
        };
        cb.Items.Add("balão");
        cb.Items.Add("rótulo");
        cb.Items.Add("faixa");
        cb.SelectedItem = role.Style is "balão" or "rótulo" or "faixa" ? role.Style : "balão";
        cb.SelectionChanged += (_, _) =>
        {
            role.Style = cb.SelectedItem?.ToString() ?? "balão";
            onChange();
        };
        return cb;
    }

    private static ComboBox MakeFontCombo(RoleEntry role, Action onChange)
    {
        var cb = new ComboBox
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51)),
            FontSize = 11, Height = 24,
        };
        foreach (var f in new[] { "Segoe UI", "Consolas", "Courier New", "Verdana", "Arial", "Tahoma" })
            cb.Items.Add(f);
        cb.SelectedItem = cb.Items.Contains(role.Font) ? role.Font : "Segoe UI";
        cb.SelectionChanged += (_, _) =>
        {
            role.Font = cb.SelectedItem?.ToString() ?? "Segoe UI";
            onChange();
        };
        return cb;
    }

    private TextBox MakeTextInput(string initial, Action<string> onChange, double width = double.NaN)
    {
        var tb = new TextBox
        {
            Text = initial,
            Padding = new Thickness(4, 2, 4, 2),
            FontSize = 11, Height = 24,
        };
        tb.SetResourceReference(TextBox.BackgroundProperty, "BgPanel");
        tb.SetResourceReference(TextBox.ForegroundProperty, "TextPrimary");
        tb.SetResourceReference(TextBox.BorderBrushProperty, "BorderSubtle");
        tb.SetResourceReference(TextBox.CaretBrushProperty, "TextPrimary");
        if (!double.IsNaN(width)) tb.Width = width;
        tb.TextChanged += (_, _) => onChange(tb.Text);
        return tb;
    }

    private void UpdatePreview()
    {
        PreviewPanel.Children.Clear();

        foreach (var (role, value, tabTag) in new[]
        {
            (_config.Assignee, "Designado",        "assignee"),
            (_config.Contact,  "Contato",           "contact"),
            (_config.Pendencia,"Aguardando retorno","pendencia"),
        })
        {
            var chip = new RoleChipControl
            {
                Value = value,
                RoleColor = role.Color,
                RoleStyle = role.Style,
                RolePrefix = role.Prefix,
                RoleFont = role.Font,
                RoleSize = role.Size,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                ToolTip = $"Editar {value}",
            };
            var tag = tabTag;
            chip.MouseLeftButtonDown += (_, _) => SelectTab(tag);
            PreviewPanel.Children.Add(chip);
        }
    }

    // Reinicia o timer de debounce — o rebuild da lista ocorre 300ms após a última alteração.
    private void ScheduleChanged()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void SelectTab(string tag)
    {
        foreach (TabItem tab in Tabs.Items)
        {
            if (tab.Tag?.ToString() == tag)
            {
                Tabs.SelectedItem = tab;
                break;
            }
        }
    }

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        _activeRole = (Tabs.SelectedItem as TabItem)?.Tag?.ToString() ?? "contact";
    }

    private static void ApplySnapshot(RoleEntry target, RoleEntry source)
    {
        target.Color = source.Color;
        target.Style = source.Style;
        target.Prefix = source.Prefix;
        target.Font = source.Font;
        target.Size = source.Size;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _debounce.Stop();
        ApplySnapshot(_config.Contact, _contactSnapshot);
        ApplySnapshot(_config.Assignee, _assigneeSnapshot);
        ApplySnapshot(_config.Pendencia, _pendenciaSnapshot);
        _onChanged();
        Close();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Garante que alterações pendentes no debounce sejam aplicadas antes de fechar.
        if (_debounce.IsEnabled) { _debounce.Stop(); _onChanged(); }
        Close();
    }
}
