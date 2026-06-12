using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UltraTask.Views;

// Diálogo simples de escolha de cor: paleta predefinida + input hex manual.
public partial class ColorPickerDialog : Window
{
    public string SelectedColor { get; private set; } = "#2563EB";

    private static readonly string[] Palette =
    [
        "#EF4444","#F97316","#F59E0B","#EAB308","#84CC16","#22C55E",
        "#10B981","#14B8A6","#06B6D4","#3B82F6","#6366F1","#8B5CF6",
        "#A855F7","#EC4899","#F43F5E","#FFFFFF","#94A3B8","#475569",
        "#1E293B","#000000","#FFFF00","#FF8000","#008080","#A80054",
        "#800080","#0080C0","#CC4FB9","#CCE6FF","#E1E1E1","#FF0080",
    ];

    public ColorPickerDialog(string current)
    {
        InitializeComponent();
        SelectedColor = current;
        HexInput.Text = current;
        BuildPalette();
    }

    private void BuildPalette()
    {
        foreach (var hex in Palette)
        {
            var b = new Border
            {
                Width = 20, Height = 20,
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.Hand,
                ToolTip = hex,
            };
            try { b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { b.Background = Brushes.Gray; }

            b.MouseLeftButtonDown += (_, _) =>
            {
                HexInput.Text = hex;
                SelectedColor = hex;
            };
            PalettePanel.Children.Add(b);
        }
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        var hex = HexInput.Text.Trim();
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            Preview.Background = new SolidColorBrush(c);
            SelectedColor = hex;
        }
        catch { Preview.Background = Brushes.Transparent; }
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
