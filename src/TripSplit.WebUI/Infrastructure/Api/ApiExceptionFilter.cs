using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.WebUI.Infrastructure.Api;

// Централизованная трансляция доменных исключений в application/problem+json
// (components/schemas/Error в openapi.yaml). Код ошибки - 1:1 с docs/api/review.md.
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var (status, code, title) = context.Exception switch
        {
            ForbiddenException => (403, "forbidden", "Forbidden"),
            UserAlreadyRegisteredException => (409, "user_already_registered", "Conflict"),
            UserNotFoundException => (404, "user_not_found", "Not Found"),
            TripNotFoundException => (404, "trip_not_found", "Not Found"),
            ExpenseNotFoundException => (404, "expense_not_found", "Not Found"),
            ReceiptNotFoundException => (404, "receipt_not_found", "Not Found"),
            ReceiptImageNotFoundException => (404, "receipt_image_not_found", "Not Found"),
            TripAlreadyFinishedException => (409, "trip_already_finished", "Conflict"),
            InvalidExpenseException => (422, "invalid_expense", "Unprocessable Entity"),
            InvalidReceiptImageException => (422, "invalid_receipt_image", "Unprocessable Entity"),
            ArgumentException => (400, "validation_error", "Bad Request"),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0) return;

        _logger.LogWarning(context.Exception, "API domain exception handled: {Code}", code);

        var error = new ApiError
        {
            Title = title,
            Status = status,
            Detail = context.Exception.Message,
            Code = code
        };

        context.Result = new ObjectResult(error)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
