using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using UltraTask.Services;

namespace UltraTask.Views;

// Janela de edição de notas ricas com toolbar de formatação.
public partial class NotesWindow : Window
{
    private readonly Action<string> _onSave;
    private string _currentTextColor = "#EF4444";
    private string _currentBgColor  = "#F59E0B";

    public NotesWindow(string? initialHtml, Action<string> onSave)
    {
        InitializeComponent();
        _onSave = onSave;

        if (!string.IsNullOrWhiteSpace(initialHtml))
        {
            Editor.Document = HtmlFlowConverter.ToFlowDocument(initialHtml);
            // Re-aplica riscado às linhas ☒ caso o HTML não tenha <s>
            Dispatcher.BeginInvoke(ReapplyCheckboxStrikethroughs,
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        Editor.Focus();
    }

    // ===== Formatação básica =====

    private void OnBold(object sender, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.ToggleBold.Execute(null, Editor);
    }

    private void OnItalic(object sender, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.ToggleItalic.Execute(null, Editor);
    }

    private void OnUnderline(object sender, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.ToggleUnderline.Execute(null, Editor);
    }

    private void OnStrikethrough(object sender, RoutedEventArgs e)
    {
        Editor.Focus();
        var sel = Editor.Selection;
        if (sel.IsEmpty) return;

        var current = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
        bool hasStrike = current?.Any(d => d.Location == TextDecorationLocation.Strikethrough) == true;

        var decorations = new TextDecorationCollection();
        if (current is not null)
            foreach (var d in current)
                if (d.Location != TextDecorationLocation.Strikethrough)
                    decorations.Add(d);

        if (!hasStrike)
            decorations.Add(TextDecorations.Strikethrough[0]);

        sel.ApplyPropertyValue(Inline.TextDecorationsProperty, decorations);

        // Garante que linhas ☒ mantêm o riscado de checkbox (independente do botão S)
        ReapplyCheckboxStrikethroughs();
        UpdateToolbarState();
    }

    // ===== Cores =====

    private void OnPickTextColor(object sender, RoutedEventArgs e)
    {
        var picked = ColorPickerDialog.Pick(_currentTextColor, this);
        if (picked is null) return;

        _currentTextColor = picked;
        try { TextColorBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentTextColor)); }
        catch { }

        var sel = Editor.Selection;
        if (!sel.IsEmpty)
            sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentTextColor)));
        Editor.Focus();
    }

    private void OnPickBgColor(object sender, RoutedEventArgs e)
    {
        var picked = ColorPickerDialog.Pick(_currentBgColor, this);
        if (picked is null) return;

        _currentBgColor = picked;
        try { BgColorBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentBgColor)); }
        catch { }

        var sel = Editor.Selection;
        if (!sel.IsEmpty)
            sel.ApplyPropertyValue(TextElement.BackgroundProperty,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentBgColor)));
        Editor.Focus();
    }

    // ===== Eraser =====

    private void OnClearFormatting(object sender, RoutedEventArgs e)
    {
        var sel = Editor.Selection;
        if (sel.IsEmpty) return;
        sel.ApplyPropertyValue(TextElement.FontWeightProperty,  FontWeights.Normal);
        sel.ApplyPropertyValue(TextElement.FontStyleProperty,   FontStyles.Normal);
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty,  new TextDecorationCollection());
        sel.ApplyPropertyValue(TextElement.ForegroundProperty,  Editor.Foreground);
        sel.ApplyPropertyValue(TextElement.BackgroundProperty,  Brushes.Transparent);

        // Garante que linhas ☒ mantêm o riscado de checkbox após "limpar formatação"
        ReapplyCheckboxStrikethroughs();
        Editor.Focus();
        UpdateToolbarState();
    }

    // ===== Checklist =====

    private void OnInsertCheckbox(object sender, RoutedEventArgs e)
    {
        Editor.Focus();
        var caretPara = Editor.CaretPosition.Paragraph;
        if (caretPara is null) return;

        // Se já há checkbox no início, remove (e o riscado junto)
        if (caretPara.Inlines.FirstInline is Run first &&
            first.Text.Length > 0 && (first.Text[0] == '☐' || first.Text[0] == '☒'))
        {
            ApplyCheckboxStrikethrough(caretPara, false);
            var remainder = first.Text.TrimStart('☐', '☒').TrimStart(' ');
            if (remainder.Length == 0)
                caretPara.Inlines.Remove(first);
            else
                first.Text = remainder;
            return;
        }

        // Insere no início da linha
        var newRun = new Run("☐ ") { FontSize = HtmlFlowConverter.CheckboxFontSize };
        if (caretPara.Inlines.FirstInline is not null)
            caretPara.Inlines.InsertBefore(caretPara.Inlines.FirstInline, newRun);
        else
            caretPara.Inlines.Add(newRun);
        Editor.CaretPosition = newRun.ContentEnd;
    }

    // Clique no editor: se clicou em ☐ ou ☒, alterna estado e aplica/remove riscado na linha.
    // O riscado de checkbox é imune ao botão S e ao Eraser.
    private void OnEditorPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = Editor.GetPositionFromPoint(e.GetPosition(Editor), true);
        if (pos is null) return;

        var range = new TextRange(pos, pos.GetPositionAtOffset(1) ?? pos);
        var ch = range.Text;

        if (ch == "☐" || ch == "☒")
        {
            e.Handled = true;
            bool nowChecked = ch == "☐";
            var para = pos.Paragraph;
            range.Text = nowChecked ? "☒" : "☐";
            if (para is not null)
                ApplyCheckboxStrikethrough(para, nowChecked);
        }
    }

    // Cursor pointer ao passar sobre ☐/☒.
    private void OnEditorMouseMove(object sender, MouseEventArgs e)
    {
        var pos = Editor.GetPositionFromPoint(e.GetPosition(Editor), false);
        if (pos is not null)
        {
            var range = new TextRange(pos, pos.GetPositionAtOffset(1) ?? pos);
            Editor.Cursor = (range.Text == "☐" || range.Text == "☒")
                ? Cursors.Hand
                : Cursors.IBeam;
        }
        else
        {
            Editor.Cursor = Cursors.IBeam;
        }
    }

    // ===== Helpers de checkbox =====

    // True se o parágrafo começa com ☒.
    private static bool IsCheckedParagraph(Paragraph p)
        => p.Inlines.FirstInline is Run r && r.Text.Length > 0 && r.Text[0] == '☒';

    // Aplica ou remove riscado em todos os runs do parágrafo, pulando o próprio ☐/☒.
    private static void ApplyCheckboxStrikethrough(Paragraph para, bool apply)
    {
        foreach (var inline in para.Inlines)
            ApplyStrikethroughToInline(inline, apply);
    }

    private static void ApplyStrikethroughToInline(Inline inline, bool apply)
    {
        if (inline is Run run)
        {
            // Nunca risca o caracter de checkbox em si
            if (run.Text.Length > 0 && (run.Text[0] == '☐' || run.Text[0] == '☒'))
                return;

            var kept = new TextDecorationCollection(
                (run.TextDecorations ?? []).Where(d => d.Location != TextDecorationLocation.Strikethrough));
            if (apply) kept.Add(TextDecorations.Strikethrough[0]);
            run.TextDecorations = kept.Count > 0 ? kept : null;
        }
        else if (inline is Span span)
        {
            foreach (var child in span.Inlines)
                ApplyStrikethroughToInline(child, apply);
        }
    }

    // Re-aplica riscado a TODAS as linhas ☒ do documento.
    // Chamado após botão S e Eraser para tornar o efeito imune à formatação do usuário.
    private void ReapplyCheckboxStrikethroughs()
    {
        foreach (var block in Editor.Document.Blocks)
            if (block is Paragraph p && IsCheckedParagraph(p))
                ApplyCheckboxStrikethrough(p, true);
    }

    // ===== Estado da toolbar =====

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
        => UpdateToolbarState();

    private void UpdateToolbarState()
    {
        var sel = Editor.Selection;

        BtnBold.IsChecked = sel.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight fw
            && fw == FontWeights.Bold;

        BtnItalic.IsChecked = sel.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle fs
            && fs == FontStyles.Italic;

        var decs = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
        BtnUnderline.IsChecked = decs?.Any(d => d.Location == TextDecorationLocation.Underline) == true;
        BtnStrike.IsChecked    = decs?.Any(d => d.Location == TextDecorationLocation.Strikethrough) == true;
    }

    // ===== Rodapé =====

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var html = HtmlFlowConverter.ToHtml(Editor.Document);
        _onSave(html);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnClear(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Apagar todas as notas?", "UltraTask",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Editor.Document = new FlowDocument();
        }
    }
}
