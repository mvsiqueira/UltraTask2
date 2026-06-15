using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UltraTask.Models;
using UltraTask.Services;

namespace UltraTask.ViewModels;

// ViewModel principal — coordena lista de itens, filtros, persistência e estado global.
public partial class MainViewModel : ObservableObject
{
    // --- Estado do arquivo atual ---

    [ObservableProperty]
    private string _fileTitle = "UltraTask";

    [ObservableProperty]
    private string _filePath = string.Empty;

    public TaskFile? CurrentFile { get; private set; }

    // Coleção fonte — mantém a ordem manual completa (incluindo itens filtrados).
    public ObservableCollection<TaskItemViewModel> AllItems { get; } = [];

    // View com filtro aplicado — usada pelo ItemsControl da lista principal.
    public ICollectionView FilteredItems { get; }

    // --- Filtros ---

    [ObservableProperty]
    private string _filterTag = string.Empty;
    partial void OnFilterTagChanged(string value) => RefreshFilter();

    [ObservableProperty]
    private string _filterContact = string.Empty;
    partial void OnFilterContactChanged(string value) => RefreshFilter();

    [ObservableProperty]
    private string _filterAssignee = string.Empty;
    partial void OnFilterAssigneeChanged(string value) => RefreshFilter();

    [ObservableProperty]
    private bool _filterImportantOnly;
    partial void OnFilterImportantOnlyChanged(bool value) => RefreshFilter();

    public bool HasActiveFilter =>
        !string.IsNullOrEmpty(FilterTag) ||
        !string.IsNullOrEmpty(FilterContact) ||
        !string.IsNullOrEmpty(FilterAssignee) ||
        FilterImportantOnly;

    // --- Modo lote ---

    [ObservableProperty]
    private bool _batchModeActive;
    partial void OnBatchModeActiveChanged(bool value)
    {
        if (!value) ClearBatchSelection();
    }

    public IEnumerable<TaskItemViewModel> SelectedItems =>
        AllItems.Where(i => i.IsSelected && !i.IsSection);

    public int BatchSelectedCount => AllItems.Count(i => i.IsSelected && !i.IsSection);

    // --- Settings ---

    public AppSettings Settings { get; private set; } = new();

    // --- Persistência com debounce ---

    private System.Timers.Timer? _saveTimer;
    private const int SaveDelayMs = 500;

    // --- Construtor ---

    public MainViewModel()
    {
        FilteredItems = CollectionViewSource.GetDefaultView(AllItems);
        FilteredItems.Filter = ApplyFilter;

        AllItems.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (TaskItemViewModel item in e.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            if (e.OldItems is not null)
                foreach (TaskItemViewModel item in e.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
        };

        Settings = PersistenceService.LoadSettings();

        if (!string.IsNullOrEmpty(Settings.TaskFilePath))
            LoadFile(Settings.TaskFilePath);
    }

    // --- Carga e recarga ---

    public void LoadFile(string path)
    {
        FilePath = path;
        var file = PersistenceService.LoadTaskFile(path);
        ApplyFile(file);

        Settings.TaskFilePath = path;
        PersistenceService.SaveSettings(Settings);
    }

    private void ApplyFile(TaskFile file)
    {
        CurrentFile = file;
        FileTitle = file.Title;

        AllItems.Clear();
        foreach (var item in file.Tasks)
        {
            var vm = new TaskItemViewModel(item);
            vm.SyncFromModel();
            AllItems.Add(vm);
        }

        OnPropertyChanged(nameof(AvailableTags));
        OnPropertyChanged(nameof(AvailableContacts));
        OnPropertyChanged(nameof(AvailableAssignees));
        OnPropertyChanged(nameof(TaskRowOrder));
        OnPropertyChanged(nameof(RoleConfig));
        OnPropertyChanged(nameof(TagCatalog));
        OnPropertyChanged(nameof(LinkCatalog));
    }

    [RelayCommand]
    public void Reload()
    {
        if (!string.IsNullOrEmpty(FilePath))
            LoadFile(FilePath);
    }

    // --- Propriedades delegadas ao arquivo ---

    public IReadOnlyList<string> TaskRowOrder =>
        CurrentFile?.TaskRowOrder ?? ["tags", "assignee", "contact", "title", "pendencia", "notes", "spacer", "date"];

    public RoleConfig RoleConfig =>
        CurrentFile?.RoleConfig ?? new RoleConfig();

    // Incrementado a cada mudança de role_config — força rebuild nas linhas via binding.
    [ObservableProperty]
    private int _roleConfigVersion;

    // Incrementado ao trocar layout — força rebuild para pegar novo FontSizeBase em código.
    [ObservableProperty]
    private int _layoutVersion;

    public IReadOnlyList<TagEntry> TagCatalog =>
        CurrentFile?.TagCatalog ?? [];

    public IReadOnlyList<LinkRule> LinkCatalog =>
        CurrentFile?.LinkCatalog ?? [];

    // --- Valores disponíveis para os dropdowns de filtro ---

    public IEnumerable<string> AvailableTags =>
        AllItems.SelectMany(i => i.TagNames).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t);

