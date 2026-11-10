# Описание интеграционных тестов API (WebLab#3)

Проект `src/TripSplit.WebUI.Tests` (xUnit, `WebApplicationFactory<Program>`,
реальный HTTP-запрос к реальному Postgres - отдельная БД `tripsplit_test`,
не `tripsplit_db`). Всего 16 тестов в 3 файлах. Для каждого - что делает и
что именно проверяет (`Assert`).

## AuthAndUsersApiTests.cs - вход и регистрация (4 теста)

| Тест | Что делает | Что проверяет |
|---|---|---|
| `RegisterUser_ThenAuthToken_Succeeds` | Регистрирует пользователя, затем запрашивает токен по тому же email | Регистрация вернула `201` и заголовок `Location`; выдача токена вернула `200` |
| `RegisterUser_DuplicateEmail_Returns409` | Регистрирует пользователя, затем регистрирует второго с тем же email | Повтор вернул `409` с кодом ошибки `user_already_registered` (строгая семантика create, не "тихий" вход) |
| `AuthToken_UnknownEmail_Returns404` | Запрашивает токен для email, которого не существует | Ответ - `404` (нет такого пользователя) |
| `GetTripById_WithoutToken_Returns401` | Обращается к `GET /trips/{id}` без заголовка `Authorization` | Ответ - `401` (без токена доступ закрыт) |

## TripsApiTests.cs - поездки (6 тестов)

| Тест | Что делает | Что проверяет |
|---|---|---|
| `CreateTrip_ThenGetById_Succeeds` | Создает поездку (`currency: "gel"`), затем читает ее по `id` | Создание вернуло `status: "active"` и `currency: "GEL"` (валюта приведена к верхнему регистру); чтение по `id` вернуло `200` |
| `CreateTrip_EmptyName_Returns400` | Пытается создать поездку с пустым `name` | Ответ - `400` (базовая валидация запроса) |
| `GetTripById_UnknownTrip_Returns404` | Запрашивает поездку со случайным, не существующим `id` | Ответ - `404` с кодом `trip_not_found` |
| `GetTripById_NotAParticipant_Returns403` | Один пользователь создает поездку, другой (не состоящий в ней) пытается ее прочитать | Ответ - `403` (авторизован, но доступа к чужой поездке нет) |
| `FinishTrip_Twice_SecondCallReturns409` | Завершает поездку, затем вызывает завершение повторно | Первый вызов - `200`; второй - `409` с кодом `trip_already_finished` (нельзя завершить уже завершенную) |
| `AddParticipant_SelfJoin_IsIdempotent` | Второй пользователь (не создатель) присоединяется к поездке самостоятельно, потом повторяет тот же запрос | Первое присоединение - `201` (создан новый участник); повторное - `200`, без ошибки и без дубликата |

## ExpensesApiTests.cs - траты (6 тестов)

| Тест | Что делает | Что проверяет |
|---|---|---|
| `CreateExpense_Succeeds_WithCorrectEffectiveAmount` | Создает трату с `value: 60`, `discount: 10` | Поле `effectiveAmount` в ответе равно `50` (`value - discount` посчитано верно) |
| `CreateExpense_DiscountExceedsValue_Returns422` | Создает трату, где `discount` (20) больше `value` (10) | Ответ - `422` с кодом `invalid_expense` (нарушение бизнес-правила, не просто "неверный формат") |
| `PatchExpense_OmittedField_KeepsExistingValue` | Создает трату с `name: "Dinner"`, затем шлет `PATCH` только с полем `value` | После `PATCH` `name` остался `"Dinner"` (не передано - не изменилось), а `value` обновился до `55` |
| `PatchExpense_NullReceiptId_DetachesReceipt` | Создает чек и трату, привязывает трату к чеку через `PATCH {receiptId: "<uuid>"}`, затем отправляет `PATCH {receiptId: null}` | Привязка - `200`; отвязка - `200`, и поле `receiptId` в ответе становится `null` (JSON Merge Patch: `null` явно очищает поле) |
| `PatchExpense_NullName_Returns400` | Отправляет `PATCH {name: null}` | Ответ - `400` (у `name` нет "пустого" состояния, `null` для него запрещен) |
| `DeleteExpense_Twice_SecondCallReturns404` | Удаляет трату, затем повторяет удаление того же `id` | Первое удаление - `204`; повторное - `404` (ресурса уже нет) |

## Как прогнать самостоятельно

```bash
cd src
dotnet test TripSplit.WebUI.Tests/TripSplit.WebUI.Tests.csproj
```

Отдельный тест - флагом `--filter`, например:

```bash
dotnet test TripSplit.WebUI.Tests/TripSplit.WebUI.Tests.csproj --filter "PatchExpense_NullReceiptId_DetachesReceipt"
```

Подробнее про саму механику тестов (отдельная БД, фикстура, почему именно
такой набор) - в `docs/api/implementation.md`, раздел "Тесты".
