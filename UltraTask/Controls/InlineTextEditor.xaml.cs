using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace UltraTask.Controls;

// Controle de edição inline: alterna entre TextBlock (visualização) e TextBox (edição).
// Ativa edição com duplo-clique; confirma com Enter ou perda de foco; cancela com Escape.
public partial class InlineTextEditor : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(InlineTextEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public event RoutedEventHandler? EditConfirmed;

    private string _textBeforeEdit = string.Empty;
    // Guarda evita duplo disparo: Enter → EndEdit → Visibility=Collapsed → LostFocus → EndEdit novamente.
    private bool _editing = false;

    public InlineTextEditor()
    {
        InitializeComponent();
        // TextBlock não expõe MouseDoubleClick em XAML — registra via código.
        ViewBlock.MouseLeftButtonDown += OnViewDoubleClick;
    }

    // Substitui o texto do ViewBlock por inlines customizados (ex: Hyperlinks).
    public void PopulateInlines(IEnumerable<Inline> inlines)
    {
        BindingOperations.ClearBinding(ViewBlock, TextBlock.TextProperty);
        ViewBlock.Inlines.Clear();
        foreach (var inline in inlines)
            ViewBlock.Inlines.Add(inline);
    }

    // Restaura o binding padrão de Text (quando não há links a renderizar).
    public void RestoreTextBinding()
    {
        var binding = new Binding(nameof(Text))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(InlineTextEditor), 1),
        };
        ViewBlock.SetBinding(TextBlock.TextProperty, binding);
    }

    public void BeginEdit()
    {
        if (_editing) return;
        _editing = true;
        _textBeforeEdit = Text;
        ViewBlock.Visibility = Visibility.Collapsed;
        EditBox.Visibility = Visibility.Visible;
        EditBox.Focus();
        EditBox.SelectAll();
    }

    private void EndEdit(bool confirm)
    {
        if (!_editing) return;
        _editing = false;

        if (confirm)
        {
            // Sincroniza Text antes de disparar EditConfirmed (caso o binding não tenha atualizado ainda)
            if (!string.IsNullOrEmpty(EditBox.Text))
                Text = EditBox.Text;
            EditConfirmed?.Invoke(this, new RoutedEventArgs());
        }
        else
        {
            // Reseta EditBox antes de colapsar — evita que o binding two-way
            // empurre o texto editado para editor.Text quando LostFocus disparar.
            EditBox.Text = _textBeforeEdit;
            Text = _textBeforeEdit;
        }

        EditBox.Visibility = Visibility.Collapsed;
        ViewBlock.Visibility = Visibility.Visible;
    }

    // Duplo-clique no ViewBlock abre edição.
    private void OnViewDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            BeginEdit();
            e.Handled = true;
        }
    }

    private void OnEditKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { EndEdit(true); e.Handled = true; }
        else if (e.Key == Key.Escape) { EndEdit(false); e.Handled = true; }
    }

    private void OnEditLostFocus(object sender, RoutedEventArgs e) => EndEdit(true);
}
