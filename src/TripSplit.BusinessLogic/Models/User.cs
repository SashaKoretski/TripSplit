namespace TripSplit.BusinessLogic.Models;

public class User
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string GoogleId { get; init; }

    public User(Guid id, string name, string email, string googleId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("GoogleId is required", nameof(googleId));

        Id = id;
        Name = name;
        Email = email;
        GoogleId = googleId;
    }
}
