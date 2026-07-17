using System.Windows;

namespace UltraTask.Views;

// Janela simples de edição de subtarefas — uma por linha.
public partial class SubtasksWindow : Window
{
    private readonly Action<List<string>> _onSave;

    public SubtasksWindow(IReadOnlyList<string> initial, Action<List<string>> onSave)
    {
        InitializeComponent();
        _onSave = onSave;
        SubtasksBox.Text = string.Join(Environment.NewLine, initial);
        SubtasksBox.Focus();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var lines = SubtasksBox.Text
            .Split('\n')
            .Select(l => l.Trim('\r', ' ', '\t'))
            .Where(l => l.Length > 0)
            .ToList();
        _onSave(lines);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
