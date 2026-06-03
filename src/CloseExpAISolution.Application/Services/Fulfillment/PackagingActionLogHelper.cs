namespace CloseExpAISolution.Application.Services.Fulfillment;

/// <summary>
/// Builds <see cref="Domain.Entities.OrderStatusLog"/> notes for packaging actions without new entities.
/// Prefix <see cref="NotePrefix"/> is used to query packaging activity on an order.
/// </summary>
public static class PackagingActionLogHelper
{
    public const string NotePrefix = "[PKG] ";

    public static string BuildNote(
        string actionLabel,
        IReadOnlyList<Guid> orderItemIds,
        IReadOnlyDictionary<Guid, string>? productNameByOrderItemId,
        string? staffNote)
    {
        var parts = new List<string> { $"{NotePrefix}{actionLabel.Trim()}" };

        if (orderItemIds.Count > 0)
        {
            var lineDescriptions = orderItemIds.Select(id =>
            {
                if (productNameByOrderItemId != null
                    && productNameByOrderItemId.TryGetValue(id, out var name)
                    && !string.IsNullOrWhiteSpace(name))
                {
                    return $"{name.Trim()} ({id:D})";
                }

                return id.ToString("D");
            });

            parts.Add($"Dòng: {string.Join("; ", lineDescriptions)}");
        }

        if (!string.IsNullOrWhiteSpace(staffNote))
            parts.Add($"Ghi chú: {staffNote.Trim()}");

        var note = string.Join(" | ", parts);
        return note.Length > 2000 ? note[..2000] : note;
    }

    public static bool IsPackagingActionNote(string? note) =>
        !string.IsNullOrWhiteSpace(note) && note.StartsWith(NotePrefix, StringComparison.Ordinal);

    public static string ExtractActionLabel(string? note)
    {
        if (!IsPackagingActionNote(note) || note == null)
            return string.Empty;

        var body = note[NotePrefix.Length..];
        var separator = body.IndexOf(" | ", StringComparison.Ordinal);
        return separator < 0 ? body.Trim() : body[..separator].Trim();
    }
}
