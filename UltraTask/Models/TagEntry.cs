using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Entrada do catálogo de tags do arquivo de tarefas.
public class TagEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#2563EB";

    [JsonPropertyName("order")]
    public int Order { get; set; }

    // Largura fixa em caracteres; string vazia = largura automática.
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;
}
