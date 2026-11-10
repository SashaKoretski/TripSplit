using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/User - GoogleId не покидает BL (внутренний технический артефакт)
public class UserDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }

    public static UserDto FromDomain(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email
    };
}
