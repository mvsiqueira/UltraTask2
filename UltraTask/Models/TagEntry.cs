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

    // "rótulo" | "balão" | "faixa"
    [JsonPropertyName("style")]
    public string Style { get; set; } = "rótulo";

    [JsonPropertyName("font")]
    public string Font { get; set; } = "Segoe UI";

    public TagEntry Clone() => new() { Name = Name, Color = Color, Order = Order, Size = Size, Style = Style, Font = Font };
}
