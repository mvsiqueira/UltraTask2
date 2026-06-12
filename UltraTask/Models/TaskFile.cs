using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Representa o arquivo de tarefas completo — é o documento principal do usuário.
// Um arquivo = uma lista, com suas próprias tags, papéis, links e ordem de linha.
public class TaskFile
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "UltraTask";

    [JsonPropertyName("tasks")]
    public List<TaskItem> Tasks { get; set; } = [];

    // Ordem visual dos tokens da linha de tarefa.
    // Tokens válidos: tags, assignee, contact, title, notes, spacer, date.
    [JsonPropertyName("task_row_order")]
    public List<string> TaskRowOrder { get; set; } = ["tags", "assignee", "contact", "title", "notes", "spacer", "date"];

    [JsonPropertyName("role_config")]
    public RoleConfig RoleConfig { get; set; } = new();

    [JsonPropertyName("tag_catalog")]
    public List<TagEntry> TagCatalog { get; set; } = [];

    [JsonPropertyName("link_catalog")]
    public List<LinkRule> LinkCatalog { get; set; } = [];
}
