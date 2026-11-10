# ЛР 1 — Unit-тесты, покрытие и отчётность: рабочие материалы

Этот файл — рабочая документация к `LAB01_EVIDENCE.txt` (корень репозитория, не коммитится
в git). Здесь — таблицы охвата и техник подготовки данных, наблюдения по количеству
процессов и справочник команд. Формальный статус по каждому пункту задания — в
`LAB01_EVIDENCE.txt`.

Редакция от 2026-11-10: независимый повторный проход по ветке `lab1_test`, сброшенной до
точки расхождения с `main` (commit `c6dfd09`, без синхронизации с более поздними изменениями
`main`). Весь код тестов и вспомогательные классы ниже перепроверены заново (сборка, все
прогоны, покрытие, allure, случайный порядок, офлайн-режим, подсчёт процессов выполнены
заново в этой сессии, см. §3 и §5, числа и PID — реальные, из текущего прогона, не
унаследованные). Структура Arrange-Act-Assert во всех тестах: вызов проверяемого метода не
прячется внутри `Assert.Throws(Async)`/`Assert.Equal`, а выносится в отдельную переменную
`act` (фаза Act), после которой идёт только фаза Assert — проверено построчно по всем файлам
`TripSplit.BusinessLogic.Tests/*.cs` и `TripSplit.DataAccess.Tests/**/*.cs`. В тестовых файлах
комментариев нет (проверено `grep` по `^\s*//|///|/\*\*`, совпадений в исходниках — 0, только
в сгенерированных `obj/**`). Итоговое число unit-тестов: 188 (BusinessLogic, MSTest) + 101
(DataAccess, xUnit, `Category!=Integration`) = 289 unit-тестов; плюс 8 интеграционных тестов
(`Category=Integration`, реальный Postgres) — вне границ ЛР1, см. §1.

## 1. Матрица охвата: классы и public-методы (Т1, Т6)

### TripSplit.BusinessLogic (бизнес-логика)

| Класс | Public-методы | Suite | Позитив/негатив |
|---|---|---|---|
| `UserService` | RegisterAsync, GetByIdAsync, FindByGoogleIdAsync, FindByEmailAsync | `UserServiceTests.cs` (+classical `UserServiceClassicalTests.cs`) | да/да на каждый |
| `TripService` | CreateAsync, GetByIdAsync, GetByUserAsync, AddParticipantAsync, FinishAsync | `TripServiceTests.cs` (+classical `TripServiceClassicalTests.cs`) | да/да |
| `ExpenseService` | AddAsync, GetByIdAsync, GetByTripAsync, UpdateAsync, DeleteAsync, AttachToReceiptAsync, DetachFromReceiptAsync | `ExpenseServiceTests.cs` | да/да |
| `ReceiptService` | CreateAsync, GetByIdAsync, GetByTripAsync, DeleteAsync | `ReceiptServiceTests.cs` | да/да |
| `ReceiptImageService` | UploadAsync, GetByReceiptAsync, GetDownloadUrlAsync, DeleteByReceiptAsync | `ReceiptImageServiceTests.cs` | да/да |
| `DebtSettlementService` | CalculateSettlementAsync | `DebtSettlementServiceTests.cs` | да/да |
| `GreedyDebtMinimizationStrategy` | Minimize | `GreedyDebtMinimizationStrategyTests.cs` | да/да (классический, без mock) |
| `Trip` (модель) | ctor | `Models/TripTests.cs` | да/да |
| `User` (модель) | ctor | `Models/UserTests.cs` | да/да |
| `Expense` (модель) | ctor | `Models/ExpenseTests.cs` | да/да |
| `Receipt` (модель) | ctor | `Models/ReceiptTests.cs` | да/да |
| `ReceiptImage` (модель) | ctor | `Models/ReceiptImageTests.cs` | да/да |
| `Transfer` (модель) | ctor | `Models/TransferTests.cs` | да/да |
| `TripStatistics` (модель) | ctor | `Models/TripStatisticsTests.cs` | да/да |

Приватные/защищенные методы (`EnsureParticipants`, `EnsureReceiptBelongsToTripAsync` в
`ExpenseService`, `ComputePerUserSpent`/`ComputeBalances` в `DebtSettlementService` и т.п.)
намеренно не тестируются напрямую (Т6) — их поведение проверяется через публичные методы,
которые их вызывают.

### TripSplit.DataAccess (доступ к данным)

