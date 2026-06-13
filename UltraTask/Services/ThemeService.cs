using System.Windows;
using System.Windows.Media;

namespace UltraTask.Services;

public static class ThemeService
{
    private record Palette(
        Color BgDeep, Color BgPanel, Color BgRow, Color BgRowHover,
        Color BgRowSelected, Color BgSection, Color BorderSubtle,
        Color TextPrimary, Color TextSecondary, Color TextMuted,
        Color Accent, Color Danger, Color ImportantEar);

    private static readonly Palette Dark = new(
        BgDeep:         C(0x11, 0x18, 0x27),
        BgPanel:        C(0x1F, 0x29, 0x37),
        BgRow:          C(0x1A, 0x21, 0x30),
        BgRowHover:     C(0x24, 0x30, 0x44),
        BgRowSelected:  C(0x1E, 0x3A, 0x5F),
        BgSection:      C(0x0F, 0x17, 0x21),
        BorderSubtle:   C(0x37, 0x41, 0x51),
        TextPrimary:    C(0xF9, 0xFA, 0xFB),
        TextSecondary:  C(0x9C, 0xA3, 0xAF),
        TextMuted:      C(0x6B, 0x72, 0x80),
        Accent:         C(0x3B, 0x82, 0xF6),
        Danger:         C(0xEF, 0x44, 0x44),
        ImportantEar:   C(0xDC, 0x26, 0x26));

    private static readonly Palette Light = new(
        BgDeep:         C(0xF3, 0xF4, 0xF6),
        BgPanel:        C(0xFF, 0xFF, 0xFF),
        BgRow:          C(0xF9, 0xFA, 0xFB),
        BgRowHover:     C(0xEF, 0xF2, 0xF7),
        BgRowSelected:  C(0xDB, 0xEA, 0xFE),
        BgSection:      C(0xE5, 0xE7, 0xEB),
        BorderSubtle:   C(0xD1, 0xD5, 0xDB),
        TextPrimary:    C(0x11, 0x18, 0x27),
        TextSecondary:  C(0x6B, 0x72, 0x80),
        TextMuted:      C(0x9C, 0xA3, 0xAF),
        Accent:         C(0x25, 0x63, 0xEB),
        Danger:         C(0xEF, 0x44, 0x44),
        ImportantEar:   C(0xDC, 0x26, 0x26));

    public static void Apply(string theme)
    {
        var p = theme == "light" ? Light : Dark;

        // Substitui o recurso de cada brush por um novo objeto no nível do App.
        // Os consumidores usam DynamicResource, então re-resolvem para o novo brush.
        // (Mutar o .Color do brush compartilhado não funciona ao vivo: o WPF congela
        // os brushes usados em Style/Template selados, e brush congelado é imutável.)
        SetBrush("BgDeep",        p.BgDeep);
        SetBrush("BgPanel",       p.BgPanel);
        SetBrush("BgRow",         p.BgRow);
        SetBrush("BgRowHover",    p.BgRowHover);
        SetBrush("BgRowSelected", p.BgRowSelected);
        SetBrush("BgSection",     p.BgSection);
        SetBrush("BorderSubtle",  p.BorderSubtle);
        SetBrush("TextPrimary",   p.TextPrimary);
        SetBrush("TextSecondary", p.TextSecondary);
        SetBrush("TextMuted",     p.TextMuted);
        SetBrush("Accent",        p.Accent);
        SetBrush("Danger",        p.Danger);
        SetBrush("ImportantEar",  p.ImportantEar);
    }

    private static void SetBrush(string key, Color c)
    {
        var brush = new SolidColorBrush(c);
        brush.Freeze(); // imutável e thread-safe; será trocado inteiro na próxima troca de tema
        Application.Current.Resources[key] = brush;
    }

    private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
