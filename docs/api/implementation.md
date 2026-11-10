# Реализация REST API (WebLab#3)

Источник истины - `src/openapi/openapi.yaml` (WebLab#2). Этот документ описывает,
как контракт подключен к существующей бизнес-логике и доступу к данным, и что
было реально проверено.

## Слои

- **DB entity** - результат `SELECT`, собранный через `*Mapper.ReadRow` (например
  `ExpenseMapper.Row`) внутри `TripSplit.DataAccess.Repositories.*`. Наружу из
  Repository не выходит - сразу превращается в domain-модель через `*Mapper.ToDomain`.
- **Domain model** - классы в `TripSplit.BusinessLogic.Models` (`Trip`, `Expense`,
  `User`, ...). Содержат инварианты (конструкторы бросают `ArgumentException`/
  доменные исключения), не содержат деталей хранения (SQL, S3 ключи и т.п. - кроме
  `ReceiptImage.StorageKey`, который остается внутренним и не отдается в DTO).
- **API DTO** - `TripSplit.WebUI.Dtos.V1.*`. Отдельные классы от domain-моделей:
  скрывают `User.GoogleId`, `ReceiptImage.StorageKey`; добавляют поля, которых нет
  в домене (`ReceiptImage.Url`/`UrlExpiresAt` - вычисляются на лету через
  `IReceiptImageService.GetDownloadUrlAsync`/`DownloadUrlLifetime`).

Repository скрывает SQL от бизнес-логики (как и раньше, до WebLab#3 - это не
новое разделение, а то, что уже было). Новый API-слой (`Controllers/Api/V1/*`)
переиспользует существующие `I*Service` без исключений - ни один интерфейс
бизнес-логики не был переписан, только дополнен (см. "Аддитивные правки" ниже).

## operationId -> код (итоговое состояние)

| operationId | Контроллер.Метод | Бизнес-логика |
|---|---|---|
| issueAuthToken | `AuthController.IssueToken` | `IUserService.FindByEmailAsync` + `IJwtTokenService` (новый, транспортный) |
| registerUser | `UsersController.Register` | `IUserService.FindByEmailAsync` + `RegisterAsync` |
| getCurrentUser | `UsersController.GetCurrent` | `IUserService.GetByIdAsync` (id из JWT claim) |
| getUserById | `UsersController.GetById` | `IUserService.GetByIdAsync` |
| listTrips | `TripsController.List` | `ITripService.GetByUserAsync` + пагинация в API-слое |
| createTrip | `TripsController.Create` | `ITripService.CreateAsync` |
| getTripById | `TripsController.GetById` | `ITripService.GetByIdAsync` + membership-check |
| finishTrip | `TripsController.Finish` | `ITripService.FinishAsync` |
| listTripParticipants | `TripsController.ListParticipants` | `Trip.ParticipantIds` + `IUserService.GetByIdAsync` (N+1, как в MVC) |
| addTripParticipant | `TripsController.AddParticipant` | `ITripService.AddParticipantAsync` + `IUserService.FindByEmailAsync` |
| getTripSettlement | `TripsController.GetSettlement` | `IDebtSettlementService.CalculateSettlementAsync` |
| listTripExpenses | `TripsController.ListExpenses` | `IExpenseService.GetByTripAsync` + пагинация |
| createExpense | `TripsController.CreateExpense` | `IExpenseService.AddAsync` |
| getExpenseById | `ExpensesController.GetById` | `IExpenseService.GetByIdAsync` |
| replaceExpense | `ExpensesController.Replace` | `IExpenseService.UpdateAsync` (полная замена) |
| patchExpense | `ExpensesController.Patch` | merge-patch в API-слое (`JsonElement`) + `IExpenseService.UpdateAsync` |
| deleteExpense | `ExpensesController.Delete` | `IExpenseService.DeleteAsync` |
| listTripReceipts | `TripsController.ListReceipts` | `IReceiptService.GetByTripAsync` + `ReceiptDtoFactory` |
| createReceipt | `TripsController.CreateReceipt` | `IReceiptService.CreateAsync` |
| getReceiptById | `ReceiptsController.GetById` | `IReceiptService.GetByIdAsync` + `ReceiptDtoFactory` |
| deleteReceipt | `ReceiptsController.Delete` | `IReceiptService.DeleteAsync` |
| getReceiptImage | `ReceiptsController.GetImage` | `IReceiptImageService.GetDownloadUrlAsync` (302) |
| uploadReceiptImage | `ReceiptsController.UploadImage` | `IReceiptImageService.UploadAsync` |
| deleteReceiptImage | `ReceiptsController.DeleteImage` | `IReceiptImageService.DeleteByReceiptAsync` |

## Аддитивные правки бизнес-логики/доступа к данным

Все - чистые добавления, ни один существующий вызывающий код не менялся:

1. `Expense.CreatedAt`/`UpdatedAt` (новые `init`-свойства) + `ExpenseMapper` теперь
   читает `created_at`/`updated_at` из БД. Колонки уже существовали и уже
   обновлялись (`UpdateAsync` пишет `updated_at = now()`), просто не были
   прочитаны обратно - а контракт их требует (`Expense.createdAt/updatedAt`,
   `required`).
2. `IReceiptImageService.DownloadUrlLifetime` (новое свойство, возвращает уже
   существовавшую приватную константу 15 минут) - чтобы API-слой не задавал
   то же "15 минут" отдельно при расчете `urlExpiresAt`.
3. `UserNotFoundException(string email)` - новый overload конструктора (кроме
   уже существующего `UserNotFoundException(Guid id)`), для `404 user_not_found`
   при поиске по email (`issueAuthToken`, `addTripParticipant`-invite).

## Найденные несоответствия контракта (не исправлены самостоятельно)

- **`addTripParticipant`**: `docs/api/README.md` формулирует блэнкет-правило
  "любой эндпоинт над `/trips/{id}/...` требует членства -> 403", но операция
  описана как self-join (`{}` -> добавить текущего пользователя, FR-005) -
  требовать членство для операции, которая и есть способ его получить,
  противоречиво. Реализовано без membership-check для этой операции (ни для
  self-join, ни для invite-by-email) - совпадает с текущим поведением Web UI
  (`TripsController.Join`/`Invite` тоже не проверяют членство). `403` в списке
  ответов операции сейчас недостижим.
- **`deleteExpense`**: описание в `openapi.yaml` говорит "логическое удаление
  (`deleted_at`)", хотя `ExpenseRepository.DeleteAsync` делает физический
  `DELETE FROM expenses` (колонка `deleted_at` в схеме есть, но не используется
  ни одним SQL-запросом). Наблюдаемое поведение API не меняется (повторный
  `DELETE` -> `404` в обоих случаях) - код оставлен как есть, формулировку в
  контракте стоит поправить отдельным изменением YAML (не входит в этот отчет -
  WebLab#3 не меняет контракт).

## Тесты

- **Unit** (`TripSplit.BusinessLogic.Tests`, MSTest) - не менялись, 123/123 до
  и после каждого этапа WebLab#3.
- **Integration** (`TripSplit.WebUI.Tests`, новый проект, xUnit +
  `WebApplicationFactory<Program>`) - 16 тестов, реальный HTTP через реальный
  Postgres. **Отдельная БД `tripsplit_test`** (не `tripsplit_db`) - переопределяется
  `TRIPSPLIT_TEST_DB`, тот же конвенционный env var, что и у
  `TripSplit.DataAccess.Tests.DatabaseFixture`. Покрыты: успешный запрос,
  400 (невалидные данные), 404 (нет сущности), 401/403 (доступ), 409 (конфликт),
  PATCH (merge-семантика, включая запрет `null` и отвязку чека).
- **Деliberate contract break -> fail -> pass**: в `ExpenseDto.FromDomain`
  временно заменено `EffectiveAmount = e.EffectiveAmount` на `= e.Value` -
  `dotnet test` реально показал `FAIL` (`Expected: 50, Actual: 60`) на тесте
  `CreateExpense_Succeeds_WithCorrectEffectiveAmount`, правка отменена, повторный
  прогон - `PASS`. Сломанная версия не коммитилась.

## Не проверено

- Реальная загрузка файла в S3/MinIO (`uploadReceiptImage`) - MinIO не запущен
  в среде разработки на момент реализации (Docker Desktop недоступен). Код
  дошел до вызова `S3FileStorageService.UploadAsync` корректно (подтверждено
  стеком вызовов упавшего запроса), сам сетевой вызов не проверен. Проверить:
  `docker compose up -d minio minio-init`, затем `PUT /receipts/{id}/image`.
- Swagger UI "Try it out" с реальным Bearer-токеном вручную через браузер -
  проверено только программным образом (`curl`, интеграционные тесты).
