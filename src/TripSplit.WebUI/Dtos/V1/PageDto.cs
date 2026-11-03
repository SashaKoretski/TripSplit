namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/TripPage, ExpensePage - пагинация в API-слое (in-memory Skip/Take),
// без изменений в Repository: коллекции на пользователя/поездку сейчас небольшие.
public class PageDto<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int Total { get; init; }

    public static PageDto<T> Of(IReadOnlyList<T> all, int page, int pageSize) => new()
    {
        Items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
        Page = page,
        PageSize = pageSize,
        Total = all.Count
    };
}
