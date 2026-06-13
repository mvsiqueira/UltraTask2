using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Configuração visual de um papel (Contato ou Designado).
public class RoleEntry
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#0F766E";

    // "tag" ou "balloon"
    [JsonPropertyName("style")]
    public string Style { get; set; } = "balloon";

    // Prefixo textual; string vazia é permitido.
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = string.Empty;

    [JsonPropertyName("font")]
    public string Font { get; set; } = "Segoe UI";

    // Largura fixa em caracteres; string vazia = automático.
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    public RoleEntry Clone() => new() { Color = Color, Style = Style, Prefix = Prefix, Font = Font, Size = Size };
}

// Agrupa as configurações dos dois papéis configuráveis.
public class RoleConfig
{
    [JsonPropertyName("contact")]
    public RoleEntry Contact { get; set; } = new() { Color = "#0F766E", Prefix = "@" };

    [JsonPropertyName("assignee")]
    public RoleEntry Assignee { get; set; } = new() { Color = "#7C3AED", Prefix = "→" };
}
