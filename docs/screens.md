# TripSplit - карта экранов Web UI

Web UI - многостраничное приложение (MVC + Razor), маршруты стандартные
ASP.NET Core (`{controller}/{action}/{id?}`). Ниже - карта экранов и их
связь со сценариями SC-001..SC-003.

| Экран | Файл view | Контроллер/action | Сценарий | Комментарий |
|---|---|---|---|---|
| Вход/регистрация | `Views/Account/Login.cshtml` | `AccountController.Login/Register/LoginByEmail` | предусловие SC-001..003 | Одна страница с двумя формами (регистрация и вход по email) |
| Список поездок | `Views/Trips/Index.cshtml` | `TripsController.Index/Create/Join` | SC-001 | Формы создания и присоединения к поездке на одной странице |
| Детали поездки | `Views/Trips/Details.cshtml` | `TripsController.Details/Invite/Finish/Select` | SC-001 (invite), SC-003 (finish) | Список участников, форма приглашения, кнопка завершения поездки |
| Список трат | `Views/Expenses/Index.cshtml` | `ExpensesController.Index/Delete` | SC-002 | Показывает плательщика и потребителей каждой траты, итоговую сумму |
| Добавление траты | `Views/Expenses/Create.cshtml` | `ExpensesController.Create` | SC-002 | Форма с полями трат + опциональным созданием/загрузкой чека |
| Список чеков | `Views/Receipts/Index.cshtml` | `ReceiptsController.Index/Delete/UploadImage/DeleteImage` | SC-002 (альт. ветка) | Показывает привязанные к чеку траты и превью фото |
| Создание чека | `Views/Receipts/Create.cshtml` | `ReceiptsController.Create` | SC-002 (альт. ветка) | Отдельное создание чека без немедленной привязки трат |
| Привязка трат к чеку | `Views/Receipts/AttachExpenses.cshtml` | `ReceiptsController.AttachExpenses` | SC-002 (альт. ветка, FR-014) | Список ранее не привязанных трат для выбора |
| Итог/расчет долгов | `Views/Settlement/Index.cshtml` | `SettlementController.Index` | SC-003 | Общая сумма, траты по участникам, список переводов |
| Ошибка | `Views/Home/Error.cshtml` | `HomeController` / `DomainExceptionFilter` | сквозной (все сценарии, ветки ошибок) | Общая страница ошибки для необработанных исключений |
| Layout | `Views/Shared/_Layout.cshtml` | - | сквозной | Шапка: текущий пользователь, активная поездка и ее статус |

## Покрытие сценариев экранами

Все экраны, необходимые для трех ключевых сценариев (SC-001, SC-002, SC-003),
реализованы - пробелов в UI-покрытии не найдено.

Единственное несоответствие - не UI-навигационное, а поведенческое: экран
"Детали поездки" открывается по `id` из URL без проверки, что текущий
пользователь - участник этой поездки (см. `requirements.md`, NFR-SEC-001,
и известный gap в `TripsController.Details`).
