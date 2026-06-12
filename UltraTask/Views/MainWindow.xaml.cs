using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using UltraTask.Controls;
using UltraTask.ViewModels;

namespace UltraTask.Views;

// Code-behind da janela principal — cola eventos de UI ao ViewModel e gerencia DnD.
public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    // Estado de drag-and-drop
    private TaskItemViewModel? _draggingItem;
    private int _dropTargetIndex = -1;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        Closed += (_, _) => Vm.SaveNow();
        BuildStamp.Text = GetBuildStamp();
        UpdateStatus();

        // Configura drop na lista
        TaskList.AllowDrop = true;
        TaskList.DragOver += OnListDragOver;
        TaskList.Drop += OnListDrop;
        TaskList.DragLeave += (_, _) => HideDropIndicator();

        if (string.IsNullOrEmpty(Vm.FilePath))
            Dispatcher.BeginInvoke(PromptOpenOrCreate);
    }

    // ===== Título do arquivo =====

    private void OnFileTitleConfirmed(object sender, RoutedEventArgs e)
    {
        if (Vm.CurrentFile is not null)
            Vm.CurrentFile.Title = Vm.FileTitle;
        Vm.ScheduleSave();
        UpdateStatus();
    }

    // ===== Barra lateral =====

    private void OpenTagManager(object sender, RoutedEventArgs e)
    {
        if (Vm.CurrentFile is null) return;
        var win = new TagManagerWindow(Vm.CurrentFile.TagCatalog, () =>
        {
            Vm.ScheduleSave();
            // Força rebind dos chips na lista
            Vm.AllItems.ToList().ForEach(i => i.RefreshTags());
        }) { Owner = this };
        win.ShowDialog();
        // Atualiza a lista após fechar (ordem das tags pode ter mudado)
        Vm.FilteredItems.Refresh();
    }

    private void OpenRoleManager(object sender, RoutedEventArgs e)
    {
        if (Vm.CurrentFile is null) return;
        var win = new RoleManagerWindow(Vm.CurrentFile.RoleConfig, () =>
        {
            Vm.ScheduleSave();
            Vm.RoleConfigVersion++;
        }) { Owner = this };
        win.ShowDialog();
    }

    private void OpenLinkManager(object sender, RoutedEventArgs e)
    {
        if (Vm.CurrentFile is null) return;
        var win = new LinkManagerWindow(Vm.CurrentFile.LinkCatalog, () =>
        {
            Vm.ScheduleSave();
            Vm.OnPropertyChanged(nameof(Vm.LinkCatalog));
        }) { Owner = this };
        win.ShowDialog();
    }

    private void OpenRowOrderManager(object sender, RoutedEventArgs e)
    {
        if (Vm.CurrentFile is null) return;
        var win = new TaskRowOrderWindow(Vm.CurrentFile.TaskRowOrder, newOrder =>
        {
            Vm.CurrentFile.TaskRowOrder.Clear();
            Vm.CurrentFile.TaskRowOrder.AddRange(newOrder);
            Vm.OnPropertyChanged(nameof(Vm.TaskRowOrder));
            Vm.ScheduleSave();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Selecionar arquivo de tarefas",
            Filter = "JSON (*.json)|*.json|Todos os arquivos|*.*",
            CheckFileExists = false,
        };
        if (dlg.ShowDialog() == true)
        {
            Vm.LoadFile(dlg.FileName);
            UpdateStatus();
        }
    }

    private void OpenAbout(object sender, RoutedEventArgs e)
    {
        var stamp = GetBuildStamp();
        MessageBox.Show(
            $"UltraTask\nPort WPF do UltraTask Python\nC# · .NET 10 · WPF\n\n{stamp}",
            "Sobre", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ===== Eventos dos itens da lista =====

    private void OnDeleteRequested(object sender, EventArgs e)
    {
        if (sender is TaskRowControl ctrl && ctrl.Item is not null)
        {
            var r = MessageBox.Show("Excluir este item?", "UltraTask",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                Vm.DeleteItemCommand.Execute(ctrl.Item);
                UpdateStatus();
            }
        }
    }

    private void OnDuplicateRequested(object sender, EventArgs e)
    {
        if (sender is TaskRowControl ctrl && ctrl.Item is not null)
        {
            Vm.DuplicateItem(ctrl.Item);
            UpdateStatus();
        }
    }

    private void OnItemChanged(object sender, EventArgs e)
    {
        Vm.ScheduleSave();
        UpdateStatus();
    }

    private void OnFilterByTag(object sender, string tag)      => Vm.FilterTag      = tag;
    private void OnFilterByContact(object sender, string v)    => Vm.FilterContact  = v;
    private void OnFilterByAssignee(object sender, string v)   => Vm.FilterAssignee = v;

    // O TaskRowControl dispara DragStarted quando o usuário pressiona o grip.
    private void OnDragStarted(object sender, EventArgs e)
    {
        if (sender is TaskRowControl ctrl && ctrl.Item is not null)
        {
            _draggingItem = ctrl.Item;
            DragDrop.DoDragDrop(ctrl, new DataObject("UltraTaskItem", ctrl.Item), DragDropEffects.Move);
            _draggingItem = null;
            HideDropIndicator();
        }
    }

    // ===== Drag-and-drop na lista =====

    private void OnListDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("UltraTaskItem") || _draggingItem is null)
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        var idx = GetDropIndex(e.GetPosition(TaskList));
        _dropTargetIndex = idx;
        ShowDropIndicator(idx, e.GetPosition(MainScroll));
    }

    private void OnListDrop(object sender, DragEventArgs e)
    {
        HideDropIndicator();
        if (_draggingItem is null) return;

        var fromIdx = Vm.AllItems.IndexOf(_draggingItem);
        var toIdx = _dropTargetIndex;
        if (fromIdx < 0 || toIdx < 0) return;

        // Ajusta índice quando movendo para baixo
        if (toIdx > fromIdx) toIdx--;
        if (fromIdx != toIdx)
            Vm.MoveItem(fromIdx, toIdx);
    }

    // Calcula o índice de inserção com base na posição Y do mouse na lista.
    private int GetDropIndex(Point posInList)
    {
        for (int i = 0; i < TaskList.Items.Count; i++)
        {
            var container = TaskList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            if (container is null) continue;
            var bounds = container.TransformToAncestor(TaskList)
                                  .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
            if (posInList.Y < bounds.Top + bounds.Height / 2)
                return i;
        }
        return TaskList.Items.Count;
    }

    // Mostra linha horizontal de drop no canvas de overlay.
    private void ShowDropIndicator(int index, Point posInScroll)
    {
        if (index < 0 || index > TaskList.Items.Count)
        {
            HideDropIndicator();
            return;
        }
        DropIndicator.Visibility = Visibility.Visible;
        // Posiciona a linha na altura correta dentro do ScrollViewer
        double y = posInScroll.Y;
        Canvas.SetTop(DropIndicator, y - 1);
    }

    private void HideDropIndicator()
    {
        DropIndicator.Visibility = Visibility.Collapsed;
        _dropTargetIndex = -1;
    }

    // ===== Status bar =====

    private void UpdateStatus()
    {
        var total = Vm.AllItems.Count(i => !i.IsSection);
        var done  = Vm.AllItems.Count(i => !i.IsSection && i.Completed);
        StatusText.Text  = $"{total} tarefas · {done} concluídas";
        FilePathText.Text = Vm.FilePath;
        Title = string.IsNullOrEmpty(Vm.FileTitle)
            ? "UltraTask"
            : $"UltraTask — {Vm.FileTitle}";
    }

    // ===== Abrir / criar arquivo =====

    private void PromptOpenOrCreate()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Abrir arquivo de tarefas (cancele para criar novo)",
            Filter = "JSON (*.json)|*.json|Todos os arquivos|*.*",
            CheckFileExists = false,
        };
        if (dlg.ShowDialog() == true)
        {
            Vm.LoadFile(dlg.FileName);
        }
        else
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "UltraTask.json");
            Vm.LoadFile(path);
        }
        UpdateStatus();
    }

    // ===== Timestamp de build =====

    private static string GetBuildStamp()
    {
        try
        {
            // Em single-file Assembly.Location é vazio — usa ProcessPath como fallback.
            var exe = Environment.ProcessPath ?? string.Empty;
#pragma warning disable IL3000
            if (string.IsNullOrEmpty(exe))
                exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
#pragma warning restore IL3000
            if (System.IO.File.Exists(exe))
                return $"build {System.IO.File.GetLastWriteTime(exe):yyyy-MM-dd HH:mm}";
        }
        catch { }
        return string.Empty;
    }
}
