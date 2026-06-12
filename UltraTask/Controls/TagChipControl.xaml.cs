using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace UltraTask.Controls;

// Chip visual de uma tag — cor configurável, largura fixa opcional.
public partial class TagChipControl : System.Windows.Controls.UserControl
{
    public string TagName  { get; set; } = string.Empty;
    public string TagColor { get; set; } = "#2563EB";
    public string TagSize  { get; set; } = string.Empty; // em chars; vazio = auto

    // Evento para o pai aplicar filtro por essa tag (clique direito).
    public event EventHandler<string>? FilterRequested;

    public TagChipControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ChipLabel.Text = TagName;
        var bg = BrushFromHex(TagColor);
        ChipBorder.Background = bg;
        ChipLabel.Foreground = ContrastBrush(bg.Color);

        // Largura fixa opcional: converte caracteres → pixels (aprox 7px/char)
        if (int.TryParse(TagSize, out int chars) && chars > 0)
        {
            ChipLabel.Width = chars * 7.0;
            ChipLabel.TextAlignment = TextAlignment.Center;
        }

        MouseRightButtonDown += (_, e2) =>
        {
            FilterRequested?.Invoke(this, TagName);
            e2.Handled = true;
        };

        // Tooltip com nome completo caso o chip seja truncado
        ToolTip = TagName;
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.DimGray; }
    }

    // Luminância relativa — retorna preto ou branco conforme contraste.
    private static SolidColorBrush ContrastBrush(Color c)
    {
        double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        return luminance > 140 ? Brushes.Black : Brushes.White;
    }
}
