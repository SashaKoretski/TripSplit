using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Infrastructure.Api;

namespace TripSplit.WebUI.Controllers.Api.V1;

[ApiController]
[Authorize]
[TypeFilter(typeof(ApiExceptionFilter))]
public abstract class ApiControllerBase : ControllerBase
{
    // userId всегда валиден для аутентифицированного запроса - токен выпускается только с этим claim
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Членство в поездке - целевое поведение контракта (docs/api/review.md); тонкая проверка
    // над уже загруженным Trip.ParticipantIds, без изменений в BusinessLogic.
    protected void EnsureParticipant(Trip trip)
    {
        if (!trip.ParticipantIds.Contains(CurrentUserId))
            throw new ForbiddenException(trip.Id);
    }
}
