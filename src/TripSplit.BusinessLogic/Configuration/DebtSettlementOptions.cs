namespace TripSplit.BusinessLogic.Configuration;

// Параметры сервиса взаимозачета долгов
public sealed class DebtSettlementOptions
{
    // Минимальная сумма перевода — долги меньше этой величины игнорируются
    public decimal MinTransferAmount { get; init; } = 0.01m;
}
