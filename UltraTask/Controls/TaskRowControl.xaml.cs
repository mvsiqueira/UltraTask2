using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using UltraTask.Models;
using UltraTask.Services;
using UltraTask.ViewModels;

namespace UltraTask.Controls;

// Controle da linha de tarefa — monta os tokens da área de conteúdo
// conforme task_row_order. Seções têm aparência distinta.
public partial class TaskRowControl : UserControl
{
    // --- Dependency Properties ---

    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(TaskItemViewModel), typeof(TaskRowControl),
            new PropertyMetadata(null, (d, e) =>
            {
                var ctrl = (TaskRowControl)d;
                if (e.OldValue is TaskItemViewModel old)
                    old.PropertyChanged -= ctrl.OnItemPropertyChanged;
                if (e.NewValue is TaskItemViewModel next)
                    next.PropertyChanged += ctrl.OnItemPropertyChanged;
                ctrl.Rebuild();
            }));

    public static readonly DependencyProperty TaskRowOrderProperty =
        DependencyProperty.Register(nameof(TaskRowOrder), typeof(IReadOnlyList<string>), typeof(TaskRowControl),
            new PropertyMetadata(null, (d, _) => ((TaskRowControl)d).Rebuild()));

    public static readonly DependencyProperty RoleConfigProperty =
        DependencyProperty.Register(nameof(RoleConfig), typeof(RoleConfig), typeof(TaskRowControl),
            new PropertyMetadata(null, (d, _) => ((TaskRowControl)d).Rebuild()));

    public static readonly DependencyProperty TagCatalogProperty =
        DependencyProperty.Register(nameof(TagCatalog), typeof(IReadOnlyList<TagEntry>), typeof(TaskRowControl),
            new PropertyMetadata(null, (d, _) => ((TaskRowControl)d).Rebuild()));

    public static readonly DependencyProperty LinkCatalogProperty =
        DependencyProperty.Register(nameof(LinkCatalog), typeof(IReadOnlyList<LinkRule>), typeof(TaskRowControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty BatchModeActiveProperty =
        DependencyProperty.Register(nameof(BatchModeActive), typeof(bool), typeof(TaskRowControl),
            new PropertyMetadata(false, (d, e) => ((TaskRowControl)d).OnBatchModeChanged((bool)e.NewValue)));

    public static readonly DependencyProperty RoleConfigVersionProperty =
        DependencyProperty.Register(nameof(RoleConfigVersion), typeof(int), typeof(TaskRowControl),
            new PropertyMetadata(0, (d, _) => ((TaskRowControl)d).Rebuild()));

    public static readonly DependencyProperty LayoutVersionProperty =
        DependencyProperty.Register(nameof(LayoutVersion), typeof(int), typeof(TaskRowControl),
            new PropertyMetadata(0, (d, _) => ((TaskRowControl)d).Rebuild()));

    public TaskItemViewModel? Item        { get => (TaskItemViewModel?)GetValue(ItemProperty);        set => SetValue(ItemProperty, value); }
    public IReadOnlyList<string>? TaskRowOrder { get => (IReadOnlyList<string>?)GetValue(TaskRowOrderProperty); set => SetValue(TaskRowOrderProperty, value); }
    public RoleConfig? RoleConfig         { get => (RoleConfig?)GetValue(RoleConfigProperty);         set => SetValue(RoleConfigProperty, value); }
    public IReadOnlyList<TagEntry>? TagCatalog { get => (IReadOnlyList<TagEntry>?)GetValue(TagCatalogProperty); set => SetValue(TagCatalogProperty, value); }
    public IReadOnlyList<LinkRule>? LinkCatalog { get => (IReadOnlyList<LinkRule>?)GetValue(LinkCatalogProperty); set => SetValue(LinkCatalogProperty, value); }
    public bool BatchModeActive           { get => (bool)GetValue(BatchModeActiveProperty);           set => SetValue(BatchModeActiveProperty, value); }
    public int RoleConfigVersion          { get => (int)GetValue(RoleConfigVersionProperty);          set => SetValue(RoleConfigVersionProperty, value); }
    public int LayoutVersion              { get => (int)GetValue(LayoutVersionProperty);              set => SetValue(LayoutVersionProperty, value); }

    // --- Eventos para o pai ---
    public event EventHandler? DeleteRequested;
    public event EventHandler? DuplicateRequested;
    public event EventHandler? ItemChanged;
    public event EventHandler? DragStarted;         // pai usa para iniciar DnD real
    public event EventHandler<string>? FilterByTag;
    public event EventHandler<string>? FilterByContact;
    public event EventHandler<string>? FilterByAssignee;

    public TaskRowControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rebuild();
    }

    private void OnBatchModeChanged(bool active)
    {
        BatchCheck.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        BatchCol.Width = active ? new GridLength(20) : new GridLength(0);

        if (!active && Item is not null)
        {
            Item.IsSelected = false;
            UpdateSelectionBackground(false);
        }
    }

    private void OnBatchCheckChanged(object sender, RoutedEventArgs e)
    {
        if (Item is not null)
            Item.IsSelected = BatchCheck.IsChecked == true;
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TaskItemViewModel.IsSelected)) return;
        var selected = Item?.IsSelected ?? false;
        BatchCheck.IsChecked = selected;
        UpdateSelectionBackground(selected);
    }

    private void UpdateSelectionBackground(bool selected)
    {
        if (Item is null || Item.IsSection) return;
        if (selected)
            RowBorder.SetResourceReference(Border.BackgroundProperty, "BgRowSelected");
        else if (Item.Important)
            RowBorder.SetResourceReference(Border.BackgroundProperty, "BgRowImportant");
        else
            RowBorder.SetResourceReference(Border.BackgroundProperty, "BgRow");
    }

    // ===== Rebuild: monta a linha conforme item e configuração =====

    private void Rebuild()
    {
        if (Item is null) return;
        LeftArea.Children.Clear();
        RightArea.Children.Clear();

        if (Item.IsSection) { RebuildSection(); return; }

        RowBorder.SetResourceReference(HeightProperty, "RowHeight");
        RowBorder.BorderThickness = new Thickness(0, 0, 0, 1);
        RowBorder.ClearValue(BackgroundProperty);
        ContentArea.VerticalAlignment = VerticalAlignment.Center;
        ContentArea.Margin = new Thickness(0);

        UpdateImportantEar();
        BuildContextMenu();

        var order = TaskRowOrder ?? ["tags", "assignee", "contact", "title", "pendencia", "notes", "spacer", "date"];
        var rightSide = false;
        foreach (var token in order)
        {
            if (token == "spacer") { rightSide = true; continue; }
            var el = BuildToken(token, order);
            if (el is null) continue;
            if (rightSide) RightArea.Children.Add(el);
            else LeftArea.Children.Add(el);
        }
    }

    private void RebuildSection()
    {
        // Seção: fundo diferente, linha colorida, título em destaque, sem borda inferior
        RowBorder.SetResourceReference(HeightProperty, "SectionHeight");
        RowBorder.SetResourceReference(BackgroundProperty, "BgSection");
        RowBorder.BorderThickness = new Thickness(0);
        ImportantEar.Visibility = Visibility.Collapsed;
        DeleteBtn.Visibility = Visibility.Visible;

        ContentArea.VerticalAlignment = VerticalAlignment.Stretch;
        LeftArea.VerticalAlignment = VerticalAlignment.Bottom;
        LeftArea.Margin = new Thickness(0, 0, 0, 6);

        var editor = new InlineTextEditor
        {
            Text = Item!.Title,
            FontSize = (double)FindResource("FontSizeBase") + 2,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushFromHex(Item.SectionColor),
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        editor.EditConfirmed += (_, _) =>
        {
            Item.Title = editor.Text;
            ItemChanged?.Invoke(this, EventArgs.Empty);
            // Atualiza a cor do traço e do label
            Rebuild();
        };
        if (Item.IsEditing)
        {
            Dispatcher.BeginInvoke(editor.BeginEdit, System.Windows.Threading.DispatcherPriority.Loaded);
            Item.IsEditing = false;
        }
        LeftArea.Children.Add(editor);

        // Menu de seção
        var menu = new ContextMenu();
        menu.Items.Add(MakeMenuItem("Editar",      () => { Item.IsEditing = true; Rebuild(); },  ""));
        menu.Items.Add(MakeMenuItem("Alterar cor", OpenSectionColorPicker,                        ""));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenuItem("Duplicar",           () => DuplicateRequested?.Invoke(this, EventArgs.Empty), ""));
        menu.Items.Add(MakeMenuItem("Excluir",            () => DeleteRequested?.Invoke(this, EventArgs.Empty),    ""));
        ContextMenu = menu;
    }

    // ===== Tokens =====

    private UIElement? BuildToken(string token, IReadOnlyList<string> order) => token switch
    {
        "tags"      => BuildTagsToken(),
        "contact"   => BuildRoleToken("contact"),
        "assignee"  => BuildRoleToken("assignee"),
        "title"     => BuildTitleToken(order),
        "pendencia" => BuildPendenciaToken(),
        "notes"     => BuildNotesToken(),
        "date"      => BuildDateToken(),
        "spacer"    => new FrameworkElement { Width = 8 },
        _           => null,
    };

    private UIElement? BuildTagsToken()
    {
        if (Item is null || Item.TagNames.Count == 0) return null;
        var sp = (double)FindResource("TokenSpacing");
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var catalog = TagCatalog ?? [];
        var ordered = Item.TagNames
            .Select(n => catalog.FirstOrDefault(t => t.Name.Equals(n, StringComparison.OrdinalIgnoreCase)) ?? new TagEntry { Name = n })
            .OrderBy(t => t.Order);
        foreach (var tag in ordered)
        {
            var chip = new TagChipControl { TagName = tag.Name, TagColor = tag.Color, TagSize = tag.Size, TagStyle = tag.Style, TagFont = tag.Font, Margin = new Thickness(0, 0, sp, 0) };
            chip.FilterRequested += (_, name) => FilterByTag?.Invoke(this, name);
            chip.MouseLeftButtonUp += (_, e) => { OpenTagEditor(); e.Handled = true; };
            panel.Children.Add(chip);
        }
        return panel;
    }

    private UIElement? BuildRoleToken(string role)
    {
        if (Item is null) return null;
        var value = role == "contact" ? Item.Contact : Item.Assignee;
        if (string.IsNullOrEmpty(value)) return null;

        var cfg = (role == "contact" ? RoleConfig?.Contact : RoleConfig?.Assignee)
               ?? (role == "contact"
                   ? new RoleEntry { Color = "#0F766E", Style = "balão", Prefix = "@" }
                   : new RoleEntry { Color = "#7C3AED", Style = "balão", Prefix = "→" });

        var chip = new RoleChipControl
        {
            Value = value, RoleColor = cfg.Color, RoleStyle = cfg.Style,
            RolePrefix = cfg.Prefix, RoleFont = cfg.Font, RoleSize = cfg.Size,
            Margin = new Thickness(0, 0, (double)FindResource("TokenSpacing"), 0),
        };
        chip.MouseRightButtonDown += (_, e) =>
        {
            if (role == "contact") FilterByContact?.Invoke(this, value);
            else FilterByAssignee?.Invoke(this, value);
            e.Handled = true;
        };
        // Clique esquerdo: editar valor inline via popup simples
        chip.MouseLeftButtonUp += (_, e) =>
        {
            OpenRoleInlineEdit(role, value, chip);
            e.Handled = true;
        };
        return chip;
    }

    private void OpenRoleInlineEdit(string role, string currentValue, UIElement anchor)
    {
        var popup = new System.Windows.Controls.Primitives.Popup
        {
            PlacementTarget = anchor,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            StaysOpen = false,
            IsOpen = true,
        };
        var tb = new TextBox
        {
            Text = currentValue,
            Width = 160,
            Background = (SolidColorBrush)FindResource("BgPanel"),
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            BorderBrush = (SolidColorBrush)FindResource("Accent"),
            Padding = new Thickness(4, 2, 4, 2),
            CaretBrush = (SolidColorBrush)FindResource("TextPrimary"),
        };
        tb.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                if (e.Key == Key.Enter)
                {
                    if (role == "contact") Item!.Contact = tb.Text;
                    else Item!.Assignee = tb.Text;
                    ItemChanged?.Invoke(this, EventArgs.Empty);
                    Rebuild();
                }
                popup.IsOpen = false;
            }
        };
        popup.Child = new Border { Background = (SolidColorBrush)FindResource("BgPanel"), Child = tb, Padding = new Thickness(4) };
        tb.Focus();
        tb.SelectAll();
    }

    private UIElement BuildTitleToken(IReadOnlyList<string> order)
    {
        var editor = new InlineTextEditor
        {
            Text = Item!.Title,
            FontSize = (double)FindResource("FontSizeBase"),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 40,
            Margin = new Thickness(0, 0, (double)FindResource("TokenSpacing"), 0),
        };
        editor.SetResourceReference(Control.ForegroundProperty,
            Item.Completed ? "TextMuted" : "TextPrimary");
        if (Item.Completed)
            editor.ViewBlock.TextDecorations = TextDecorations.Strikethrough;

        editor.Loaded += (_, _) => ApplyLinkInlines(editor);
        editor.EditConfirmed += (_, _) =>
        {
            Item.Title = editor.Text;
            ItemChanged?.Invoke(this, EventArgs.Empty);
            Rebuild(); // reconstrói a linha — novo editor aplica links via Loaded
        };
        if (Item.IsEditing)
        {
            Dispatcher.BeginInvoke(editor.BeginEdit, System.Windows.Threading.DispatcherPriority.Loaded);
            Item.IsEditing = false;
        }
        return editor;
    }

    // Resolve links no título e popula o ViewBlock com Hyperlinks quando há matches.
    private void ApplyLinkInlines(InlineTextEditor editor)
    {
        var catalog = LinkCatalog ?? [];
        if (catalog.Count == 0 || Item is null) return;

        var segments = Services.LinkResolverService.Resolve(Item.Title, catalog);
        if (segments.All(s => s.Url is null))
        {
            editor.RestoreTextBinding();
            return;
        }

        var inlines = new List<System.Windows.Documents.Inline>();
        var completedFg = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
        var linkFg = Item.Completed
            ? completedFg
            : new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)); // azul

        foreach (var seg in segments)
        {
            if (seg.Url is null)
            {
                inlines.Add(new Run(seg.Text));
            }
            else
            {
                var run = new Run(seg.Text);
                var link = new Hyperlink(run)
                {
                    Foreground = linkFg,
                    TextDecorations = null,
                };
                // Impede o clique de propagar para o InlineTextEditor (evita abrir edição)
                link.MouseLeftButtonDown += (_, e) => e.Handled = true;
                link.Click += (_, _) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(seg.Url) { UseShellExecute = true });
                    }
                    catch { }
                };
                inlines.Add(link);
            }
        }

        editor.PopulateInlines(inlines);

        if (Item.Completed)
            editor.ViewBlock.TextDecorations = TextDecorations.Strikethrough;
    }

    private UIElement? BuildNotesToken()
    {
        if (Item is null || !Item.HasNotes) return null;
        var iconSize = (double)FindResource("FontSizeBase") + 2;
        var circleSize = iconSize + 8;
        var circle = new Border
        {
            Width = circleSize, Height = circleSize,
            CornerRadius = new CornerRadius(circleSize / 2),
            Background = (SolidColorBrush)FindResource("BgRow"),
            BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = iconSize - 2,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        var btn = new Button
        {
            Content = circle,
            ToolTip = MakeTooltip("Ver notas"),
            Padding = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Thickness(0, 0, (double)FindResource("TokenSpacing"), 0),
            Template = new ControlTemplate(typeof(Button))
            {
                VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)),
            },
        };
        btn.Click += (_, e) => { e.Handled = true; OpenNotesWindow(); };
        return btn;
    }

    private UIElement? BuildPendenciaToken()
    {
        if (Item is null || string.IsNullOrEmpty(Item.Pendencia)) return null;
        var chip = new RoleChipControl
        {
            Value = Item.Pendencia,
            RoleColor = RoleConfig?.Pendencia.Color ?? "#B45309",
            RoleStyle = RoleConfig?.Pendencia.Style ?? "balão",
            RolePrefix = RoleConfig?.Pendencia.Prefix ?? "⚠",
            RoleFont = RoleConfig?.Pendencia.Font ?? "Segoe UI",
            RoleSize = RoleConfig?.Pendencia.Size ?? string.Empty,
            Margin = new Thickness(0, 0, (double)FindResource("TokenSpacing"), 0),
            ToolTip = MakeTooltip("Pendência — clique para editar"),
        };
        chip.MouseLeftButtonUp += (_, e) =>
        {
            OpenPendenciaInlineEdit(chip);
            e.Handled = true;
        };
        return chip;
    }
    private void OpenPendenciaInlineEdit(UIElement anchor)
    {
        var popup = new System.Windows.Controls.Primitives.Popup
        {
            PlacementTarget = anchor,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            StaysOpen = false,
            IsOpen = true,
        };
        var tb = new TextBox
        {
            Text = Item!.Pendencia,
            Width = 220,
            Background = (SolidColorBrush)FindResource("BgPanel"),
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            BorderBrush = (SolidColorBrush)FindResource("Accent"),
            Padding = new Thickness(4, 2, 4, 2),
            CaretBrush = (SolidColorBrush)FindResource("TextPrimary"),
        };
        tb.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                if (e.Key == Key.Enter)
                {
                    Item!.Pendencia = tb.Text;
                    ItemChanged?.Invoke(this, EventArgs.Empty);
                    Rebuild();
                }
                popup.IsOpen = false;
            }
        };
        popup.Child = new Border { Background = (SolidColorBrush)FindResource("BgPanel"), Child = tb, Padding = new Thickness(4) };
        tb.Focus();
        tb.SelectAll();
    }

    private UIElement? BuildDateToken()
    {
        if (Item is null || string.IsNullOrEmpty(Item.DueDate)) return null;
        var isOverdue = DateOnly.TryParse(Item.DueDate, out var due) && due < DateOnly.FromDateTime(DateTime.Today);
        var tb = new TextBlock
        {
            Text = FormatDate(Item.DueDate),
            FontSize = (double)FindResource("FontSizeSmall"),
            Foreground = isOverdue
                ? (SolidColorBrush)FindResource("Danger")
                : (SolidColorBrush)FindResource("TextSecondary"),
            FontWeight = isOverdue ? FontWeights.SemiBold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, (double)FindResource("TokenSpacing"), 0),
            Cursor = Cursors.Hand,
        };
        tb.MouseLeftButtonDown += (_, _) => OpenDatePicker(tb);
        return tb;
    }

    // ===== Importância =====

    private void UpdateImportantEar()
    {
        if (Item is null) return;
        ImportantEar.Background = Item.Important
            ? (SolidColorBrush)FindResource("ImportantEar")
            : Brushes.Transparent;
        ImportantEar.ToolTip = Item.Important ? "Desmarcar importante" : "Marcar como importante";
        UpdateSelectionBackground(Item.IsSelected);
        BuildContextMenu();
    }

    private void OnToggleImportant(object sender, MouseButtonEventArgs e)
    {
        if (Item is null) return;
        Item.Important = !Item.Important;
        UpdateImportantEar();
        ItemChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    // ===== Hover =====

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        GripIcon.Opacity = 1;
        DeleteBtn.Opacity = 1;
        if (!Item!.IsSection && Item.IsSelected == false)
            RowBorder.SetResourceReference(BackgroundProperty,
                Item.Important ? "BgRowImportantHover" : "BgRowHover");
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        GripIcon.Opacity = 0;
        DeleteBtn.Opacity = Item?.IsSelected == true ? 0.6 : 0;
        if (!Item!.IsSection)
            UpdateSelectionBackground(Item.IsSelected);
    }

    // ===== Excluir =====

    private void OnDeleteClick(object sender, RoutedEventArgs e) =>
        DeleteRequested?.Invoke(this, EventArgs.Empty);

    // ===== Drag-and-drop =====

    private Point _dragStartPoint;
    private bool _isDragReady;

    private void OnGripMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragStarted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    // Inicia drag ao clicar e arrastar qualquer área vazia da linha.
    private void OnRowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (IsInteractiveDragSource(e.OriginalSource)) return;
        _dragStartPoint = e.GetPosition(this);
        _isDragReady = true;
    }

    private void OnRowPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragReady || e.LeftButton != MouseButtonState.Pressed) { _isDragReady = false; return; }
        var pos = e.GetPosition(this);
        var delta = pos - _dragStartPoint;
        if (Math.Abs(delta.X) > 4 || Math.Abs(delta.Y) > 4)
        {
            _isDragReady = false;
            DragStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnRowMouseUp(object sender, MouseButtonEventArgs e) => _isDragReady = false;

    private static bool IsInteractiveDragSource(object source)
    {
        for (var el = source as DependencyObject; el is not null; el = VisualTreeHelper.GetParent(el))
        {
            if (el is TextBox or Button or CheckBox or ComboBox or ListBox or System.Windows.Controls.Primitives.ScrollBar)
                return true;
            // Tag chips, role chips e outros controles clicáveis
            if (el is UserControl and not TaskRowControl)
                return true;
        }
        return false;
    }

    // ===== Context menu =====

    private void BuildContextMenu()
    {
        if (Item is null) return;
        var menu = new ContextMenu();
        menu.Items.Add(MakeMenuItem("Editar título", () => { Item.IsEditing = true; Rebuild(); }, ""));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenuItem(Item.Important ? "Desmarcar importante" : "Marcar como importante", () =>
        {
            Item.Important = !Item.Important;
            UpdateImportantEar();
            ItemChanged?.Invoke(this, EventArgs.Empty);
        }, Item.Important ? "" : ""));
        menu.Items.Add(MakeMenuItem("Alterar tags",       OpenTagEditor,                                       ""));
        menu.Items.Add(MakeMenuItem("Definir designado",  () => OpenRoleInlineEditByName("assignee"),           ""));
        menu.Items.Add(MakeMenuItem("Definir contato",    () => OpenRoleInlineEditByName("contact"),            ""));
                menu.Items.Add(MakeMenuItem("Definir pendência",  OpenPendenciaInlineEditByName,                        ""));
menu.Items.Add(MakeMenuItem("Definir data",       () => OpenDatePicker(null),                           ""));
        menu.Items.Add(MakeMenuItem("Notas",              OpenNotesWindow,                                      ""));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenuItem("Duplicar",           () => DuplicateRequested?.Invoke(this, EventArgs.Empty), ""));
        menu.Items.Add(MakeMenuItem("Excluir",            () => DeleteRequested?.Invoke(this, EventArgs.Empty),    ""));
        ContextMenu = menu;
    }

    private static MenuItem MakeMenuItem(string header, Action action, string? mdl2Icon = null)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var iconBlock = new TextBlock
        {
            Text       = mdl2Icon ?? string.Empty,
            FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize   = 13,
            Width      = 20,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var textBlock = new TextBlock
        {
            Text   = header,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(iconBlock);
        panel.Children.Add(textBlock);
        var item = new MenuItem { Header = panel };
        item.Click += (_, _) => action();
        return item;
    }

    private void OpenRoleInlineEditByName(string role)
    {
        var value = role == "contact" ? Item!.Contact : Item!.Assignee;
        OpenRoleInlineEdit(role, value, this);
    }

    private void OpenPendenciaInlineEditByName() => OpenPendenciaInlineEdit(this);

    private void OpenTagEditor()
    {
        if (Item is null) return;
        var catalog = TagCatalog ?? [];
        if (catalog.Count == 0)
        {
            MessageBox.Show("Nenhuma tag no catálogo. Adicione tags em # Gerenciar tags.", "UltraTask");
            return;
        }

        var popup = new System.Windows.Controls.Primitives.Popup
        {
            PlacementTarget = this,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
            StaysOpen = false,
        };

        // Estado de seleção por nome de tag
        var selected = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in catalog)
            selected[t.Name] = Item.Model.Tags.Contains(t.Name, StringComparer.OrdinalIgnoreCase);

        // Chips em coluna vertical
        var chipsWrap = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        void RenderChips()
        {
            chipsWrap.Children.Clear();
            foreach (var tag in catalog.OrderBy(t => t.Order))
            {
                var isOn = selected[tag.Name];
                var bg = isOn
                    ? BrushFromHex(tag.Color)
                    : new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));

                Brush fg;
                if (isOn)
                {
                    double lum = 0.299 * bg.Color.R + 0.587 * bg.Color.G + 0.114 * bg.Color.B;
                    fg = lum > 140 ? Brushes.Black : Brushes.White;
                }
                else
                {
                    fg = Brushes.White;
                }

                var chipBorder = new Border
                {
                    Background = bg,
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = Cursors.Hand,
                    Opacity = isOn ? 1.0 : 0.35,
                };
                var chipText = new TextBlock
                {
                    Text = tag.Name,
                    Foreground = fg,
                    FontSize = 11,
                };
                chipBorder.Child = chipText;

                var tagName = tag.Name; // captura
                chipBorder.MouseLeftButtonDown += (_, _) =>
                {
                    selected[tagName] = !selected[tagName];
                    RenderChips();
                };

                chipsWrap.Children.Add(chipBorder);
            }
        }

        RenderChips();

        var btnOk = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(16, 4, 16, 4),
            Style = (Style)FindResource("AccentButtonStyle"),
        };
        btnOk.Click += (_, _) =>
        {
            Item.Model.Tags.Clear();
            foreach (var kvp in selected)
                if (kvp.Value) Item.Model.Tags.Add(kvp.Key);
            Item.RefreshTags();
            ItemChanged?.Invoke(this, EventArgs.Empty);
            Rebuild();
            popup.IsOpen = false;
        };

        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(chipsWrap);
        panel.Children.Add(btnOk);

        var border = new Border
        {
            Background = (SolidColorBrush)FindResource("BgPanel"),
            BorderBrush = (SolidColorBrush)FindResource("BorderSubtle"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = panel,
            Focusable = true,
        };
        border.KeyDown += (_, e) => { if (e.Key == Key.Escape) popup.IsOpen = false; };
        popup.Child = border;
        popup.IsOpen = true;
        border.Focus();
    }

    private void OpenSectionColorPicker()
    {
        if (Item is null) return;
        var picked = Views.ColorPickerDialog.Pick(Item.SectionColor, Window.GetWindow(this));
        if (picked is not null)
        {
            Item.SectionColor = picked; // atualiza VM e Model via partial
            ItemChanged?.Invoke(this, EventArgs.Empty);
            Rebuild();
        }
    }

    private void OpenNotesWindow()
    {
        if (Item is null) return;
        var win = new Views.NotesWindow(Item.Model.NotesRich?.Html, html =>
        {
            Item.Model.NotesRich = string.IsNullOrWhiteSpace(html)
                ? null
                : new Models.NotesRich { Html = html };
            Item.RefreshNotes();
            ItemChanged?.Invoke(this, EventArgs.Empty);
            Rebuild();
        })
        {
            Owner = Window.GetWindow(this),
            Title = $"Notas — {Item.Title}",
        };
        win.ShowDialog();
    }

    private void OpenDatePicker(TextBlock? _)
    {
        var win = new Views.DatePickerWindow(Item!.DueDate) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() != true) return;

        if (win.Cleared)
            Item.DueDate = string.Empty;
        else if (win.SelectedDate.HasValue)
            Item.DueDate = win.SelectedDate.Value.ToString("yyyy-MM-dd");

        ItemChanged?.Invoke(this, EventArgs.Empty);
        Rebuild();
    }

    // ===== Auxiliares =====

    private static string FormatDate(string raw) =>
        DateOnly.TryParse(raw, out var d) ? d.ToString("dd/MM/yyyy") : raw;

    // Cria tooltip com fundo escuro compatível com o tema.
    private static ToolTip MakeTooltip(string text) => new()
    {
        Content = text,
        Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)),
        Foreground = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51)),
        FontSize = 11,
        Padding = new Thickness(6, 3, 6, 3),
    };

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.Gray; }
    }
}