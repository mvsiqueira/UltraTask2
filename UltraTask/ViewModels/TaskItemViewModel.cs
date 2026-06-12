using CommunityToolkit.Mvvm.ComponentModel;
using UltraTask.Models;

namespace UltraTask.ViewModels;

// ViewModel de um item da lista — expõe propriedades observáveis para o binding da linha.
// Mantém referência ao modelo e notifica a UI quando qualquer campo muda.
public partial class TaskItemViewModel : ObservableObject
{
    // Modelo subjacente — serializado diretamente para JSON.
    public TaskItem Model { get; }

    public TaskItemViewModel(TaskItem model) => Model = model;

    // --- Propriedades espelhadas do modelo com notificação ---

    public string Id => Model.Id;
    public bool IsSection => Model.IsSection;

    [ObservableProperty]
    private string _title = string.Empty;
    partial void OnTitleChanged(string value) => Model.Title = value;

    [ObservableProperty]
    private bool _completed;
    partial void OnCompletedChanged(bool value) => Model.Completed = value;

    [ObservableProperty]
    private bool _important;
    partial void OnImportantChanged(bool value) => Model.Important = value;

    [ObservableProperty]
    private string _dueDate = string.Empty;
    partial void OnDueDateChanged(string value) => Model.DueDate = value;

    [ObservableProperty]
    private string _contact = string.Empty;
    partial void OnContactChanged(string value) => Model.Contact = value;

    [ObservableProperty]
    private string _assignee = string.Empty;
    partial void OnAssigneeChanged(string value) => Model.Assignee = value;

    [ObservableProperty]
    private string _sectionColor = "#B45309";
    partial void OnSectionColorChanged(string value) => Model.SectionColor = value;

    // Nota rica — indica visualmente se há nota.
    public bool HasNotes => !string.IsNullOrWhiteSpace(Model.Notes) || Model.NotesRich is not null;

    // Tags — a UI lê direto do Model.Tags (lista mutável).
    // Chame RefreshTags() para forçar notificação após mudanças.
    public void RefreshTags() => OnPropertyChanged(nameof(TagNames));
    public IReadOnlyList<string> TagNames => Model.Tags;

    // --- Estado de UI (não persistido) ---

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isSelected; // para operações em lote

    // Sincroniza todas as propriedades observáveis com o modelo subjacente.
    // Chamado após carregar um arquivo ou recarregar do disco.
    public void SyncFromModel()
    {
        Title    = Model.Title;
        Completed  = Model.Completed;
        Important  = Model.Important;
        DueDate   = Model.DueDate;
        Contact   = Model.Contact;
        Assignee  = Model.Assignee;
        SectionColor = Model.SectionColor;
        OnPropertyChanged(nameof(HasNotes));
        OnPropertyChanged(nameof(TagNames));
    }

    // Notifica a UI de que as notas mudaram (para atualizar o badge).
    public void RefreshNotes() => OnPropertyChanged(nameof(HasNotes));
}
