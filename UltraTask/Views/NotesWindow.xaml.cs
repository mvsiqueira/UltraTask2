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
            Editor.Document = HtmlFlowConverter.ToFlowDocument(initialHtml);

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
        {
            sel.ApplyPropertyValue(TextElement.ForegroundProperty,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentTextColor)));
        }
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
        {
            sel.ApplyPropertyValue(TextElement.BackgroundProperty,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentBgColor)));
        }
        Editor.Focus();
    }

    // ===== Checklist =====

    private void OnClearFormatting(object sender, RoutedEventArgs e)
    {
        var sel = Editor.Selection;
        if (sel.IsEmpty) return;
        sel.ApplyPropertyValue(TextElement.FontWeightProperty,  FontWeights.Normal);
        sel.ApplyPropertyValue(TextElement.FontStyleProperty,   FontStyles.Normal);
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty,  new TextDecorationCollection());
        sel.ApplyPropertyValue(TextElement.ForegroundProperty,  Editor.Foreground);
        sel.ApplyPropertyValue(TextElement.BackgroundProperty,  Brushes.Transparent);
        Editor.Focus();
        UpdateToolbarState();
    }

    private void OnInsertCheckbox(object sender, RoutedEventArgs e)
    {
        Editor.Focus();
        var caretPara = Editor.CaretPosition.Paragraph;
        if (caretPara is null) return;

        var run = new Run("☐ ") { FontSize = 16 };
        caretPara.Inlines.Add(run);
        Editor.CaretPosition = run.ContentEnd;
    }

    // Clique no editor: se clicou em ☐ ou ☒, alterna estado.
    private void OnEditorPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = Editor.GetPositionFromPoint(e.GetPosition(Editor), true);
        if (pos is null) return;

        var range = new TextRange(pos, pos.GetPositionAtOffset(1) ?? pos);
        var ch = range.Text;

        if (ch == "☐" || ch == "☒")
        {
            e.Handled = true;
            range.Text = ch == "☐" ? "☒" : "☐";
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

    // ===== Estado da toolbar (atualiza ao mover cursor) =====

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

    // ===== Ações do rodapé =====

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