| Класс | Public-методы | Suite (unit, без БД) | Интеграционный suite (реальный Postgres, вне LAB01) |
|---|---|---|---|
| `UserRepository` | GetByIdAsync, GetByGoogleIdAsync, GetByEmailAsync, AddAsync, UpdateAsync, DeleteAsync | `Repositories/UserRepositoryUnitTests.cs` | `UserRepositoryTests.cs` (`[Trait("Category","Integration")]`) |
| `TripRepository` | GetByIdAsync, GetAllAsync, GetByUserAsync, AddAsync, UpdateAsync, DeleteAsync | `Repositories/TripRepositoryUnitTests.cs` | — |
| `ExpenseRepository` | GetByIdAsync, GetByTripAsync, AddAsync, UpdateAsync, DeleteAsync | `Repositories/ExpenseRepositoryUnitTests.cs` | `ExpenseRepositoryTests.cs` (`[Trait("Category","Integration")]`) |
| `ReceiptRepository` | GetByIdAsync, GetByTripAsync, AddAsync, DeleteAsync | `Repositories/ReceiptRepositoryUnitTests.cs` | — |
| `ReceiptImageRepository` | GetByReceiptAsync, AddAsync, DeleteAsync | `Repositories/ReceiptImageRepositoryUnitTests.cs` | — |
| `UserMapper`/`TripMapper`/`ExpenseMapper`/`ReceiptMapper`/`ReceiptImageMapper` | Map/ReadRow/ToDomain/ToDbType/ToDbStatus | `Mapping/*MapperTests.cs` | — |
| `DatabaseInitializer` | InitializeAsync | `Infrastructure/DatabaseInitializerTests.cs` | — |
| `S3FileStorageService` | UploadAsync, DeleteAsync, GetPresignedUrlAsync | `Storage/S3FileStorageServiceTests.cs` | — |

**Границы unit-теста для слоя данных (З0/П к Т1, формулировка из плана ЛР1).** Репозитории
используют "сырой" ADO.NET (Npgsql) без ORM. Чтобы протестировать их как unit, а не
integration, весь стек `DbConnection`/`DbCommand`/`DbDataReader`/`DbParameter` подделан в
памяти (`TestDoubles/FakeAdo.cs`) — тест не открывает сокет и не видит реальной БД. Это
покрывает: валидацию аргументов (Guid.Empty и т.п. — ArgumentException без обращения к БД),
корректность SQL/маппинга при успешном ответе, и доменные исключения, которые зависят только
от "affected rows" (`EntityNotFoundException`). Единственное, что этим способом принципиально
не проверить — ветки `catch (PostgresException e) when (e.SqlState == UniqueViolation)`,
потому что `PostgresException` не имеет публичного конструктора и может быть порождена только
реальным сервером Postgres. Эти ветки (уникальность email/id) проверены интеграционным тестом
`UserRepositoryTests.AddAsync_Duplicate_Email_Throws_DuplicateKey` (реальная тестовая БД) — и
явно вне границ LAB01, см. П к ЛР2. По той же причине `NpgsqlConnectionFactory` (тонкая
обертка над реальным TCP-соединением) и `S3StorageOptions` (DTO без логики) не имеют
unit-тестов — им нечего проверять без реальной инфраструктуры.

## 2. Таблица техник подготовки тестовых данных (З3)

Технику не повторяет одна фраза на весь набор — ниже конкретная техника для каждой
смысловой группы тестов, с примером теста и обоснованием "почему".

