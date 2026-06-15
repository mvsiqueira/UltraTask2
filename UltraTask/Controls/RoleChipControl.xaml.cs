using System.Windows;
using System.Windows.Media;

namespace UltraTask.Controls;

// Chip de papel (Contato ou Designado) — suporta estilo "rótulo" (cantos retos) e "balão" (pill).
public partial class RoleChipControl : System.Windows.Controls.UserControl
{
    public string Value      { get; set; } = string.Empty;
    public string RoleColor  { get; set; } = "#0F766E";
    public string RoleStyle  { get; set; } = "balão"; // "rótulo" | "balão"
    public string RolePrefix { get; set; } = string.Empty;
    public string RoleFont   { get; set; } = "Segoe UI";
    public string RoleSize   { get; set; } = string.Empty;

    public RoleChipControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ChipBorder.Background = BrushFromHex(RoleColor);

        var text = string.IsNullOrEmpty(RolePrefix) ? Value : $"{RolePrefix} {Value}";
        ChipLabel.Text = text;
        ChipLabel.FontFamily = new FontFamily(RoleFont);
        ChipLabel.Foreground = GetContrastForeground(RoleColor);
        ChipLabel.VerticalAlignment = VerticalAlignment.Center;

        if (RoleStyle == "faixa")
        {
            VerticalAlignment = VerticalAlignment.Center;
            SetResourceReference(HeightProperty, "RowHeight");
            ChipBorder.CornerRadius = new CornerRadius(0);
            ChipBorder.Padding = new Thickness(6, 0, 6, 0);
            ChipBorder.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            VerticalAlignment = VerticalAlignment.Center;
            ChipBorder.CornerRadius = RoleStyle == "balão"
                ? (CornerRadius)FindResource("BalloonRadius")
                : (CornerRadius)FindResource("ChipRadius");
            ChipBorder.Padding = (Thickness)FindResource("ChipPadding");
        }

        if (int.TryParse(RoleSize, out int chars) && chars > 0)
        {
            ChipLabel.Width = chars * 7.0;
            ChipLabel.TextAlignment = TextAlignment.Center;
        }

        ToolTip = Value;
    }

    private static Brush GetContrastForeground(string hex)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            return luminance > 140 ? Brushes.Black : Brushes.White;
        }
        catch { return Brushes.White; }
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.DimGray; }
    }
}
