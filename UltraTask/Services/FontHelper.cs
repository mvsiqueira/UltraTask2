using System.Windows.Media;

namespace UltraTask.Services;

// Resolve FontFamily pelo nome, usando fontes embutidas como recurso quando disponíveis.
// Fontes embutidas em Fonts/ são referenciadas via URI pack para garantir que funcionem
// mesmo sem a fonte instalada no sistema do usuário.
public static class FontHelper
{
    private static readonly Uri _packBase = new("pack://application:,,,/");

    private static readonly Dictionary<string, string> _embedded = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dancing Script"]  = "/UltraTask;component/Fonts/DancingScript-Regular.ttf#Dancing Script",
        ["JetBrains Mono"]  = "/UltraTask;component/Fonts/JetBrainsMono-Regular.ttf#JetBrains Mono",
        ["Space Mono"]      = "/UltraTask;component/Fonts/SpaceMono-Regular.ttf#Space Mono",
    };

    public static FontFamily Resolve(string name)
    {
        if (_embedded.TryGetValue(name, out var packPath))
            return new FontFamily(_packBase, packPath);
        return new FontFamily(name);
    }
}
