using UltraTask.Models;

namespace UltraTask.Services;

// Lógica de filtro da lista principal — sem dependência de UI.
public static class FilterService
{
    public record FilterCriteria(
        string? Tag,
        string? Contact,
        string? Assignee,
        bool? ImportantOnly
    );

    // Retorna true quando o item deve aparecer na lista com os filtros dados.
    // Seções sempre passam — a supressão de seções vazias fica a cargo da UI.
    public static bool Matches(TaskItem item, FilterCriteria criteria)
    {
        if (item.IsSection)
            return true;

        if (criteria.Tag is { Length: > 0 } tag &&
            !item.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (criteria.Contact is { Length: > 0 } contact &&
            !item.Contact.Equals(contact, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.Assignee is { Length: > 0 } assignee &&
            !item.Assignee.Equals(assignee, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.ImportantOnly == true && !item.Important)
            return false;

        return true;
    }

    public static bool IsEmpty(FilterCriteria c) =>
        string.IsNullOrEmpty(c.Tag) &&
        string.IsNullOrEmpty(c.Contact) &&
        string.IsNullOrEmpty(c.Assignee) &&
        c.ImportantOnly != true;
}
