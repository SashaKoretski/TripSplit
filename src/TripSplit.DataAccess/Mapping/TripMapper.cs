using TripSplit.BusinessLogic.Models;

namespace TripSplit.DataAccess.Mapping;

internal static class TripMapper
{
    public const string Columns = "id, name, status, currency, created_at";

    public static Trip Map(DbDataReader r, IReadOnlyList<Guid> participantIds)
    {
        var trip = new Trip(
            id:        r.GetGuid(r.GetOrdinal("id")),
            name:      r.GetString(r.GetOrdinal("name")),
            currency:  r.GetString(r.GetOrdinal("currency")).Trim(),
            createdAt: r.GetFieldValue<DateTime>(r.GetOrdinal("created_at"))
        );
        trip.Status = ParseStatus(r.GetString(r.GetOrdinal("status")));
        foreach (var pid in participantIds) trip.ParticipantIds.Add(pid);
        return trip;
    }

    public static string ToDbStatus(TripStatus s) => s switch
    {
        TripStatus.Active   => "active",
        TripStatus.Finished => "finished",
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown TripStatus")
    };

    private static TripStatus ParseStatus(string s) => s switch
    {
        "active"   => TripStatus.Active,
        "finished" => TripStatus.Finished,
        _ => throw new InvalidOperationException($"Unknown trip status: {s}")
    };
}
