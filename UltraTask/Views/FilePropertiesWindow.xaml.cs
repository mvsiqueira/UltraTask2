using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UltraTask.Models;

namespace UltraTask.Views;

public partial class FilePropertiesWindow : Window
{
    private static readonly (string Key, string Label)[] AllTokens =
    [
        ("tags",      "Tags"),
        ("assignee",  "Designado"),
        ("contact",   "Contato"),
        ("title",     "Título"),
        ("pendencia", "Pendência"),
        ("notes",     "Notas"),
        ("spacer",    "Espaço"),
        ("date",      "Data"),
    ];

    private readonly TaskFile _file;
    private readonly Action _onSave;
    private readonly List<(string Key, bool Active)> _state = [];
    private readonly List<(string Key, bool Active)> _snapshot = [];

    public FilePropertiesWindow(TaskFile file, Action onSave)
    {
        InitializeComponent();
        _file = file;
        _onSave = onSave;

        foreach (var key in file.TaskRowOrder)
            _state.Add((key, true));
        foreach (var (key, _) in AllTokens)
            if (_state.All(s => s.Key != key))
                _state.Add((key, false));

        _snapshot.AddRange(_state);
        RenderList();
    }

    private void RenderList()
    {
        TokenList.Children.Clear();
        for (int i = 0; i < _state.Count; i++)
        {
            var (key, active) = _state[i];
            var label = AllTokens.FirstOrDefault(t => t.Key == key).Label ?? key;
            var idx = i;

            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cb = new CheckBox
            {
                IsChecked = active,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 6, 0),
            };
            cb.Checked   += (_, _) => { _state[idx] = (key, true);  };
            cb.Unchecked += (_, _) => { _state[idx] = (key, false); };

            var lbl = new TextBlock
            {
                Text = label,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
            btnPanel.Children.Add(MakeArrowBtn("▲", idx, -1));
            btnPanel.Children.Add(MakeArrowBtn("▼", idx, +1));

            Grid.SetColumn(cb,       0);
            Grid.SetColumn(lbl,      1);
            Grid.SetColumn(btnPanel, 2);
            row.Children.Add(cb);
            row.Children.Add(lbl);
            row.Children.Add(btnPanel);
            TokenList.Children.Add(row);
        }
    }

    private Button MakeArrowBtn(string content, int idx, int dir)
    {
        var btn = new Button
        {
            Content = content,
            Width = 24, Height = 22,
            Margin = new Thickness(2, 0, 0, 0),
            Style = (Style)FindResource("SidebarButtonStyle"),
        };
        btn.Click += (_, _) =>
        {
            var target = idx + dir;
            if (target < 0 || target >= _state.Count) return;
            (_state[idx], _state[target]) = (_state[target], _state[idx]);
            RenderList();
        };
        return btn;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var result = _state.Where(s => s.Active).Select(s => s.Key).ToList();
        if (!result.Contains("title")) result.Add("title");

        _file.TaskRowOrder = result;
        _onSave();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
