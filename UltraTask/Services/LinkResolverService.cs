using System.Text.RegularExpressions;
using System.Web;
using UltraTask.Models;

namespace UltraTask.Services;

// Resolve regras de link automático em títulos de tarefas.
// Retorna segmentos de texto intercalados com links para montar FlowDocument na UI.
public static class LinkResolverService
{
    // Segmento de texto com link opcional.
    public record Segment(string Text, string? Url = null);

    // Quebra o título em segmentos de texto e link conforme o catálogo.
    // Regras são aplicadas na ordem do catálogo; cada trecho só é processado uma vez.
    public static List<Segment> Resolve(string title, IReadOnlyList<LinkRule> catalog)
    {
        if (string.IsNullOrEmpty(title) || catalog.Count == 0)
            return [new Segment(title)];

        // Mapa de posição inicial → (tamanho, url) para não sobrepor matches.
        var covered = new SortedDictionary<int, (int Length, string Url)>();

        foreach (var rule in catalog.OrderBy(r => r.Order))
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern))
                continue;

            Regex regex;
            try { regex = new Regex(rule.Pattern); }
            catch { continue; }

            foreach (Match m in regex.Matches(title))
            {
                // Ignora sobreposição com match anterior de maior prioridade.
                if (covered.Any(kv => m.Index < kv.Key + kv.Value.Length && m.Index + m.Length > kv.Key))
                    continue;

                var url = BuildUrl(rule.UrlTemplate, m);
                covered[m.Index] = (m.Length, url);
            }
        }

        // Monta a lista de segmentos a partir dos intervalos cobertos.
        var result = new List<Segment>();
        int pos = 0;

        foreach (var kv in covered)
        {
            int start = kv.Key;
            int len = kv.Value.Length;

            if (start > pos)
                result.Add(new Segment(title[pos..start]));

            result.Add(new Segment(title[start..(start + len)], kv.Value.Url));
            pos = start + len;
        }

        if (pos < title.Length)
            result.Add(new Segment(title[pos..]));

        return result.Count > 0 ? result : [new Segment(title)];
    }

    // Constrói a URL substituindo {match}, {1}, {nome} pelos grupos capturados.
    private static string BuildUrl(string template, Match m)
    {
        var url = template;

        // {match} — texto completo do match
        url = url.Replace("{match}", HttpUtility.UrlEncode(m.Value));

        // Grupos numéricos {1}, {2} …
        for (int i = 1; i < m.Groups.Count; i++)
        {
            if (m.Groups[i].Success)
                url = url.Replace($"{{{i}}}", HttpUtility.UrlEncode(m.Groups[i].Value));
        }

        // Grupos nomeados {nome}
        foreach (Group g in m.Groups.Values.Where(g => !int.TryParse(g.Name, out _)))
        {
            if (g.Success)
                url = url.Replace($"{{{g.Name}}}", HttpUtility.UrlEncode(g.Value));
        }

        return url;
    }
}
