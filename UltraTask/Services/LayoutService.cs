using System.Windows;

namespace UltraTask.Services;

public static class LayoutService
{
    private record LayoutValues(
        double RowHeight, double SectionHeight,
        double FontSizeBase, double FontSizeSmall,
        Thickness RowSpacing, double TokenSpacing);

    private static readonly Dictionary<string, LayoutValues> Presets = new()
    {
        ["compact"]  = new(26, 36, 12, 11, new Thickness(8, 0, 8, 3), 3),
        ["normal"]   = new(34, 44, 13, 12, new Thickness(8, 0, 8, 6), 5),
        ["extended"] = new(46, 54, 15, 14, new Thickness(8, 0, 8, 8), 8),
    };

    public static void Apply(string mode)
    {
        if (!Presets.TryGetValue(mode, out var v))
            v = Presets["compact"];

        var res = Application.Current.Resources;
        res["RowHeight"]     = v.RowHeight;
        res["SectionHeight"] = v.SectionHeight;
        res["FontSizeBase"]  = v.FontSizeBase;
        res["FontSizeSmall"] = v.FontSizeSmall;
        res["RowSpacing"]    = v.RowSpacing;
        res["TokenSpacing"]  = v.TokenSpacing;
    }
}
