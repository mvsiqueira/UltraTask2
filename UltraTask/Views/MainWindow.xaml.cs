using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

        Services.ThemeService.HighlightImportant = Vm.Settings.HighlightImportant;
        Services.ThemeService.Apply(Vm.Settings.Theme);
        Services.LayoutService.Apply(Vm.Settings.LayoutMode);
        RestoreWindowGeometry();

        Closed += (_, _) => { SaveWindowGeometry(); Vm.SaveNow(); };
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.BatchSelectedCount))
                UpdateBatchCount();
        };
        BuildStamp.Text = GetBuildStamp();
        UpdateStatus();

        // Configura drop na lista
        TaskList.AllowDrop = true;
        TaskList.DragOver += OnListDragOver;
        TaskList.Drop += OnListDrop;
        TaskList.DragLeave += (_, _) => HideDropIndicator();

        if (string.IsNullOrEmpty(Vm.FilePath))
            Dispatcher.BeginInvoke(PromptOpenOrCreate);

        KeyDown += OnGlobalKeyDown;
        PreviewMouseWheel += OnGlobalMouseWheel;
    }

    private static readonly string[] Layouts = ["compact", "normal", "extended"];

    // Debounce do rebuild ao mudar layout via scroll — evita rebuild a cada tick da roda.
    private readonly DispatcherTimer _layoutDebounce = new() { Interval = TimeSpan.FromMilliseconds(150) };

    private void OnGlobalMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        e.Handled = true;

        var current = Array.IndexOf(Layouts, Vm.Settings.LayoutMode);
        if (current < 0) current = 1;
        var next = Math.Clamp(current + (e.Delta > 0 ? -1 : 1), 0, Layouts.Length - 1);
        if (next == current) return;

        var mode = Layouts[next];
        Services.LayoutService.Apply(mode); // atualiza DynamicResources imediatamente
        Vm.Settings.LayoutMode = mode;
        Services.PersistenceService.SaveSettings(Vm.Settings);

        // Rebuild das linhas adiado — evita rebuild a cada tick enquanto o usuário scrolla
        _layoutDebounce.Stop();
        _layoutDebounce.Tick -= OnLayoutDebounced;
        _layoutDebounce.Tick += OnLayoutDebounced;
        _layoutDebounce.Start();
    }

    private void OnLayoutDebounced(object? sender, EventArgs e)
    {
        _layoutDebounce.Stop();
        _layoutDebounce.Tick -= OnLayoutDebounced;
        Vm.LayoutVersion++;
    }

    private void OnGlobalKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F11:
                Services.ThemeService.Apply("light");
                Vm.Settings.Theme = "light";
                Services.PersistenceService.SaveSettings(Vm.Settings);
                e.Handled = true;
                break;
            case Key.F12:
                Services.ThemeService.Apply("dark");
                Vm.Settings.Theme = "dark";
                Services.PersistenceService.SaveSettings(Vm.Settings);
                e.Handled = true;
                break;
            case Key.F5:
                Services.LayoutService.Apply("compact");
                Vm.Settings.LayoutMode = "compact";
                Services.PersistenceService.SaveSettings(Vm.Settings);
                Vm.LayoutVersion++;
                e.Handled = true;
                break;
            case Key.F6:
                Services.LayoutService.Apply("normal");
                Vm.Settings.LayoutMode = "normal";
                Services.PersistenceService.SaveSettings(Vm.Settings);
                Vm.LayoutVersion++;
                e.Handled = true;
                break;
            case Key.F7:
                Services.LayoutService.Apply("extended");
                Vm.Settings.LayoutMode = "extended";
                Services.PersistenceService.SaveSettings(Vm.Settings);
                Vm.LayoutVersion++;
                e.Handled = true;
                break;
        }
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
            Vm.AllItems.ToList().ForEach(i => i.RefreshTags());
            Vm.NotifyItemChanged();
        }, onTagDeleted: tagName =>
        {
            foreach (var item in Vm.AllItems)
                item.Model.Tags.Remove(tagName);
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
            Vm.LayoutVersion++;
            Vm.ScheduleSave();
        }) { Owner = this };
        win.ShowDialog();
    }

    private void OpenAppSettings(object sender, RoutedEventArgs e)
    {
        var win = new AppSettingsWindow(Vm.Settings,
            filePath => { Vm.LoadFile(filePath); UpdateStatus(); },
            () => { Vm.LayoutVersion++; UpdateStatus(); }) { Owner = this };
        win.ShowDialog();
    }

    private void OpenAbout(object sender, RoutedEventArgs e)
    {
        new AboutWindow(GetBuildStamp()) { Owner = this }.ShowDialog();
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
        if (Vm.HasActiveFilter)
            Vm.NotifyItemChanged();
        UpdateStatus();
    }

    private void OnFilterByTag(object sender, string tag)      => Vm.FilterTag      = tag;
    private void OnFilterByContact(object sender, string v)    => Vm.FilterContact  = v;
    private void OnFilterByAssignee(object sender, string v)   => Vm.FilterAssignee = v;

    // ===== Modo lote =====

    private void UpdateBatchCount()
    {
        if (BatchCountText is not null)
            BatchCountText.Text = $"{Vm.BatchSelectedCount} selecionada(s)";
    }

    private void OnBatchSelectAll(object sender, RoutedEventArgs e)
    {
        foreach (var item in Vm.AllItems.Where(i => !i.IsSection))
            item.IsSelected = true;
        UpdateBatchCount();
    }

    private void OnBatchClearSelection(object sender, RoutedEventArgs e)
    {
        Vm.ClearBatchSelection();
        UpdateBatchCount();
    }

    private void OnBatchAddTag(object sender, RoutedEventArgs e)
    {
        var catalog = Vm.TagCatalog;
        if (catalog is null || catalog.Count == 0) return;
        var menu = new ContextMenu();
        foreach (var tag in catalog)
        {
            var item = new MenuItem { Header = tag.Name };
            item.Click += (_, _) => { Vm.BatchAddTag(tag.Name); RefreshBatchResult(); };
            menu.Items.Add(item);
        }
        menu.PlacementTarget = BtnBatchAddTag;
        menu.IsOpen = true;
    }

    private void OnBatchRemoveTag(object sender, RoutedEventArgs e)
    {
        var tags = Vm.SelectedItems
            .SelectMany(i => i.TagNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();
        if (tags.Count == 0) return;
        var menu = new ContextMenu();
        foreach (var tag in tags)
        {
            var item = new MenuItem { Header = tag };
            item.Click += (_, _) => { Vm.BatchRemoveTag(tag); RefreshBatchResult(); };
            menu.Items.Add(item);
        }
        menu.PlacementTarget = BtnBatchRemoveTag;
        menu.IsOpen = true;
    }

    private void OnBatchSetAssignee(object sender, RoutedEventArgs e)
        => ShowRoleMenu(BtnBatchAssignee, Vm.AvailableAssignees,
            v => { Vm.BatchSetAssignee(v); RefreshBatchResult(); });

    private void OnBatchSetContact(object sender, RoutedEventArgs e)
        => ShowRoleMenu(BtnBatchContact, Vm.AvailableContacts,
            v => { Vm.BatchSetContact(v); RefreshBatchResult(); });

    private static void ShowRoleMenu(UIElement anchor, IEnumerable<string> values, Action<string> apply)
    {
        var menu = new ContextMenu();
        foreach (var v in values)
        {
            var val = v;
            var item = new MenuItem { Header = val };
            item.Click += (_, _) => apply(val);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var custom = new MenuItem { Header = "Digitar..." };
        custom.Click += (_, _) =>
        {
            var value = ShowInputDialog("Nome:");
            if (value is not null) apply(value);
        };
        menu.Items.Add(custom);
        var clear = new MenuItem { Header = "Limpar" };
        clear.Click += (_, _) => apply(string.Empty);
        menu.Items.Add(clear);
        menu.PlacementTarget = anchor;
        menu.IsOpen = true;
    }

    private static string? ShowInputDialog(string label)
    {
        var win = new Window
        {
            Title = "UltraTask",
            Width = 360, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            WindowStyle = WindowStyle.ToolWindow,
            ShowInTaskbar = false,
            Background = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["BgDeep"],
        };
        var stack = new StackPanel { Margin = new Thickness(16, 12, 16, 12) };
        var lbl = new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6),
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextMuted"] };
        var box = new TextBox
        {
            Height = 26,
            Background = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["BgPanel"],
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextPrimary"],
            BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["BorderSubtle"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4, 2, 4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        var btns = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var ok = new Button { Content = "OK", Width = 72, IsDefault = true,
            Style = (Style)System.Windows.Application.Current.Resources["AccentButtonStyle"] };
        var cancel = new Button { Content = "Cancelar", Width = 80, IsCancel = true, Margin = new Thickness(6, 0, 0, 0),
            Style = (Style)System.Windows.Application.Current.Resources["NeutralButtonStyle"] };

        string? result = null;
        ok.Click     += (_, _) => { result = box.Text.Trim(); win.DialogResult = true; };
        cancel.Click += (_, _) => win.DialogResult = false;

        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        stack.Children.Add(lbl);
        stack.Children.Add(box);
        stack.Children.Add(btns);
        win.Content = stack;
        win.Owner = System.Windows.Application.Current.MainWindow;

        win.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };
        return win.ShowDialog() == true ? result : null;
    }

    private void OnBatchImportant(object sender, RoutedEventArgs e)
    {
        Vm.BatchSetImportant(true);
        RefreshBatchResult();
    }

    private void OnBatchNotImportant(object sender, RoutedEventArgs e)
    {
        Vm.BatchSetImportant(false);
        RefreshBatchResult();
    }

    private void OnBatchDelete(object sender, RoutedEventArgs e)
    {
        var count = Vm.BatchSelectedCount;
        if (count == 0) return;
        var r = MessageBox.Show($"Excluir {count} tarefa(s) selecionada(s)?", "UltraTask",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;
        Vm.BatchDeleteCommand.Execute(null);
        RefreshBatchResult();
        UpdateStatus();
    }

    private void RefreshBatchResult()
    {
        Vm.LayoutVersion++;   // força rebuild de todas as linhas
        UpdateBatchCount();
    }

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
        StatusText.Text  = total == 1 ? "1 tarefa" : $"{total} tarefas";
        FilePathText.Text = Vm.FilePath;
        var appName  = "UltraTask";
        var listName = Vm.FileTitle;
        Title = (Vm.Settings.TitlebarFormat, string.IsNullOrEmpty(listName)) switch
        {
            ("list",     false) => listName!,
            ("list",     true)  => appName,
            ("app-list", false) => $"{appName} — {listName}",
            ("list-app", false) => $"{listName} — {appName}",
            _                   => appName,
        };
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
