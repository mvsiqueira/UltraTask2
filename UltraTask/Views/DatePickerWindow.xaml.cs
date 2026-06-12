using System.Windows;

namespace UltraTask.Views;

// Janela modal de seleção de data — usa Calendar diretamente (sem DatePicker dropdown).
public partial class DatePickerWindow : Window
{
    public DateOnly? SelectedDate { get; private set; }
    public bool Cleared { get; private set; }

    public DatePickerWindow(string currentDate)
    {
        InitializeComponent();

        if (DateOnly.TryParse(currentDate, out var d))
            Cal.SelectedDate = new DateTime(d.Year, d.Month, d.Day);

        KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };

        // Duplo clique no dia confirma direto
        Cal.MouseDoubleClick += (_, _) => OnOk(this, null!);
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (Cal.SelectedDate.HasValue)
            SelectedDate = DateOnly.FromDateTime(Cal.SelectedDate.Value);
        DialogResult = true;
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        Cleared = true;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
