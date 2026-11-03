using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.WebUI.Dtos.V1;
using TripSplit.WebUI.Infrastructure.Api;

namespace TripSplit.WebUI.Controllers.Api.V1;

[Route("api/v1/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    // operationId: registerUser - строгий create: email занят -> 409, не тихий логин
    // (это сознательное расхождение с UserService.RegisterAsync/BR-08, см. docs/api/review.md;
    // сам UserService не меняем, он остается прежним для Web UI).
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register(RegisterUserRequestDto request)
    {
        if (await _users.FindByEmailAsync(request.Email) is not null)
            throw new UserAlreadyRegisteredException(request.Email);

        var googleId = $"local:{request.Email.Trim().ToLowerInvariant()}";
        var user = await _users.RegisterAsync(request.Name, request.Email, googleId);

        var dto = UserDto.FromDomain(user);
        return CreatedAtAction(nameof(GetById), new { userId = user.Id }, dto);
    }

    // operationId: getCurrentUser
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrent()
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        return Ok(UserDto.FromDomain(user));
    }

    // operationId: getUserById
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        return Ok(UserDto.FromDomain(user));
    }
}
