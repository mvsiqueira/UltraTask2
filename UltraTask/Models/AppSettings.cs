using System.Text.Json.Serialization;

namespace UltraTask.Models;

// Preferências locais do aplicativo — não contém dados da lista.
// Persiste apenas o que é específico da instalação local.
public class AppSettings
{
    // Caminho completo do arquivo de tarefas ativo.
    [JsonPropertyName("task_file_path")]
    public string TaskFilePath { get; set; } = string.Empty;

    // Modo de layout da lista (ex: "compact", "normal").
    [JsonPropertyName("layout_mode")]
    public string LayoutMode { get; set; } = "compact";

    // Largura da janela principal salva entre sessões.
    [JsonPropertyName("window_width")]
    public double WindowWidth { get; set; } = 1100;

    [JsonPropertyName("window_height")]
    public double WindowHeight { get; set; } = 700;
}
