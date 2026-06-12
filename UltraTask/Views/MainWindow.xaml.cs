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

        Services.LayoutService.Apply(Vm.Settings.LayoutMode);
        RestoreWindowGeometry();

        Closed += (_, _) => { SaveWindowGeometry(); Vm.SaveNow(); };
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

    // ===== Geometria da janela =====

    private void RestoreWindowGeometry()
    {
        var s = Vm.Settings;

        if (s.WindowState == "Maximized")
        {
            WindowState = System.Windows.WindowState.Maximized;
            return;
        }

        // Valida posição dentro dos limites da tela virtual (multi-monitor)
        if (s.WindowLeft.HasValue && s.WindowTop.HasValue)
        {
            var vl = SystemParameters.VirtualScreenLeft;
            var vt = SystemParameters.VirtualScreenTop;
            var vr = vl + SystemParameters.VirtualScreenWidth;
            var vb = vt + SystemParameters.VirtualScreenHeight;

            if (s.WindowLeft.Value >= vl && s.WindowLeft.Value + s.WindowWidth <= vr &&
                s.WindowTop.Value  >= vt && s.WindowTop.Value  + s.WindowHeight <= vb)
            {
                Left = s.WindowLeft.Value;
                Top  = s.WindowTop.Value;
            }
        }
    }

    private void SaveWindowGeometry()
    {
        var s = Vm.Settings;
        s.WindowState = WindowState == System.Windows.WindowState.Maximized ? "Maximized" : "Normal";
        if (WindowState == System.Windows.WindowState.Normal)
        {
            s.WindowWidth  = Width;
            s.WindowHeight = Height;
            s.WindowLeft   = Left;
            s.WindowTop    = Top;
        }
        Services.PersistenceService.SaveSettings(s);
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

    private void OpenFileProperties(object sender, RoutedEventArgs e)
    {
        if (Vm.CurrentFile is null) return;
        var win = new FilePropertiesWindow(Vm.CurrentFile, () =>
        {
            Vm.OnPropertyChanged(nameof(Vm.TaskRowOrder));
            Vm.ScheduleSave();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void OpenAppSettings(object sender, RoutedEventArgs e)
    {
        var win = new AppSettingsWindow(Vm.Settings,
            filePath => { Vm.LoadFile(filePath); UpdateStatus(); },
            () => Vm.LayoutVersion++) { Owner = this };
        win.ShowDialog();
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
        SnapDropIndicator(idx);
    }

    private void OnListDrop(object sender, DragEventArgs e)
    {
        var savedDropIndex = _dropTargetIndex; // salva antes de HideDropIndicator zerar
        HideDropIndicator();
        if (_draggingItem is null) return;

        // Usa FilteredItems como referência para ambos os índices
        var filtered = Vm.FilteredItems.Cast<TaskItemViewModel>().ToList();
        var fromFiltered = filtered.IndexOf(_draggingItem);
        var toFiltered = savedDropIndex;
        if (fromFiltered < 0 || toFiltered < 0) return;

        // Ajusta índice de destino após remoção da origem
        if (toFiltered > fromFiltered) toFiltered--;
        if (fromFiltered == toFiltered) return;

        // Converte para índices em AllItems (que preserva a mesma ordem quando sem filtro)
        var fromAll = Vm.AllItems.IndexOf(_draggingItem);
        var toItem  = toFiltered < filtered.Count ? filtered[toFiltered] : null;
        var toAll   = toItem is null ? Vm.AllItems.Count - 1 : Vm.AllItems.IndexOf(toItem);

        Vm.MoveItem(fromAll, toAll);
    }

    // Calcula o índice de inserção com base na posição Y do mouse relativa ao TaskList.
    private int GetDropIndex(Point posInList)
    {
        for (int i = 0; i < TaskList.Items.Count; i++)
        {
            if (TaskList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container) continue;
            var top = container.TranslatePoint(new Point(0, 0), TaskList).Y;
            if (posInList.Y < top + container.ActualHeight / 2)
                return i;
        }
        return TaskList.Items.Count;
    }

    // Posiciona o indicador de drop snapped ao topo do item alvo.
    private void SnapDropIndicator(int dropIndex)
    {
        if (dropIndex < 0 || dropIndex > TaskList.Items.Count) { HideDropIndicator(); return; }

        FrameworkElement? container;
        double yInContainer;

        if (dropIndex < TaskList.Items.Count)
        {
            container = TaskList.ItemContainerGenerator.ContainerFromIndex(dropIndex) as FrameworkElement;
            yInContainer = 0;
        }
        else
        {
            container = TaskList.ItemContainerGenerator.ContainerFromIndex(dropIndex - 1) as FrameworkElement;
            yInContainer = container?.ActualHeight ?? 0;
        }

        if (container is null) { HideDropIndicator(); return; }

        var y = container.TranslatePoint(new Point(0, yInContainer), OverlayCanvas).Y;
        DropIndicator.Visibility = Visibility.Visible;
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
