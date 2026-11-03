using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.WebUI.Dtos.V1;
using TripSplit.WebUI.Infrastructure.Api;

namespace TripSplit.WebUI.Controllers.Api.V1;

[ApiController]
[AllowAnonymous]
[TypeFilter(typeof(ApiExceptionFilter))]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IJwtTokenService _tokens;

    public AuthController(IUserService users, IJwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    // operationId: issueAuthToken
    [HttpPost("token")]
    public async Task<ActionResult<AuthTokenDto>> IssueToken(AuthTokenRequestDto request)
    {
        var user = await _users.FindByEmailAsync(request.Email)
            ?? throw new UserNotFoundException(request.Email);

        var (accessToken, expiresIn) = _tokens.IssueToken(user.Id);
        return Ok(new AuthTokenDto { AccessToken = accessToken, ExpiresIn = expiresIn });
    }
}
