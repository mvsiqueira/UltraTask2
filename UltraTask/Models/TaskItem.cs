using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Representa um item da lista — pode ser tarefa comum ou seção.
// Espelha fielmente a estrutura JSON do arquivo de tarefas.
public class TaskItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("important")]
    public bool Important { get; set; }

    [JsonPropertyName("due_date")]
    public string DueDate { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    // Payload de notas ricas em HTML normalizado; null quando não há nota rica.
    [JsonPropertyName("notes_rich")]
    public NotesRich? NotesRich { get; set; }

    [JsonPropertyName("contact")]
    public string Contact { get; set; } = string.Empty;

    [JsonPropertyName("assignee")]
    public string Assignee { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    // "task" ou "section"
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = "task";

    [JsonPropertyName("section_color")]
    public string SectionColor { get; set; } = "#B45309";

    [JsonIgnore]
    public bool IsSection => ItemType == "section";

    // Cria uma cópia independente do item (para duplicar).
    public TaskItem Clone() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Title = Title,
        Completed = Completed,
        Important = Important,
        DueDate = DueDate,
        Notes = Notes,
        NotesRich = NotesRich is null ? null : new NotesRich { Html = NotesRich.Html },
        Contact = Contact,
        Assignee = Assignee,
        Tags = [.. Tags],
        ItemType = ItemType,
        SectionColor = SectionColor,
    };
}

// Envelope do HTML normalizado das notas ricas.
public class NotesRich
{
    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;
}
