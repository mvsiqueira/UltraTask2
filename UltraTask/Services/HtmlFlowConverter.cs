using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace UltraTask.Services;

// Converte entre o HTML simples usado no notes_rich e FlowDocument do WPF.
// Suporte: <b>, <i>, <u>, <s>, <span style="color:#hex">, <span style="background:#hex">, \n como quebra de linha.
public static partial class HtmlFlowConverter
{
    // ===== HTML → FlowDocument =====

    public static FlowDocument ToFlowDocument(string html)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            PagePadding = new Thickness(0),
            LineHeight = 18,
        };

        // Normaliza quebras de linha
        var lines = html.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        foreach (var line in lines)
        {
            var para = new Paragraph { Margin = new Thickness(0), Padding = new Thickness(0) };
            ParseInlines(line, para.Inlines);
            doc.Blocks.Add(para);
        }

        return doc;
    }

    // Parseia uma linha HTML e adiciona Inlines no container.
    private static void ParseInlines(string html, InlineCollection target)
    {
        int pos = 0;
        var stack = new Stack<InlineState>();
        var current = new InlineState();

        while (pos < html.Length)
        {
            int tagStart = html.IndexOf('<', pos);
            if (tagStart < 0)
            {
                // Restante é texto puro
                AppendText(html[pos..], current, target);
                break;
            }

            // Texto antes da tag
            if (tagStart > pos)
                AppendText(html[pos..tagStart], current, target);

            int tagEnd = html.IndexOf('>', tagStart);
            if (tagEnd < 0)
            {
                AppendText(html[tagStart..], current, target);
                break;
            }

            var tag = html[(tagStart + 1)..tagEnd].Trim();
            pos = tagEnd + 1;

            if (tag.StartsWith('/'))
            {
                // Fecha tag — restaura estado anterior
                if (stack.Count > 0)
                    current = stack.Pop();
            }
            else
            {
                stack.Push(current.Clone());

                var tagLower = tag.ToLowerInvariant();
                current = current.Clone();

                if (tagLower == "b" || tagLower == "strong")
                    current.Bold = true;
                else if (tagLower == "i" || tagLower == "em")
                    current.Italic = true;
                else if (tagLower == "u")
                    current.Underline = true;
                else if (tagLower == "s" || tagLower == "strike" || tagLower == "del")
                    current.Strikethrough = true;
                else if (tagLower.StartsWith("span"))
                    ApplySpanStyle(tag, current);
                // Tags desconhecidas: ignora, mantém estado
            }
        }
    }

    private static void AppendText(string text, InlineState state, InlineCollection target)
    {
        if (string.IsNullOrEmpty(text)) return;

        var run = new Run(text);

        if (state.Bold) run.FontWeight = FontWeights.Bold;
        if (state.Italic) run.FontStyle = FontStyles.Italic;

        var decorations = new TextDecorationCollection();
        if (state.Underline) decorations.Add(TextDecorations.Underline[0]);
        if (state.Strikethrough) decorations.Add(TextDecorations.Strikethrough[0]);
        if (decorations.Count > 0) run.TextDecorations = decorations;

        if (state.ForeColor is not null)
            run.Foreground = new SolidColorBrush(state.ForeColor.Value);
        if (state.BackColor is not null)
            run.Background = new SolidColorBrush(state.BackColor.Value);

        target.Add(run);
    }

    private static void ApplySpanStyle(string tag, InlineState state)
    {
        // Extrai style="..."
        var styleMatch = StyleAttrRegex().Match(tag);
        if (!styleMatch.Success) return;

        var style = styleMatch.Groups[1].Value;

        var colorMatch = ColorPropRegex().Match(style);
        if (colorMatch.Success)
            state.ForeColor = TryParseColor(colorMatch.Groups[1].Value);

        var bgMatch = BgPropRegex().Match(style);
        if (bgMatch.Success)
            state.BackColor = TryParseColor(bgMatch.Groups[1].Value);
    }

    private static Color? TryParseColor(string hex)
    {
        try { return (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex); }
        catch { return null; }
    }

    // ===== FlowDocument → HTML =====

    public static string ToHtml(FlowDocument doc)
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (var block in doc.Blocks)
        {
            if (!first) sb.Append('\n');
            first = false;

            if (block is Paragraph para)
                sb.Append(SerializeParagraph(para));
        }

        return sb.ToString();
    }

    private static string SerializeParagraph(Paragraph para)
    {
        var sb = new StringBuilder();

        foreach (var inline in para.Inlines)
        {
            if (inline is Run run)
                sb.Append(SerializeRun(run));
            else if (inline is Span span)
                sb.Append(SerializeSpan(span));
        }

        return sb.ToString();
    }

    private static string SerializeRun(Run run)
    {
        var text = run.Text;
        if (string.IsNullOrEmpty(text)) return string.Empty;

        text = EscapeHtml(text);
        text = WrapFormatting(text, run);
        return text;
    }

    private static string SerializeSpan(Span span)
    {
        var inner = new StringBuilder();
        foreach (var inline in span.Inlines)
        {
            if (inline is Run r) inner.Append(SerializeRun(r));
            else if (inline is Span s) inner.Append(SerializeSpan(s));
        }
        return inner.ToString();
    }

    private static string WrapFormatting(string text, TextElement el)
    {
        // Cor de texto
        if (el.Foreground is SolidColorBrush fg && fg.Color != Colors.White && fg.Color != Colors.Transparent)
            text = $"<span style=\"color:{ColorToHex(fg.Color)}\">{text}</span>";

        // Cor de fundo
        if (el.Background is SolidColorBrush bg && bg.Color != Colors.Transparent)
            text = $"<span style=\"background:{ColorToHex(bg.Color)}\">{text}</span>";

        if (el is Run run)
        {
            // Decorações
            var decs = run.TextDecorations;
            bool hasStrike = decs?.Any(d => d.Location == TextDecorationLocation.Strikethrough) == true;
            bool hasUnder = decs?.Any(d => d.Location == TextDecorationLocation.Underline) == true;
            if (hasStrike) text = $"<s>{text}</s>";
            if (hasUnder) text = $"<u>{text}</u>";

            if (run.FontStyle == FontStyles.Italic) text = $"<i>{text}</i>";
            if (run.FontWeight == FontWeights.Bold) text = $"<b>{text}</b>";
        }

        return text;
    }

    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string ColorToHex(Color c) =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // ===== Suporte — estado de formatação inline =====

    private sealed class InlineState
    {
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public bool Strikethrough;
        public Color? ForeColor;
        public Color? BackColor;

        public InlineState Clone() => (InlineState)MemberwiseClone();
    }

    // ===== Regex compiladas =====

    [GeneratedRegex(@"style=""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex StyleAttrRegex();

    [GeneratedRegex(@"\bcolor\s*:\s*(#[0-9a-fA-F]+|[a-zA-Z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ColorPropRegex();

    [GeneratedRegex(@"\bbackground(?:-color)?\s*:\s*(#[0-9a-fA-F]+|[a-zA-Z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex BgPropRegex();
}
