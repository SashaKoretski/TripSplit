using TripSplit.BusinessLogic.Models;

namespace TripSplit.DataAccess.Mapping;

internal static class ReceiptMapper
{
    public const string Columns = "id, trip_id, name, date";

    public static Receipt Map(DbDataReader r) => new(
        id:     r.GetGuid(r.GetOrdinal("id")),
        tripId: r.GetGuid(r.GetOrdinal("trip_id")),
        name:   r.GetString(r.GetOrdinal("name")),
        date:   r.GetFieldValue<DateOnly>(r.GetOrdinal("date"))
    );
}
