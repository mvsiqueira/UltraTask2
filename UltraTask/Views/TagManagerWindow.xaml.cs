using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UltraTask.Models;
using System.Linq;

namespace UltraTask.Views;

// Janela de gerenciamento de tags — criar, renomear, reordenar, colorir, excluir.
public partial class TagManagerWindow : Window
{
    private readonly List<TagEntry> _tags;
    private readonly Action _onChanged;
    private readonly Action<string>? _onTagDeleted;
    private string _pendingColor = "#2563EB";
    private List<TagEntry> _snapshot = [];

    private static readonly string[] Styles = ["rótulo", "balão", "faixa"];
    private static readonly string[] Fonts  = ["Arial", "Calibri", "Century Gothic", "Consolas", "Courier New", "Dancing Script", "Impact", "JetBrains Mono", "Lucida Handwriting", "Segoe UI", "Space Mono", "Tahoma", "Verdana"];

    public TagManagerWindow(List<TagEntry> tags, Action onChanged, Action<string>? onTagDeleted = null)
    {
        InitializeComponent();
        _tags = tags;
        _onChanged = onChanged;
        _onTagDeleted = onTagDeleted;
        _snapshot = tags.Select(t => t.Clone()).ToList();

        foreach (var s in Styles) NewTagStyle.Items.Add(s);
        NewTagStyle.SelectedIndex = 0;
        foreach (var f in Fonts) NewTagFont.Items.Add(f);
        NewTagFont.SelectedIndex = 0;

        RefreshList();
        UpdateNewSwatch();
    }

    private void TakeSnapshot() =>
        _snapshot = _tags.Select(t => t.Clone()).ToList();

    private void RefreshList()
    {
        TagList.ItemsSource = null;
        TagList.ItemsSource = _tags.OrderBy(t => t.Order).ToList();

        // Inicializa combos de estilo e fonte em cada linha
        // Usa Background para garantir que o DataBind resolveu Tag="{Binding}" antes de acessar item.Tag
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
        {
            foreach (var item in FindVisualChildren<ComboBox>(TagList))
            {
                if (item.Tag is not TagEntry tag || item.Items.Count != 0) continue;
                if (item.Name == "StyleCombo")
                {
                    foreach (var s in Styles) item.Items.Add(s);
                    item.SelectedItem = Styles.Contains(tag.Style) ? tag.Style : "rótulo";
                }
                else if (item.Name == "FontCombo")
                {
                    foreach (var f in Fonts) item.Items.Add(f);
                    item.SelectedItem = Fonts.Contains(tag.Font) ? tag.Font : "Segoe UI";
                }
            }
        });
    }

    private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) yield return t;
            foreach (var sub in FindVisualChildren<T>(child)) yield return sub;
        }
    }

    // --- Nova tag ---

    private void OnNewTagKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { AddTag(); e.Handled = true; }
    }

    private void OnAddTag(object sender, RoutedEventArgs e) => AddTag();

    private void AddTag()
    {
        var name = NewTagName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        // Verifica duplicata (case-insensitive)
        if (_tags.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Já existe uma tag com esse nome.", "UltraTask", MessageBoxButton.OK);
            return;
        }

        _tags.Add(new TagEntry
        {
            Name = name,
            Color = _pendingColor,
            Order = _tags.Count,
            Size = NewTagSize.Text.Trim(),
            Style = NewTagStyle.SelectedItem?.ToString() ?? "rótulo",
            Font = NewTagFont.SelectedItem?.ToString() ?? "Segoe UI",
        });

        NewTagName.Text = string.Empty;
        NewTagSize.Text = string.Empty;
        _onChanged();
        RefreshList();
    }

    // --- Editar cor de uma tag existente ---

    private void OnEditTagColor(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.Tag is TagEntry tag)
        {
            var color = PickColor(tag.Color);
            if (color is not null)
            {
                tag.Color = color;
                _onChanged();
                RefreshList();
            }
        }
    }

    // --- Cor da nova tag ---

    private void OnPickColor(object sender, MouseButtonEventArgs e)
    {
        var color = PickColor(_pendingColor);
        if (color is not null)
        {
            _pendingColor = color;
            UpdateNewSwatch();
        }
    }

    private void UpdateNewSwatch()
    {
        try { ColorSwatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_pendingColor)); }
        catch { }
    }

    // --- Renomear / tamanho ---

    private void OnTagNameChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.Tag is TagEntry tag)
        {
            var newName = tb.Text.Trim();
            if (!string.IsNullOrEmpty(newName)) tag.Name = newName;
            _onChanged();
        }
    }

    private void OnTagSizeChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.Tag is TagEntry tag)
        {
            tag.Size = tb.Text.Trim();
            _onChanged();
        }
    }

    private void OnTagStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.Tag is TagEntry tag && cb.SelectedItem is string style)
        {
            tag.Style = style;
            _onChanged();
        }
    }

    private void OnTagFontChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.Tag is TagEntry tag && cb.SelectedItem is string font)
        {
            tag.Font = font;
            _onChanged();
        }
    }

    // --- Reordenar ---

    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TagEntry tag)
        {
            var list = _tags.OrderBy(t => t.Order).ToList();
            var idx = list.IndexOf(tag);
            if (idx <= 0) return;
            list[idx].Order--;
            list[idx - 1].Order++;
            _onChanged();
            RefreshList();
        }
    }

    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TagEntry tag)
        {
            var list = _tags.OrderBy(t => t.Order).ToList();
            var idx = list.IndexOf(tag);
            if (idx >= list.Count - 1) return;
            list[idx].Order++;
            list[idx + 1].Order--;
            _onChanged();
            RefreshList();
        }
    }

    // --- Excluir ---

    private void OnDeleteTag(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TagEntry tag)
        {
            var r = MessageBox.Show($"Excluir tag \"{tag.Name}\"?", "UltraTask",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                _tags.Remove(tag);
                // Reordena para não deixar buracos
                var ordered = _tags.OrderBy(t => t.Order).ToList();
                for (int i = 0; i < ordered.Count; i++) ordered[i].Order = i;
                _onTagDeleted?.Invoke(tag.Name);
                _onChanged();
                RefreshList();
            }
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _tags.Clear();
        foreach (var t in _snapshot) _tags.Add(t);
        _onChanged();
        Close();
    }

    private void OnSave(object sender, RoutedEventArgs e) => Close();

    private string? PickColor(string current) =>
        ColorPickerDialog.Pick(current, this);
}
