namespace TripSplit.BusinessLogic.Models;

public class Transfer
{
    public Guid FromUserId { get; init; }
    public Guid ToUserId { get; init; }
    public decimal Amount { get; init; }

    public Transfer(Guid fromUserId, Guid toUserId, decimal amount)
    {
        if (fromUserId == Guid.Empty)
            throw new ArgumentException("From user id cannot be empty", nameof(fromUserId));
        if (toUserId == Guid.Empty)
            throw new ArgumentException("To user id cannot be empty", nameof(toUserId));
        if (fromUserId == toUserId)
            throw new ArgumentException("Transfer cannot be to the same user");
        if (amount <= 0)
            throw new ArgumentException("Transfer amount must be positive", nameof(amount));

        FromUserId = fromUserId;
        ToUserId = toUserId;
        Amount = amount;
    }
}
