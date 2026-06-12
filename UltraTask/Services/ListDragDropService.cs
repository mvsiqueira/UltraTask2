using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UltraTask.ViewModels;

namespace UltraTask.Services;

// Gerencia o drag-and-drop de itens em um ItemsControl.
// Uso: chamar Attach(itemsControl, onMove) após o controle ser carregado.
public class ListDragDropService
{
    private readonly ItemsControl _list;
    private readonly Action<int, int> _onMove; // (fromIndex, toIndex)

    private TaskItemViewModel? _dragging;
    private Border? _dropIndicator;
    private int _dropTargetIndex = -1;

    public ListDragDropService(ItemsControl list, Action<int, int> onMove)
    {
        _list = list;
        _onMove = onMove;

        _list.AllowDrop = true;
        _list.DragOver += OnDragOver;
        _list.Drop += OnDrop;
        _list.DragLeave += OnDragLeave;
    }

    // Chamado pelo TaskRowControl quando o usuário começa a arrastar.
    public void BeginDrag(TaskItemViewModel item, DependencyObject source)
    {
        _dragging = item;
        DragDrop.DoDragDrop(source, new DataObject("UltraTaskItem", item), DragDropEffects.Move);
        _dragging = null;
        RemoveIndicator();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("UltraTaskItem"))
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        var index = GetDropIndex(e.GetPosition(_list));
        if (index != _dropTargetIndex)
        {
            _dropTargetIndex = index;
            MoveOrUpdateIndicator(index);
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        RemoveIndicator();
        if (_dragging is null) return;

        var fromIndex = FindIndex(_dragging);
        var toIndex = _dropTargetIndex;
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex) return;

        // Ajuste: se arrastamos para baixo, o índice alvo já considera o item removido.
        if (toIndex > fromIndex) toIndex--;
        _onMove(fromIndex, toIndex);
    }

    private void OnDragLeave(object sender, DragEventArgs e) => RemoveIndicator();

    // Determina o índice de inserção baseado na posição Y do mouse.
    private int GetDropIndex(Point pos)
    {
        var panel = GetItemsPanel();
        if (panel is null) return _list.Items.Count;

        for (int i = 0; i < _list.Items.Count; i++)
        {
            var container = _list.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            if (container is null) continue;

            var bounds = container.TransformToAncestor(_list).TransformBounds(
                new Rect(0, 0, container.ActualWidth, container.ActualHeight));

            if (pos.Y < bounds.Top + bounds.Height / 2)
                return i;
        }

        return _list.Items.Count;
    }

    // Mostra uma linha horizontal indicando onde o item será solto.
    private void MoveOrUpdateIndicator(int index)
    {
        RemoveIndicator();

        var panel = GetItemsPanel();
        if (panel is null) return;

        _dropIndicator = new Border
        {
            Height = 2,
            Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)), // Accent
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        // Insere o indicador como adorno — usa o AdornerLayer da janela.
        // Simplificação: insere diretamente no painel do ItemsControl via attached behavior.
        // Na prática, vamos usar a técnica de overlay via Canvas no MainWindow.
    }

    private void RemoveIndicator()
    {
        _dropIndicator = null;
        _dropTargetIndex = -1;
    }

    private int FindIndex(TaskItemViewModel item)
    {
        for (int i = 0; i < _list.Items.Count; i++)
        {
            if (_list.Items[i] is TaskItemViewModel vm && ReferenceEquals(vm, item))
                return i;
        }
        return -1;
    }

    private Panel? GetItemsPanel()
    {
        if (_list.Items.Count == 0) return null;
        var container = _list.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
        return container?.Parent as Panel;
    }
}