    public IEnumerable<string> AvailableContacts =>
        AllItems.Where(i => !i.IsSection && !string.IsNullOrEmpty(i.Contact))
                .Select(i => i.Contact).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c);

    public IEnumerable<string> AvailableAssignees =>
        AllItems.Where(i => !i.IsSection && !string.IsNullOrEmpty(i.Assignee))
                .Select(i => i.Assignee).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a);

    // --- Filtro ---

    private bool ApplyFilter(object obj)
    {
        if (obj is not TaskItemViewModel vm) return false;
        var criteria = new FilterService.FilterCriteria(
            Tag: FilterTag.Length > 0 ? FilterTag : null,
            Contact: FilterContact.Length > 0 ? FilterContact : null,
            Assignee: FilterAssignee.Length > 0 ? FilterAssignee : null,
            ImportantOnly: FilterImportantOnly ? true : null
        );
        return FilterService.Matches(vm.Model, criteria);
    }

    private void RefreshFilter()
    {
        FilteredItems.Refresh();
        OnPropertyChanged(nameof(HasActiveFilter));
    }

    [RelayCommand]
    public void ClearFilters()
    {
        FilterTag = string.Empty;
        FilterContact = string.Empty;
        FilterAssignee = string.Empty;
        FilterImportantOnly = false;
    }

    // --- Adicionar itens ---

    [RelayCommand]
    public void AddTask()
    {
        var item = new TaskItem { Title = "Nova tarefa" };
        var vm = new TaskItemViewModel(item);
        vm.SyncFromModel();
        AllItems.Add(vm);
        CurrentFile?.Tasks.Add(item);
        vm.IsEditing = true;
        ScheduleSave();
    }

    [RelayCommand]
    public void AddSection()
    {
        var item = new TaskItem { Title = "Nova seção", ItemType = "section" };
        var vm = new TaskItemViewModel(item);
        vm.SyncFromModel();
        AllItems.Add(vm);
        CurrentFile?.Tasks.Add(item);
        vm.IsEditing = true;
        ScheduleSave();
    }

    // --- Remover item ---

    [RelayCommand]
    public void DeleteItem(TaskItemViewModel vm)
    {
        AllItems.Remove(vm);
        CurrentFile?.Tasks.Remove(vm.Model);
        ScheduleSave();
    }

    // --- Duplicar item ---

    public void DuplicateItem(TaskItemViewModel vm)
    {
        var clone = vm.Model.Clone();
        var cloneVm = new TaskItemViewModel(clone);
        cloneVm.SyncFromModel();

        var idx = AllItems.IndexOf(vm);
        AllItems.Insert(idx + 1, cloneVm);

        var modelIdx = CurrentFile?.Tasks.IndexOf(vm.Model) ?? -1;
        if (modelIdx >= 0)
            CurrentFile!.Tasks.Insert(modelIdx + 1, clone);

        ScheduleSave();
    }

    // --- Reordenar (drag-and-drop) ---

    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        var vm = AllItems[fromIndex];
        AllItems.Move(fromIndex, toIndex);

        // Sincroniza por referência do model, não por índice
        if (CurrentFile is not null)
        {
            CurrentFile.Tasks.Remove(vm.Model);
            CurrentFile.Tasks.Insert(toIndex, vm.Model);
        }

        ScheduleSave();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskItemViewModel.IsSelected))
            OnPropertyChanged(nameof(BatchSelectedCount));
    }

    // --- Operações em lote ---

    public void ClearBatchSelection()
    {
        foreach (var vm in AllItems) vm.IsSelected = false;
    }

    public void BatchAddTag(string tagName)
    {
        foreach (var vm in SelectedItems.ToList())
        {
            if (!vm.Model.Tags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            {
                vm.Model.Tags.Add(tagName);
                vm.RefreshTags();
            }
        }
        ScheduleSave();
    }

    public void BatchRemoveTag(string tagName)
    {
        foreach (var vm in SelectedItems.ToList())
        {
            vm.Model.Tags.RemoveAll(t => t.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            vm.RefreshTags();
        }
        ScheduleSave();
    }

    public void BatchSetContact(string value)
    {
        foreach (var vm in SelectedItems.ToList()) vm.Contact = value;
        ScheduleSave();
    }

    public void BatchSetAssignee(string value)
    {
        foreach (var vm in SelectedItems.ToList()) vm.Assignee = value;
        ScheduleSave();
    }

    public void BatchSetImportant(bool value)
    {
        foreach (var vm in SelectedItems.ToList()) vm.Important = value;
        ScheduleSave();
    }

    [RelayCommand]
    public void BatchDelete()
    {
        foreach (var vm in SelectedItems.ToList())
        {
            AllItems.Remove(vm);
            CurrentFile?.Tasks.Remove(vm.Model);
        }
        ScheduleSave();
    }

    // --- Persistência com debounce ---

    // Agenda o salvamento do arquivo com atraso para não travar a UI em edições rápidas.
    public void ScheduleSave()
    {
        _saveTimer?.Stop();
        _saveTimer ??= new System.Timers.Timer(SaveDelayMs) { AutoReset = false };
        _saveTimer.Elapsed += (_, _) => SaveNow();
        _saveTimer.Start();
    }

    // Expõe notificação para uso externo (ex: MainWindow após fechar janelas auxiliares).
    public new void OnPropertyChanged(string propertyName) => base.OnPropertyChanged(propertyName);

    [RelayCommand]
    public void SaveNow()
    {
        if (CurrentFile is null || string.IsNullOrEmpty(FilePath)) return;

        // Sincroniza a ordem do modelo com a ObservableCollection antes de salvar.
        CurrentFile.Tasks.Clear();
        CurrentFile.Tasks.AddRange(AllItems.Select(vm => vm.Model));
        CurrentFile.Title = FileTitle;

        PersistenceService.SaveTaskFile(CurrentFile, FilePath);
    }
}
