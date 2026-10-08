# TripSplit REST API

Контракт: [`src/openapi/openapi.yaml`](../../src/openapi/openapi.yaml) (OpenAPI 3.1.0).

## Зачем этот контракт и почему `/api/v1`

До WebLab#2 в проекте не было ни одного JSON/HTTP API-эндпоинта. Web UI
(`TripSplit.WebUI`) - server-rendered ASP.NET Core MVC: Razor views,
cookie-сессия, формы с `ValidateAntiForgeryToken`, без AJAX/fetch к backend
(подтверждено чтением `Program.cs` - только `AddControllersWithViews`,
`app.MapControllerRoute`, без `[ApiController]`). Поэтому новый контракт -
это не версионирование существующего API, а проектирование с нуля.

Версия - `api/v1`, так как публичного API раньше не было (альтернатива
"сохранить старое как v1, новое сделать v2" неприменима - нечего сохранять).

## Авторизация

ADR-001 (WebLab#1) уже разделил механизмы: Web UI остается на email + cookie
(`HttpOnly`-сессия), REST API получает отдельную stateless-авторизацию.
Контракт реализует это как:

- `POST /auth/token {email}` -> `Bearer` JWT, без пароля (как и вход в Web UI).
- Без refresh-токена - при истечении клиент повторно вызывает `/auth/token`.
- Logout как отдельный эндпоинт не нужен: токен stateless, клиент просто
  перестает его использовать.

Любой эндпоинт над `/trips/{tripId}/...` в контракте требует членства
вызывающего пользователя в этой поездке (`403`, если не участник). Это
целевое поведение. **Известный факт**: сейчас даже Web UI
(`TripsController.Details`) эту проверку не делает - только факт входа в
систему. Контракт фиксирует правильное поведение явно, не копируя пробел
в API (см. `review.md`, реализация проверки - задача WebLab#3).

## Карта ресурсов

| Ресурс | Операции | Доменная сущность |
|---|---|---|
| `/auth/token` | выдача JWT | - |
| `/users`, `/users/me`, `/users/{id}` | Create, Read one | `User` |
| `/trips`, `/trips/{id}` | Create, Read one, Read collection (paginated) | `Trip` |
| `/trips/{id}/finish` | переход статуса (RPC, необратимо) | `Trip.Status` |
| `/trips/{id}/participants` | Create (self-join / invite-by-email), Read collection | `TripParticipant` |
| `/trips/{id}/settlement` | Read only (производный, не хранится) | `TripStatistics`/`Transfer` |
| `/trips/{id}/expenses`, `/expenses/{id}` | **Create, Read one, Read collection (paginated), Update (PUT), Partial update (PATCH), Delete** | `Expense` |
| `/trips/{id}/receipts`, `/receipts/{id}` | Create, Read one, Read collection, Delete | `Receipt` |
| `/receipts/{id}/image` | Create/Replace (PUT), Read (302 redirect), Delete | `ReceiptImage` |

### Полный CRUD: `Expense`

`IExpenseService` уже содержит весь набор операций
(`AddAsync`/`GetByIdAsync`/`GetByTripAsync`/`UpdateAsync`/`DeleteAsync`) -
контроллер `ExpensesController` их просто не все использовал. Контракт
покрывает:

- `POST /trips/{tripId}/expenses` - Create
- `GET /expenses/{expenseId}` - Read one
- `GET /trips/{tripId}/expenses` - Read collection (пагинация `page`/`pageSize`)
- `PUT /expenses/{expenseId}` - Update (полная замена)
- `DELETE /expenses/{expenseId}` - Delete (логическое, `deleted_at`)

### Обязательный `PATCH`: `Expense`

`PATCH /expenses/{expenseId}`, семантика - **JSON Merge Patch** (RFC 7396,
media type `application/merge-patch+json`):

- поле, отсутствующее в теле запроса, не изменяется;
- `receiptId: null` явно отвязывает трату от чека (заменяет RPC
  `ReceiptsController.AttachExpenses`/`DetachFromReceiptAsync`);
- `receiptId: "<uuid>"` привязывает к чеку (та же проверка BR-11, что и при
  создании);
- `null` для `name`/`value`/`payerId`/`consumerIds` недопустим (у этих полей
  нет осмысленного "пустого" состояния) - `400`.

Выбор JSON Merge Patch вместо JSON Patch (RFC 6902) - осознанный: у Expense
небольшое, плоское множество полей, точечные list-операции (`add`/`remove`
элемента `consumerIds`) не нужны ни одному сценарию WebLab#1.

## Пагинация, фильтрация, сортировка

Только там, где коллекция может реально расти: `GET /trips` (поездки
пользователя) и `GET /trips/{tripId}/expenses` (траты поездки) - конверт
`{items, page, pageSize, total}`. Остальные коллекции (`participants`,
`receipts` одной поездки) - простые массивы: по сценариям WebLab#1 они
маленькие, добавлять пагинацию "для порядка" не стали (прямое указание
`LAB02.md`, Этап 2: "не добавляй все это формально, если проекту не нужно").
Фильтрация и сортировка не нужны ни одному сценарию - не добавлены.

## Ошибки

Формат - RFC 7807 Problem Details (`application/problem+json`) + стабильное
машиночитаемое поле `code` (см. `components.schemas.Error` в
`openapi.yaml`). Коды `code` соответствуют доменным исключениям
`TripSplit.BusinessLogic.Models.Exceptions.*` 1:1 - таблица в `review.md`.

## Файлы

```text
docs/api/
  README.md              - этот файл
  scenario-mapping.md     - сценарии WebLab#1 -> operationId (Этап 4 LAB02)
  review.md               - семантическое ревью и спорные места (Этап 5 LAB02)
src/openapi/
  openapi.yaml            - контракт OpenAPI 3.1
```

Решение не создавать `docs/weblab02/...` - по уже принятому в WebLab#1
правилу "документация по смыслу, не по номеру лабы" (см. `ai/AI_REVIEW.md`).
Решение держать `docs/api/*.md` как документацию, а `src/openapi/` -
как контракт и инструменты его проверки (не прямо в корне репозитория) -
по запросу пользователя от 2026-10-08.
