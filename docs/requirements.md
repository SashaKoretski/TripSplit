# TripSplit - требования

Функциональные требования, бизнес-правила и измеримые нефункциональные
требования (NFR) системы TripSplit (Web UI). Требования выведены из кода
(`src/TripSplit.BusinessLogic`, `src/TripSplit.WebUI`).

## Functional requirements

| ID | Требование | Источник в коде |
|---|---|---|
| FR-001 | Пользователь может зарегистрироваться, указав имя и email | `AccountController.Register` |
| FR-002 | Уже зарегистрированный пользователь может войти по email без пароля | `AccountController.LoginByEmail` |
| FR-003 | Пользователь может выйти из системы | `AccountController.Logout` |
| FR-004 | Аутентифицированный пользователь может создать поездку (название, валюта ISO 4217); создатель автоматически становится первым участником | `TripsController.Create`, `TripService.CreateAsync` |
| FR-005 | Пользователь может присоединиться к существующей поездке по ее идентификатору | `TripsController.Join`, `TripService.AddParticipantAsync` |
| FR-006 | Участник поездки может пригласить в нее другого уже зарегистрированного пользователя по email | `TripsController.Invite` |
| FR-007 | Пользователь может выбрать одну из своих поездок активной в текущей сессии либо снять выбор | `TripsController.Select/Deselect` |
| FR-008 | Пользователь может посмотреть список своих поездок и детали конкретной поездки (участники, статус, валюта) | `TripsController.Index/Details` |
| FR-009 | Участник активной поездки может добавить трату: название, тип, сумма, скидка, кто оплатил, кто потребители | `ExpensesController.Create`, `ExpenseService.AddAsync` |
| FR-010 | Участник может удалить трату | `ExpensesController.Delete`, `ExpenseService.DeleteAsync` |
| FR-011 | Участник может посмотреть список трат поездки с итоговой суммой | `ExpensesController.Index` |
| FR-012 | Участник может создать чек (название, дата) для поездки | `ReceiptsController.Create`, `ReceiptService.CreateAsync` |
| FR-013 | Участник может загрузить, заменить и удалить фото чека (объектное хранилище S3/MinIO) | `ReceiptsController.UploadImage/DeleteImage` |
| FR-014 | Участник может привязать ранее не привязанные траты к чеку | `ReceiptsController.AttachExpenses` |
| FR-015 | Участник может создать чек и сразу загрузить его фото прямо при добавлении траты | `ExpensesController.Create` (поле `NewReceiptFile`) |
| FR-016 | Участник может завершить поездку; после завершения новые траты/чеки добавлять нельзя, повторное завершение запрещено | `TripsController.Finish`, `TripService.FinishAsync` |
| FR-017 | Система рассчитывает итоговый баланс каждого участника и минимальный набор переводов, закрывающий все долги | `SettlementController.Index`, `DebtSettlementService`, `GreedyDebtMinimizationStrategy` |

## Business rules

| ID | Правило | Источник |
|---|---|---|
| BR-01 | Валюта поездки - код из 3 латинских букв (ISO 4217) | `Trip` (конструктор) |
| BR-02 | Сумма траты (`Value`) строго больше 0; скидка (`Discount`) не отрицательна и не превышает `Value` | `Expense` (конструктор) |
| BR-03 | У траты должен быть минимум один потребитель (`ConsumerIds`) | `Expense` (конструктор) |
| BR-04 | Между потребителями делится поровну именно `EffectiveAmount = Value - Discount` | `Expense.EffectiveAmount`, `DebtSettlementService.ComputeBalances` |
| BR-05 | После перевода поездки в статус `Finished` нельзя добавлять траты и чеки; повторное завершение выбрасывает `TripAlreadyFinishedException` | `TripService.FinishAsync`, `ExpensesController`, `ReceiptsController` |
| BR-06 | Баланс участника = сумма оплаченного минус его доля потребленного по всем тратам поездки; минимизация переводов - жадный алгоритм (наибольший должник переводит наибольшему кредитору), с порогом округления `DebtSettlement:MinTransferAmount` (по умолчанию 0.01) | `DebtSettlementService`, `GreedyDebtMinimizationStrategy` |
| BR-07 | У одного чека может быть не более одного изображения; при удалении чека изображение удаляется каскадно | `receipt_images.receipt_id UNIQUE`, `ReceiptService` → `ReceiptImageService` |
| BR-08 | Повторная регистрация с уже занятым email не создает нового пользователя - выполняется вход в существующий аккаунт | `AccountController.Register` |
| BR-09 | Пригласить в поездку можно только пользователя, уже зарегистрированного в системе (поиск по email) | `TripsController.Invite` |
| BR-10 | Плательщик и все потребители траты должны быть участниками той же поездки | `ExpenseService.EnsureParticipants` |
| BR-11 | Чек, к которому привязывается трата, должен принадлежать той же поездке | `ExpenseService.EnsureReceiptBelongsToTripAsync` |

## Non-functional requirements

Целевые ориентиры для текущей архитектуры (single-instance, без кэша, без
репликации). Числа не измерены - это цели, а не результаты нагрузочного
тестирования.

| ID | Требование | Целевое значение | Как проверяется |
|---|---|---|---|
| NFR-PERF-001 | Время отклика ключевых страниц (`Expenses/Index`, `Settlement/Index`) при типовой нагрузке | p95 < 300 мс при ~10 RPS на одном инстансе | Нагрузочное тестирование; не измерено на текущий момент |
| NFR-REL-001 | Доступность и целостность данных при отказах | Availability ≥ 99% для single-instance развертывания; 0 потерь подтвержденных данных при сбое процесса (soft delete в `trips`/`expenses`, идемпотентный `init.sql`) | Не измерено; фиксируется как цель. Транзакционность операций "запись метаданных чека + загрузка файла в S3" в `ReceiptImageService` требует отдельной проверки (см. открытый вопрос ниже) |
| NFR-SEC-001 | Контроль доступа к данным поездки | 100% действий, привязанных к конкретной поездке (`trip_id`-scoped), должны проверять, что текущий пользователь - участник этой поездки; сессия - `HttpOnly` cookie; все изменяющие POST-формы защищены `ValidateAntiForgeryToken` | Code review + тесты доступа. На текущий момент `TripsController.Details` **не** проверяет членство - зафиксированный пробел, не исправлен (см. открытый вопрос ниже) |

### Открытые технические вопросы

- Не проверено, атомарна ли операция "создать `receipt_images` в БД + загрузить объект в S3" в `ReceiptImageService` (риск для NFR-REL-001 при частичном сбое).
- `NFR-SEC-001` описывает целевое поведение; фактически `TripsController.Details(Guid id)` проверяет только `IsAuthenticated`, не участие пользователя в поездке (`trip.ParticipantIds`). Это расхождение между целевым NFR и текущим кодом - известный gap, требует исправления.