| Тест (пример) | Техника | Почему |
|---|---|---|
| `TripTests.Ctor_CurrencyLengthNot3_ThrowsArgumentException("RU"/"RUBL"/"")` | Граничные значения длины строки (2 / 3 / 4 символа вокруг границы `== 3`) | Условие в коде — явная проверка длины; граница — самый вероятный источник off-by-one ошибки |
| `ExpenseTests.Ctor_NonPositiveValue_ThrowsInvalidExpenseException(0, -1)` + `Ctor_SmallestPositiveValue_IsAccepted` | Граничные значения вокруг `value <= 0` | Технически три смежных класса (отрицательное/ноль/минимально положительное) — проверены все |
| `ExpenseTests.Ctor_DiscountEqualsValue_IsAccepted` / `Ctor_DiscountExceedsValueByOneCent_Throws` | Граничные значения вокруг `discount > value` | `==` — разрешено, `+0.01` — запрещено; классическая ошибка `>` vs `>=` |
| `UserServiceTests.RegisterAsync_InvalidArguments_Throws` (6 `DataRow`) | Классы эквивалентности (пустая строка / строка из пробелов) × (name/email/googleId) | Комбинаторный перебор по трём независимым полям через `IsNullOrWhiteSpace` |
| `GreedyDebtMinimizationStrategyTests.Minimize_OneDebtorTwoCreditors_TwoTransfers` / `TwoDebtorsOneCreditor` / `MultipleParties_BalancesZeroOut` | Переходы состояний жадного алгоритма (1:1, 1:N, N:1, N:M) | Алгоритм — стейт-машина по двум указателям; эти формы входа — единственные структурно различные переходы |
| `TripServiceTests.FinishAsync_AlreadyFinished_Throws` / `AddParticipantAsync_TripFinished_Throws` | Переходы состояний `TripStatus` (Active→Finished — разрешено; Finished→* — запрещено) | Доменное правило завязано на состояние, не на входные данные |
| `ExpenseMapperTests.ToDbType_EachEnumValue_MapsToExpectedDbString` / `ReadRow_EachDbTypeString_RoundTripsToEnumValue` (`[Theory]` по всем 5 значениям `ExpenseType`) | Комбинаторный перебор (полный, не попарный — мощность входа мала: 1 параметр, 5 значений) | Малая мощность делает полный перебор дешевле попарного |
| `UserRepositoryUnitTests.GetByIdAsync_*` (строка найдена / не найдена / Guid.Empty) | Классы эквивалентности результата запроса (0 строк / 1 строка) + граница (Guid.Empty) | Для data-access слоя значим не столько ввод, сколько форма ответа БД |
| `ReceiptImageTests.Ctor_NonPositiveFileSize_ThrowsArgumentException(0, -1)` / `SmallestPositiveFileSize_IsAccepted(1)` | Граничные значения (0 / 1 байт) | PDF прямо просит технику "граничные значения" как пример |
| `S3FileStorageServiceTests.*_S3Throws_WrapsIntoStorageException` | Тестирование исключений (ожидаемый результат — Exception) | Т2 ЛР1: отдельная техника для веток перевода чужого исключения в доменное |

## 3. Количество процессов на прогон (Т12)

Измерено через диагностический журнал VSTest (`--diag:<file>.log`), который построчно
фиксирует создание процесса test host с PID и временной меткой —
`artifacts/lab01/logs/process-count/` (перегенерировано в этой сессии, 2026-11-10).

- **Один `dotnet test` на один csproj (BusinessLogic.Tests, 188 тестов, MSTest 4.3.2):**
  ровно один процесс test host (`diag-mstest.host.26-11-10_11-16-00_77690_5.log`, PID 77690).
  Все 188 тестов выполнились внутри этого одного процесса.
- **`dotnet test TripSplit.sln` (оба тестовых проекта):** два процесса test host, по одному на
  проект (`diag-all.host.26-11-10_11-16-12_32097_5.log` — testhost.dll PID 1908 внутри, и
  `diag-all.host.26-11-10_11-16-12_92051_5.log` — testhost.dll PID 19780 внутри; оба журнала
  стартовали в один и тот же момент `11:16:12`, т.е. VSTest поднимает процессы на обе тестовые
  сборки практически одновременно, а не строго последовательно).
- **Вывод:** гранулярность процесса — **один OS-процесс на тестовую сборку (csproj)**, а не
  на тест-класс и не на тест-метод. Это конфигурирует `DotnetTestHostmanager`
  (`Microsoft.TestPlatform.TestHostRuntimeProvider`) — штатный провайдер test host для
  `dotnet test`/VSTest: он поднимает ровно один `testhost` (через `dotnet exec testhost.dll`)
  на каждую переданную тестовую DLL. Чтобы получить параллельные процессы на несколько
  сборок, нужно явно вызывать `vstest.console.exe` с несколькими `--Tests`/source-путями и
  флагом параллельности сборок, либо STR runsettings `<RunConfiguration><MaxCpuCount>`.
- **Внутри одного процесса** параллелизм — потоковый, не процессный:
  xUnit (`TripSplit.DataAccess.Tests`) по умолчанию параллелит тест-классы потоками внутри
  одного test host (collection-level); MSTest 4.3.2 параллелит тест-методы только при явном
  `[assembly: Parallelize]` — в проекте этот атрибут не установлен, поэтому тесты внутри
  test host идут последовательно (что и позволяет `RandomizeTestOrder` детерминированно
  переставлять их по seed — см. `random-order.runsettings`).

## 4. Команды (справочник; подробные логи — в `LAB01_EVIDENCE.txt`)

