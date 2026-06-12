using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Regra de link automático: transforma trechos do título em hiperlinks via regex.
public class LinkRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    // Expressão regular com suporte a grupos nomeados e numéricos.
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    // Template de URL — aceita {match}, {1}, {nome}.
    [JsonPropertyName("url_template")]
    public string UrlTemplate { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }
}
