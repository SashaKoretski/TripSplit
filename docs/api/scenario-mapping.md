# Сценарии WebLab#1 -> operationId

Источник сценариев - корневой `README.md` и `docs/details.md` (SC-001..003).
Предусловие всех трех сценариев - аутентификация, вынесена отдельно, чтобы
не повторять в каждой таблице.

## Предусловие всех сценариев: вход

| Step | operationId | HTTP method | Path | Expected result |
|---|---|---|---|---|
| Получить access-токен по email | `issueAuthToken` | POST | `/auth/token` | `200` с `accessToken`; `404 user_not_found`, если email не зарегистрирован |
| (если пользователь новый) зарегистрироваться | `registerUser` | POST | `/users` | `201` с новым `User`, либо `200` с уже существующим (BR-08) |

## SC-001 - Создание поездки и приглашение участников

| Step | operationId | HTTP method | Path | Expected result |
|---|---|---|---|---|
| Создать поездку (название, валюта) | `createTrip` | POST | `/trips` | `201`, `Trip.status = active`, создатель - единственный участник |
| Увидеть список своих поездок | `listTrips` | GET | `/trips` | `200`, страница с созданной поездкой |
| Открыть детали поездки | `getTripById` | GET | `/trips/{tripId}` | `200`, если участник; `403`, если нет |
| Пригласить зарегистрированного участника по email | `addTripParticipant` | POST | `/trips/{tripId}/participants` | `201` с добавленным `User`; `404 user_not_found`, если email не зарегистрирован (BR-09) |
| Альт. A1: присоединиться самому по id поездки | `addTripParticipant` | POST | `/trips/{tripId}/participants` | `201` (тело `{}`); `200`, если уже участник |
| Увидеть участников поездки | `listTripParticipants` | GET | `/trips/{tripId}/participants` | `200`, массив `User` |
| Ошибка: пригласить/присоединиться к завершенной поездке | `addTripParticipant` | POST | `/trips/{tripId}/participants` | `409 trip_already_finished` |

## SC-002 - Добавление траты с чеком

| Step | operationId | HTTP method | Path | Expected result |
|---|---|---|---|---|
| Открыть форму (список участников, список чеков поездки) | `listTripParticipants`, `listTripReceipts` | GET | `/trips/{tripId}/participants`, `/trips/{tripId}/receipts` | `200` |
| Альт.: создать новый чек "на лету" | `createReceipt` | POST | `/trips/{tripId}/receipts` | `201` с новым `Receipt` |
| Загрузить фото к новому чеку | `uploadReceiptImage` | PUT | `/receipts/{receiptId}/image` | `200` с `ReceiptImage`; `413 file_too_large`, если файл больше 10 МБ; `422 invalid_receipt_image`, если дошел до бизнес-логики, но content-type недопустим |
| Создать трату | `createExpense` | POST | `/trips/{tripId}/expenses` | `201` с `Expense.effectiveAmount = value - discount` |
| Ошибка: поездка завершена | `createExpense` | POST | `/trips/{tripId}/expenses` | `409 trip_already_finished` |
| Ошибка: скидка больше суммы / пустой список потребителей | `createExpense` | POST | `/trips/{tripId}/expenses` | `422 invalid_expense` |
| Увидеть список трат с итоговой суммой | `listTripExpenses` | GET | `/trips/{tripId}/expenses` | `200`, страница трат |
| Удалить трату | `deleteExpense` | DELETE | `/expenses/{expenseId}` | `204`; повторный вызов - `404` |
| Привязать ранее не привязанную трату к чеку | `patchExpense` | PATCH | `/expenses/{expenseId}` | `200`, тело `{"receiptId": "<uuid>"}` |
| Отвязать трату от чека | `patchExpense` | PATCH | `/expenses/{expenseId}` | `200`, тело `{"receiptId": null}` |
| Изменить сумму/скидку/плательщика существующей траты | `replaceExpense` или `patchExpense` | PUT/PATCH | `/expenses/{expenseId}` | `200`, пересчитанный `effectiveAmount` |

## SC-003 - Завершение поездки и расчет долгов

| Step | operationId | HTTP method | Path | Expected result |
|---|---|---|---|---|
| Альт. A1: предварительный просмотр расчета до завершения | `getTripSettlement` | GET | `/trips/{tripId}/settlement` | `200`, доступно при `status = active` |
| Завершить поездку | `finishTrip` | POST | `/trips/{tripId}/finish` | `200`, `Trip.status = finished` |
| Ошибка: повторное завершение | `finishTrip` | POST | `/trips/{tripId}/finish` | `409 trip_already_finished`, статус не меняется |
| Увидеть итог и переводы | `getTripSettlement` | GET | `/trips/{tripId}/settlement` | `200`, `totalSpent`, `perUserSpent`, `transfers` |

## Проверка полноты

- Нет шага UI (из `docs/details.md`, раздел "Полная карта экранов"), которому
  не хватает operationId: все действия `TripsController`, `ExpensesController`,
  `ReceiptsController`, `SettlementController`, `AccountController` покрыты.
- Нет эндпоинта контракта, который не нужен ни одному из трех сценариев,
  кроме явно инфраструктурных: `GET /users/{userId}` и `GET /users/me`
  (нужны, чтобы клиент API мог получить данные пользователя без доступа к
  server-side сессии, которой у stateless API нет).
- CRUD (`Expense`: Create/Read one/Read collection/Update/Delete) и `PATCH`
  реально достижимы в рамках SC-002.
