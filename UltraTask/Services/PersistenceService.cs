using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UltraTask.Models;

namespace UltraTask.Services;

// Responsável por ler e salvar os dois tipos de arquivo do app:
//   1. settings.json  — preferências locais
//   2. arquivo de tarefas JSON — dados do usuário
public static class PersistenceService
{
    // Localização fixa do settings.json ao lado do executável.
    public static string SettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new NotesRichConverter() },
    };

    // --- AppSettings ---

    public static AppSettings LoadSettings()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch
        {
            // Arquivo corrompido: começa com padrões.
            return new AppSettings();
        }
    }

    public static void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    // --- Arquivo de tarefas ---

    public static TaskFile LoadTaskFile(string path)
    {
        if (!File.Exists(path))
            return new TaskFile();

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize<TaskFile>(json, _jsonOptions) ?? new TaskFile();
            return Migrate(file);
        }
        catch
        {
            return new TaskFile();
        }
    }

    public static void SaveTaskFile(TaskFile file, string path)
    {
        // Garante que a pasta de destino existe (útil se o arquivo estiver em OneDrive etc.)
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(file, _jsonOptions);
        File.WriteAllText(path, json);
    }

    // Compatibilidade com arquivos antigos: preenche campos ausentes sem quebrar.
    private static TaskFile Migrate(TaskFile file)
    {
        // Arquivo antigo sem task_row_order — aplica padrão.
        if (file.TaskRowOrder.Count == 0)
            file.TaskRowOrder = ["tags", "assignee", "contact", "title", "pendencia", "notes", "spacer", "date"];

        if (!file.TaskRowOrder.Contains("pendencia"))
        {
            var notesIdx = file.TaskRowOrder.IndexOf("notes");
            var insertAt = notesIdx >= 0 ? notesIdx : file.TaskRowOrder.Count;
            file.TaskRowOrder.Insert(insertAt, "pendencia");
        }

        // Migra nomes antigos de estilo de chip para os novos.
        foreach (var entry in new[] { file.RoleConfig.Contact, file.RoleConfig.Assignee, file.RoleConfig.Pendencia })
        {
            if (entry.Style == "balloon") entry.Style = "balão";
            if (entry.Style == "tag")     entry.Style = "rótulo";
        }

        // role_config ausente já é inicializado pelo construtor padrão.

        // Promove tags encontradas apenas dentro de tarefas para o catálogo.
        var catalogNames = file.TagCatalog
            .Select(t => t.Name.ToUpperInvariant())
            .ToHashSet();

        foreach (var task in file.Tasks)
        {
            foreach (var tagName in task.Tags)
            {
                if (!catalogNames.Contains(tagName.ToUpperInvariant()))
                {
                    file.TagCatalog.Add(new TagEntry
                    {
                        Name = tagName,
                        Color = "#6B7280",
                        Order = file.TagCatalog.Count,
                    });
                    catalogNames.Add(tagName.ToUpperInvariant());
                }
            }
        }

        return file;
    }

    // Cria um arquivo de tarefas novo no caminho indicado e salva.
    public static TaskFile CreateNewTaskFile(string path, string title)
    {
        var file = new TaskFile { Title = title };
        SaveTaskFile(file, path);
        return file;
    }
}
