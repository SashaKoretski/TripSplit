workspace "TripSplit" "Учёт совместных расходов в поездках" {

    model {
        user = person "Пользователь" "Организатор и участник поездки. Вносит траты, получает итоговый расчёт долгов."

        tripSplit = softwareSystem "TripSplit" "Считает общие расходы и минимизирует количество переводов между участниками." {

            webUi = container "Web UI" "Многостраничное веб-приложение (MVC): страницы поездок, трат, чеков и расчёта. Подключает BusinessLogic напрямую (in-process)." "ASP.NET Core MVC, Razor Views, C#" "WebBrowser" {
                program         = component "Program (Composition Root)" "Собирает DI-граф, читает конфигурацию, регистрирует логирование, сессию, MVC и глобальный фильтр исключений." "C#"
                controllers     = component "Controllers (Use Cases)" "Home/Account/Trips/Expenses/Receipts/Settlement — 1 контроллер на Use Case из ТЗ." "C# ASP.NET Core MVC"
                tripAwareBase   = component "TripAwareController" "Абстрактная база: разрешение «текущая поездка → редирект», используется контроллерами, работающими только в контексте выбранной поездки." "C#"
                viewModels      = component "ViewModels" "LoginVm / CreateTripVm / CreateExpenseVm / … — привязка форм + подготовка данных для отрисовки." "C# POCO"
                views           = component "Razor Views + _Layout" "Единый layout с шапкой: текущий пользователь, активная поездка и её статус — состояние бизнес-процесса всегда видно пользователю." "Razor"
                webSession      = component "WebAppSession (IWebAppSession)" "Реализация IWebAppSession поверх HttpContext.Session (id пользователя и активной поездки в cookie-based session store)." "C#"
                exceptionFilter = component "DomainExceptionFilter" "IExceptionFilter: маппит доменные исключения BusinessLogic (TripNotFoundException, InvalidExpenseException и др.) в TempData.Error + redirect. OCP." "C#"

                program         -> controllers      "регистрирует"
                program         -> webSession       "регистрирует IWebAppSession → WebAppSession"
                program         -> exceptionFilter  "как глобальный фильтр MVC"
                controllers     -> tripAwareBase    "Expenses/Receipts/Settlement наследуют"
                controllers     -> webSession       "читают/меняют текущий контекст"
                controllers     -> viewModels       "принимают/отдают"
                controllers     -> views            "рендерят View"
                views           -> webSession       "_Layout читает user/trip для шапки"
                exceptionFilter -> controllers      "перехватывает исключения"
            }

            consoleUi = container "Technological UI" "Консольное приложение для системного тестирования всех Use Case. Подключает BusinessLogic напрямую (in-process)." ".NET Console App, C#" {
                cProgram          = component "Program (Composition Root)" "Собирает DI-граф, читает конфигурацию, регистрирует логирование, запускает меню." "C#"
                cMenuRunner       = component "MenuRunner" "Цикл: обновление сессии, отрисовка, полиморфный вызов IMenuItem." "C#"
                cCommands         = component "Commands (IMenuItem)" "Login/Join/CreateTrip/Invite/View*/AddExpense/DeleteExpense/AddReceipt/FinishTrip — 1 класс на Use Case." "C# + DI"
                cCommandDecorator = component "LoggingMenuItemDecorator" "Оборачивает IMenuItem: логирует действия пользователя и исключения (Decorator, OCP)." "C#"
                cAppSession       = component "AppSession + TripSessionRefresher" "Текущий пользователь, активная поездка; синхронизация с БД перед каждой отрисовкой меню." "C#"
                cConsoleIo        = component "ConsoleIO" "Абстракция ввода/вывода (DIP)." "C#"

                cProgram          -> cMenuRunner       "создаёт"
                cMenuRunner       -> cCommandDecorator "итерирует"
                cCommandDecorator -> cCommands         "делегирует"
                cMenuRunner       -> cAppSession       "обновляет + читает"
                cMenuRunner       -> cConsoleIo        "отрисовывает"
                cCommands         -> cAppSession       "изменяют/читают"
                cCommands         -> cConsoleIo        "ввод/вывод"
            }

            businessLogic = container "Business Logic" "Сервисы предметной области: поездки, траты, чеки, расчёт долгов. Class Library, подключается in-process." ".NET Class Library, C#" {
                tripService       = component "TripService" "Жизненный цикл поездки." "C#"
                expenseService    = component "ExpenseService" "CRUD трат, привязка к участникам." "C#"
                receiptService    = component "ReceiptService" "Чеки и их метаданные." "C#"
                userService       = component "UserService" "Работа с пользователями." "C#"
                settlementService = component "DebtSettlementService" "Собирает траты, считает балансы, вызывает стратегию." "C#"
                debtStrategy      = component "IDebtMinimizationStrategy (Greedy)" "Алгоритм минимизации переводов. Параметризуется DebtSettlementOptions. Внедряется через DI (Strategy)." "C#"
                settlementOptions = component "DebtSettlementOptions" "POCO с параметрами (MinTransferAmount). Заполняется из IConfiguration." "C# POCO"

                settlementService -> debtStrategy      "делегирует минимизацию"
                debtStrategy      -> settlementOptions "читает параметры"
            }

            dataAccess = container "Data Access" "Реализация I*Repository (интерфейсы объявлены в BusinessLogic) и подключения к БД." ".NET Class Library, C#" {
                tripRepo    = component "TripRepository"           "Доступ к данным поездок."       "C# + Npgsql"
                expenseRepo = component "ExpenseRepository"        "Доступ к данным трат."          "C# + Npgsql"
                receiptRepo = component "ReceiptRepository"        "Метаданные чеков."              "C# + Npgsql"
                userRepo    = component "UserRepository"           "Доступ к данным пользователей." "C# + Npgsql"
                connFactory = component "NpgsqlConnectionFactory"  "Фабрика подключений (IDbConnectionFactory). Строка подключения из IConfiguration." "Npgsql"
                mappers     = component "Mappers"                  "Мэппинг Row ↔ доменные модели." "C#"

                tripRepo    -> connFactory "открывает"
                expenseRepo -> connFactory "открывает"
                receiptRepo -> connFactory "открывает"
                userRepo    -> connFactory "открывает"
                tripRepo    -> mappers     "использует"
                expenseRepo -> mappers     "использует"
                receiptRepo -> mappers     "использует"
                userRepo    -> mappers     "использует"
            }

            logger = container "Logging" "Фасад над Serilog: конфигурирует провайдер Microsoft.Extensions.Logging, чтобы остальные контейнеры зависели только от ILogger<T> из абстракций." ".NET Class Library, C# + Serilog" {
                loggingExt     = component "LoggingExtensions"     "Метод AddTripSplitLogging(services, config) — регистрирует Serilog как ILoggerProvider в DI." "C#"
                loggerFactoryC = component "TripSplitLoggerFactory" "Создаёт standalone ILoggerFactory для сценариев без DI (тесты, утилиты)." "C#"

                loggingExt -> loggerFactoryC "может использовать"
            }

            configuration = container "Configuration" "Внешний файл с настройками: строка подключения к БД, параметры бизнес-логики, конфигурация Serilog." "appsettings.json" "Config"

            logFiles = container "Log Files" "Rolling log-файлы: действия пользователя, исключения, диагностика. Ротация по дням." "Файловая система, logs/tripsplit-*.log" "Database"

            database = container "База данных" "Пользователи, поездки, траты, метаданные чеков." "PostgreSQL" "Database"
        }

        # L1
        user -> tripSplit "Ведёт учёт расходов в поездках"

        # L2
        user          -> webUi         "Использует"                             "HTTPS"
        user          -> consoleUi     "Использует для системного тестирования" "терминал"
        webUi         -> businessLogic "Вызывает сервисы"                       "in-process"
        consoleUi     -> businessLogic "Вызывает сервисы"                       "in-process"
        businessLogic -> dataAccess    "Через I*Repository (реализации в DA)"   "in-process"
        dataAccess    -> database      "Чтение/запись"                          "TCP/SQL"

        # L2 — логирование и конфигурация
        webUi     -> logger        "Подключает через AddTripSplitLogging" "in-process"
        consoleUi -> logger        "Подключает через AddTripSplitLogging" "in-process"
        webUi     -> configuration "Читает при старте"                    "file I/O"
        consoleUi -> configuration "Читает при старте"                    "file I/O"
        logger    -> logFiles      "Пишет структурированные события"      "Serilog File Sink"

        # L3 — компоненты Web UI на сервисы бизнес-логики
        controllers -> tripService       "вызывают"
        controllers -> expenseService    "вызывают"
        controllers -> receiptService    "вызывают"
        controllers -> userService       "вызывают"
        controllers -> settlementService "вызывают"
        webSession  -> userService       "лениво подгружает текущего пользователя"
        webSession  -> tripService       "лениво подгружает активную поездку"

        # L3 — компоненты Console UI на сервисы бизнес-логики
        cCommands -> tripService       "вызывают"
        cCommands -> expenseService    "вызывают"
        cCommands -> receiptService    "вызывают"
        cCommands -> userService       "вызывают"
        cCommands -> settlementService "вызывают"

        # L3 — сервисы BusinessLogic на репозитории
        tripService       -> tripRepo    "использует"
        expenseService    -> expenseRepo "использует"
        receiptService    -> receiptRepo "использует"
        userService       -> userRepo    "использует"
        settlementService -> tripRepo    "читает поездку"
        settlementService -> expenseRepo "читает траты"

        connFactory -> database "TCP"

        # L3 — логирование и конфигурация на уровне компонентов
        program           -> loggingExt        "AddTripSplitLogging(services, config)"
        program           -> configuration     "ConfigurationBuilder.AddJsonFile"
        program           -> settlementOptions "заполняет из IConfiguration"
        exceptionFilter   -> loggingExt        "пишет исключения"
        cProgram          -> loggingExt        "AddTripSplitLogging(services, config)"
        cProgram          -> configuration     "ConfigurationBuilder.AddJsonFile"
        cProgram          -> settlementOptions "заполняет из IConfiguration"
        cCommandDecorator -> loggingExt        "пишет действия пользователя и исключения"
        loggingExt        -> logFiles          "Serilog File Sink"
        connFactory       -> configuration     "GetConnectionString(\"Postgres\")"
    }

    views {
        systemContext tripSplit "L1_Context" "Уровень 1 — Контекст системы" {
            include *
            autolayout lr
        }

        container tripSplit "L2_Containers" "Уровень 2 — Контейнеры" {
            include *
            autolayout lr
        }

        component webUi "L3_WebUI" "Уровень 3 — Компоненты Web UI" {
            include *
            autolayout lr
        }

        component consoleUi "L3_ConsoleUI" "Уровень 3 — Компоненты Console UI" {
            include *
            autolayout lr
        }

        component businessLogic "L3_BusinessLogic" "Уровень 3 — Компоненты Business Logic" {
            include *
            autolayout lr
        }

        component dataAccess "L3_DataAccess" "Уровень 3 — Компоненты Data Access" {
            include *
            autolayout lr
        }

        component logger "L3_Logger" "Уровень 3 — Компоненты Logger" {
            include *
            autolayout lr
        }

        styles {
            element "Person" {
                shape person
                background #08427b
                color #ffffff
            }

            element "External" {
                background #999999
                color #ffffff
            }

            element "Database" {
                shape cylinder
                background #1168bd
                color #ffffff
            }

            element "WebBrowser" {
                shape webBrowser
                background #438dd5
                color #ffffff
            }

            element "Config" {
                shape folder
                background #f2c14e
                color #000000
            }

            element "Container" {
                background #438dd5
                color #ffffff
            }

            element "Component" {
                background #85bbf0
                color #000000
            }
        }
    }
}