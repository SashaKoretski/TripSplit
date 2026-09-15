using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.WebUI.Infrastructure;

public class DomainExceptionFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempFactory;
    private readonly ILogger<DomainExceptionFilter> _logger;

    public DomainExceptionFilter(ITempDataDictionaryFactory tempFactory, ILogger<DomainExceptionFilter> logger)
    {
        _tempFactory = tempFactory;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var message = context.Exception switch
        {
            InvalidExpenseException e     => e.Message,
            InvalidReceiptImageException e => e.Message,
            ExpenseNotFoundException      => "Трата не найдена",
            ReceiptNotFoundException      => "Чек не найден",
            ReceiptImageNotFoundException => "Изображение чека не найдено",
            TripNotFoundException         => "Поездка не найдена",
            UserNotFoundException         => "Пользователь не найден",
            TripAlreadyFinishedException  => "Поездка уже завершена",
            ArgumentException e           => e.Message,
            _ => null
        };

        if (message is null) return;

        _logger.LogWarning(context.Exception, "Domain exception handled: {Message}", message);

        var temp = _tempFactory.GetTempData(context.HttpContext);
        temp["Error"] = message;

        var referer = context.HttpContext.Request.Headers["Referer"].ToString();
        context.Result = string.IsNullOrEmpty(referer)
            ? new RedirectToActionResult("Index", "Home", null)
            : new RedirectResult(referer);
        context.ExceptionHandled = true;
    }
}
