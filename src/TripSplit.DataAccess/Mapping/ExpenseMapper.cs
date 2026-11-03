using TripSplit.BusinessLogic.Models;

namespace TripSplit.DataAccess.Mapping;

internal static class ExpenseMapper
{
    public const string Columns =
        "id, trip_id, receipt_id, payer_id, name, type, value, discount, created_at, updated_at";

    public readonly record struct Row(
        Guid Id, Guid TripId, Guid? ReceiptId, Guid PayerId,
        string Name, ExpenseType Type, decimal Value, decimal Discount,
        DateTime CreatedAt, DateTime UpdatedAt);

    public static Row ReadRow(DbDataReader r)
    {
        var recOrd = r.GetOrdinal("receipt_id");
        return new Row(
            Id:        r.GetGuid(r.GetOrdinal("id")),
            TripId:    r.GetGuid(r.GetOrdinal("trip_id")),
            ReceiptId: r.IsDBNull(recOrd) ? null : r.GetGuid(recOrd),
            PayerId:   r.GetGuid(r.GetOrdinal("payer_id")),
            Name:      r.GetString(r.GetOrdinal("name")),
            Type:      ParseType(r.GetString(r.GetOrdinal("type"))),
            Value:     r.GetDecimal(r.GetOrdinal("value")),
            Discount:  r.GetDecimal(r.GetOrdinal("discount")),
            CreatedAt: r.GetDateTime(r.GetOrdinal("created_at")),
            UpdatedAt: r.GetDateTime(r.GetOrdinal("updated_at"))
        );
    }

    public static Expense ToDomain(Row row, IEnumerable<Guid> consumerIds) => new(
        id:          row.Id,
        tripId:      row.TripId,
        payerId:     row.PayerId,
        name:        row.Name,
        type:        row.Type,
        value:       row.Value,
        discount:    row.Discount,
        consumerIds: consumerIds,
        receiptId:   row.ReceiptId
    )
    {
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };

    public static string ToDbType(ExpenseType t) => t switch
    {
        ExpenseType.Food          => "food",
        ExpenseType.Transport     => "transport",
        ExpenseType.Accommodation => "accommodation",
        ExpenseType.Entertainment => "entertainment",
        ExpenseType.Other         => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(t), t, "Unknown ExpenseType")
    };

    private static ExpenseType ParseType(string t) => t switch
    {
        "food"          => ExpenseType.Food,
        "transport"     => ExpenseType.Transport,
        "accommodation" => ExpenseType.Accommodation,
        "entertainment" => ExpenseType.Entertainment,
        "other"         => ExpenseType.Other,
        _ => throw new InvalidOperationException($"Unknown expense type: {t}")
    };
}
