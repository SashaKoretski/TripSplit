using TripSplit.BusinessLogic.Models;

namespace TripSplit.DataAccess.Mapping;

internal static class ReceiptMapper
{
    public const string Columns = "id, trip_id, file_url, date";

    public static Receipt Map(DbDataReader r) => new(
        id:      r.GetGuid(r.GetOrdinal("id")),
        tripId:  r.GetGuid(r.GetOrdinal("trip_id")),
        fileUrl: r.GetString(r.GetOrdinal("file_url")),
        date:    r.GetFieldValue<DateOnly>(r.GetOrdinal("date"))
    );
}
