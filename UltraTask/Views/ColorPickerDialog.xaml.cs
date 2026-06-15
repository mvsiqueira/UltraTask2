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

    // Abre o seletor de cor padrão do Windows com a paleta pré-carregada.
    // Retorna hex escolhido (#RRGGBB) ou null se cancelado.
    public static string? Pick(string current, Window? owner = null)
    {
        var dlg = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AllowFullOpen = true,
            SolidColorOnly = false,
            CustomColors = PaletteToColorRefs(),
        };

        try
        {
            var c = (Color)ColorConverter.ConvertFromString(current);
            dlg.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
        }
        catch { }

        var hwnd = owner is not null
            ? new System.Windows.Interop.WindowInteropHelper(owner).Handle
            : nint.Zero;

        var result = hwnd != nint.Zero
            ? dlg.ShowDialog(new Win32Wrapper(hwnd))
            : dlg.ShowDialog();

        if (result != System.Windows.Forms.DialogResult.OK) return null;

        var s = dlg.Color;
        return $"#{s.R:X2}{s.G:X2}{s.B:X2}";
    }

    private static int[] PaletteToColorRefs()
    {
        // ColorDialog.CustomColors usa COLORREF: 0x00BBGGRR
        return Palette.Take(16).Select(hex =>
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                return c.R | (c.G << 8) | (c.B << 16);
            }
            catch { return 0; }
        }).ToArray();
    }

    private sealed class Win32Wrapper(nint handle) : System.Windows.Forms.IWin32Window
    {
        public nint Handle { get; } = handle;
    }

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
