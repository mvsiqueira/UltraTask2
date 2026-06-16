using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Preferências locais do aplicativo — não contém dados da lista.
// Persiste apenas o que é específico da instalação local.
public class AppSettings
{
    // Caminho completo do arquivo de tarefas ativo.
    [JsonPropertyName("task_file_path")]
    public string TaskFilePath { get; set; } = string.Empty;

    // Tema visual: "dark" ou "light".
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "dark";

    // Modo de layout da lista: "compact", "normal" ou "extended".
    [JsonPropertyName("layout_mode")]
    public string LayoutMode { get; set; } = "compact";

    // Geometria da janela principal salva entre sessões.
    [JsonPropertyName("window_width")]
    public double WindowWidth { get; set; } = 1100;

    [JsonPropertyName("window_height")]
    public double WindowHeight { get; set; } = 700;

    // null = não restaurar (centraliza na inicialização).
    [JsonPropertyName("window_left")]
    public double? WindowLeft { get; set; } = null;

    [JsonPropertyName("window_top")]
    public double? WindowTop { get; set; } = null;

    [JsonPropertyName("window_state")]
    public string WindowState { get; set; } = "Normal";

    // Formato do título na taskbar: "app", "list", "app-list", "list-app".
    [JsonPropertyName("titlebar_format")]
    public string TitlebarFormat { get; set; } = "app-list";

    // Realça o fundo das tarefas marcadas como importantes.
    [JsonPropertyName("highlight_important")]
    public bool HighlightImportant { get; set; } = false;
}
