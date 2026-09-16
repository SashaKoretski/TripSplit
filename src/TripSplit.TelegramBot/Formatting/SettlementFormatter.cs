using System.Text;

namespace TripSplit.TelegramBot.Formatting;

public static class SettlementFormatter
{
    public static async Task<string> BuildAsync(Trip trip, TripStatistics stats, IUserService users)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Поездка «{trip.Name}», всего потрачено: {stats.TotalSpent} {trip.Currency}");
        sb.AppendLine();

        if (stats.Transfers.Count == 0)
        {
            sb.AppendLine("Долгов нет — все квиты.");
            return sb.ToString();
        }

        sb.AppendLine("Переводы для погашения долгов:");
        foreach (var transfer in stats.Transfers)
        {
            var from = await users.GetByIdAsync(transfer.FromUserId);
            var to = await users.GetByIdAsync(transfer.ToUserId);
            sb.AppendLine($"  {from.Name} → {to.Name}: {transfer.Amount} {trip.Currency}");
        }

        return sb.ToString();
    }
}
