namespace TripSplit.TelegramBot.Infrastructure;

// Превращает доменные исключения в понятный пользователю текст (аналог DomainExceptionFilter из WebUI)
public static class DomainErrorMessages
{
    public static string Resolve(Exception ex) => ex switch
    {
        InvalidExpenseException e => e.Message,
        InvalidReceiptImageException e => e.Message,
        ExpenseNotFoundException => "Трата не найдена.",
        ReceiptNotFoundException => "Чек не найден.",
        ReceiptImageNotFoundException => "Изображение чека не найдено.",
        TripNotFoundException => "Поездка не найдена. Проверьте ссылку.",
        UserNotFoundException => "Пользователь не найден.",
        TripAlreadyFinishedException => "Поездка уже завершена.",
        ArgumentException e => e.Message,
        _ => "Что-то пошло не так. Попробуйте еще раз или напишите /help."
    };
}