```bash
# Обычный прогон (из src/)
dotnet test TripSplit.BusinessLogic.Tests/TripSplit.BusinessLogic.Tests.csproj
dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj --filter "Category!=Integration"

# Случайный порядок
dotnet test TripSplit.BusinessLogic.Tests/TripSplit.BusinessLogic.Tests.csproj --settings TripSplit.BusinessLogic.Tests/random-order.runsettings
TRIPSPLIT_TEST_SEED=42 dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj --filter "FullyQualifiedName~Mapping"

# Офлайн-режим (только mock/fake, без БД/сети — тот же фильтр, что и "обычный прогон" DataAccess)
dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj --filter "Category!=Integration"

# Покрытие (строки + ветвления)
dotnet test TripSplit.BusinessLogic.Tests/TripSplit.BusinessLogic.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ../artifacts/lab01/coverage/businesslogic
dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj --filter "Category!=Integration" --collect:"XPlat Code Coverage" --results-directory ../artifacts/lab01/coverage/dataaccess
reportgenerator -reports:"../artifacts/lab01/coverage/*/*/coverage.cobertura.xml" -targetdir:"../artifacts/lab01/coverage/report" -reporttypes:"Html;TextSummary"

# Allure-отчет (TripSplit.DataAccess.Tests — xUnit; для MSTest-проекта официального адаптера Allure нет, см. LAB01_EVIDENCE.txt Т9)
# Allure.Xunit добавляет по файлу на тест в allure-results при каждом прогоне и не очищает
# его сам (только --clean у allure-commandline стирает ЦЕЛЕВУЮ папку ОТЧЁТА, не исходные
# результаты) — при повторных локальных прогонах без очистки result-файлы накапливаются
# между сессиями и отчёт перестаёт соответствовать последнему прогону. Поэтому перед каждым
# прогоном удаляйте allure-results вручную:
rm -rf TripSplit.DataAccess.Tests/bin/Debug/net8.0/allure-results
dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj --filter "Category!=Integration"
npx allure-commandline generate TripSplit.DataAccess.Tests/bin/Debug/net8.0/allure-results -o ../artifacts/lab01/allure-report-dataaccess --clean

# Интеграционные тесты репозиториев (реальный Postgres; требуют TRIPSPLIT_TEST_DB)
dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj --filter "Category=Integration"
```

## 5. Офлайн-режим (Т11): реальный прогон без доступа к интернету

Проверено Docker-контейнером с `--network none` (без доступа ни к NuGet, ни к реальному
Postgres), 2026-11-10. Чтобы контейнер не ходил в сеть даже за restore, NuGet-пакеты
восстановлены заранее в смонтированный `~/.nuget/packages` отдельным прогоном с сетью,
офлайн-прогон использует их через `--no-restore`:

```bash
# 1) разовый restore с сетью (заполняет общий кэш пакетов)
docker run --rm -v ~/.nuget/packages:/root/.nuget/packages -v "$PWD":/repo -w /repo/src \
  mcr.microsoft.com/dotnet/sdk:8.0 dotnet restore TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj

# 2) прогон unit-тестов БЕЗ сети, из локального кэша пакетов
docker run --rm --network none -v ~/.nuget/packages:/root/.nuget/packages:ro -v "$PWD":/repo -w /repo/src \
  mcr.microsoft.com/dotnet/sdk:8.0 dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj \
  --filter "Category!=Integration" --no-restore
```

Результат: **101/101 unit-тестов (mock/fake, `Category!=Integration`) прошли** внутри
контейнера без сети — ожидаемо, т.к. весь ADO.NET-стек подделан в памяти (`TestDoubles/FakeAdo.cs`)
и реальных сокетов не открывается. Тот же прогон для `Category=Integration` (8 тестов,
реальный Postgres через `DatabaseFixture`) в том же офлайн-контейнере **упал с
`Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432` / `SocketException: Connection
refused`** — что и требует Т11: "тесты только с mock должны в таком случае проходить
успешно", а тесты, которым нужна реальная инфраструктура — закономерно падают без сети/БД.
`TripSplit.BusinessLogic.Tests` (188 тестов, чистый Moq, вообще без I/O) в том же офлайн-
контейнере проходит полностью без изменений (188/188).

## 6. Фактические числа покрытия (Т14, 2026-11-10)

```
dotnet test .../TripSplit.BusinessLogic.Tests.csproj --collect:"XPlat Code Coverage" ...
dotnet test .../TripSplit.DataAccess.Tests.csproj --filter "Category!=Integration" --collect:"XPlat Code Coverage" ...
reportgenerator -reports:"...coverage.cobertura.xml" -targetdir:"...report" -reporttypes:"Html;TextSummary"
```

Line coverage: **89.2%** (1142/1280 покрываемых строк). Branch coverage: **91.9%** (502/546
ветвей). Method coverage: 94.6% (177/187). Полная текстовая сводка —
`artifacts/lab01/coverage/report/Summary.txt` (не коммитится, регенерируется командами выше).
