using TripSplit.BusinessLogic.Models;

namespace TripSplit.DataAccess.Mapping;

internal static class UserMapper
{
    public static User Map(DbDataReader r) => new(
        id: r.GetGuid(r.GetOrdinal("id")),
        name: r.GetString(r.GetOrdinal("name")),
        email: r.GetString(r.GetOrdinal("email")),
        googleId: r.GetString(r.GetOrdinal("google_id"))
    );
}