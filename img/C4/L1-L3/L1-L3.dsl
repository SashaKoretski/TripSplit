workspace "TripSplit" "Учёт совместных расходов в поездках" {

    model {
        user = person "Пользователь" "Организатор и участник поездки. Вносит траты, получает итоговый расчёт долгов."

        googleAuth = softwareSystem "Google Identity" "OAuth 2.0 провайдер для аутентификации." "External"

        tripSplit = softwareSystem "TripSplit" "Считает общие расходы и минимизирует количество переводов между участниками." {

            frontend = container "Frontend SPA" "Интерфейс поездок, трат, статистики." "Blazor WebAssembly, C#" "WebBrowser" {
                pages         = component "Pages & UI Components" "Страницы поездок, форма трат, экран статистики." "Blazor Components"
                stateService  = component "State Service" "Хранит текущую поездку, кэш данных." "Scoped C# services"
                apiClient     = component "API Client" "Типизированные вызовы Backend API." "HttpClient + Refit"
                authService   = component "Auth Service" "OAuth 2.0 flow, хранение JWT." "Blazor Authentication"

                pages -> stateService "читает/меняет"
                pages -> apiClient "запрашивает данные"
                pages -> authService "проверяет авторизацию"
                apiClient -> authService "берёт JWT для заголовка"
            }

            backend = container "Backend API" "REST-API: поездки, траты, чеки, расчёт долгов. Тонкий слой над BusinessLogic." "ASP.NET Core Web API, C#" {
                controllers = component "API Controllers" "HTTP endpoints, валидация, маппинг DTO." "ASP.NET Core Controllers"
                authMw      = component "Auth Middleware" "Проверяет JWT от Google." "ASP.NET Core Auth"

                controllers -> authMw "проходит через"
            }

            consoleUi = container "Technological UI" "Консольное приложение для системного тестирования всех Use Case. Подключает BusinessLogic напрямую (in-process)." ".NET Console App, C#" {
                program          = component "Program (Composition Root)" "Собирает DI-граф, читает конфигурацию, регистрирует логирование, запускает меню." "C#"
                menuRunner       = component "MenuRunner" "Цикл: обновление сессии, отрисовка, полиморфный вызов IMenuItem." "C#"
                commands         = component "Commands (IMenuItem)" "Login/Join/CreateTrip/Invite/View*/AddExpense/DeleteExpense/AddReceipt/FinishTrip — 1 класс на Use Case." "C# + DI"
                commandDecorator = component "LoggingMenuItemDecorator" "Оборачивает IMenuItem: логирует действия пользователя и исключения (Decorator, OCP)." "C#"
                appSession       = component "AppSession + TripSessionRefresher" "Текущий пользователь, активная поездка; синхронизация с БД перед каждой отрисовкой меню." "C#"
                consoleIo        = component "ConsoleIO" "Абстракция ввода/вывода (DIP)." "C#"

                program -> menuRunner "создаёт"
                menuRunner -> commandDecorator "итерирует"
                commandDecorator -> commands "делегирует"
                menuRunner -> appSession "обновляет + читает"
                menuRunner -> consoleIo "отрисовывает"
                commands -> appSession "изменяют/читают"
                commands -> consoleIo "ввод/вывод"
            }

            businessLogic = container "Business Logic" "Сервисы предметной области: поездки, траты, чеки, расчёт долгов. Class Library, подключается in-process." ".NET Class Library, C#" {
                tripService       = component "TripService" "Жизненный цикл поездки." "C#"
                expenseService    = component "ExpenseService" "CRUD трат, привязка к участникам." "C#"
                receiptService    = component "ReceiptService" "Чеки и их метаданные." "C#"
                userService       = component "UserService" "Работа с пользователями." "C#"
                settlementService = component "DebtSettlementService" "Собирает траты, считает балансы, вызывает стратегию." "C#"
                debtStrategy      = component "IDebtMinimizationStrategy (Greedy)" "Алгоритм минимизации переводов. Параметризуется DebtSettlementOptions. Внедряется через DI (Strategy)." "C#"
                settlementOptions = component "DebtSettlementOptions" "POCO с параметрами (MinTransferAmount). Заполняется из IConfiguration." "C# POCO"

                settlementService -> debtStrategy "делегирует минимизацию"
                debtStrategy -> settlementOptions "читает параметры"
            }

            dataAccess = container "Data Access" "Реализация I*Repository (интерфейсы объявлены в BusinessLogic) и подключения к БД." ".NET Class Library, C#" {
                tripRepo     = component "TripRepository" "Доступ к данным поездок." "C# + Npgsql"
                expenseRepo  = component "ExpenseRepository" "Доступ к данным трат." "C# + Npgsql"
                receiptRepo  = component "ReceiptRepository" "Метаданные чеков." "C# + Npgsql"
                userRepo     = component "UserRepository" "Доступ к данным пользователей." "C# + Npgsql"
                connFactory  = component "NpgsqlConnectionFactory" "Фабрика подключений (IDbConnectionFactory). Строка подключения из IConfiguration." "Npgsql"
                mappers      = component "Mappers" "Мэппинг Row ↔ доменные модели." "C#"
                storageAdapt = component "FileStorageAdapter" "Загрузка/чтение файлов чеков." "MinIO SDK"

                tripRepo    -> connFactory "открывает"
                expenseRepo -> connFactory "открывает"
                receiptRepo -> connFactory "открывает"
                userRepo    -> connFactory "открывает"
                tripRepo    -> mappers "использует"
                expenseRepo -> mappers "использует"
                receiptRepo -> mappers "использует"
                userRepo    -> mappers "использует"
            }

            logger = container "Logging" "Фасад над Serilog: конфигурирует провайдер Microsoft.Extensions.Logging, чтобы остальные контейнеры зависели только от ILogger<T> из абстракций." ".NET Class Library, C# + Serilog" {
                loggingExt      = component "LoggingExtensions" "Метод AddTripSplitLogging(services, config) — регистрирует Serilog как ILoggerProvider в DI." "C#"
                loggerFactoryC  = component "TripSplitLoggerFactory" "Создаёт standalone ILoggerFactory для сценариев без DI (тесты, утилиты)." "C#"

                loggingExt -> loggerFactoryC "может использовать"
            }

            configuration = container "Configuration" "Внешний файл с настройками: строка подключения к БД, параметры бизнес-логики, конфигурация Serilog." "appsettings.json" "Config"

            logFiles = container "Log Files" "Rolling log-файлы: действия пользователя, исключения, диагностика. Ротация по дням." "Файловая система, logs/tripsplit-*.log" "Database"
        
            database    = container "База данных" "Пользователи, поездки, траты, метаданные чеков." "PostgreSQL" "Database"
            fileStorage = container "Хранилище файлов" "Blob-хранилище для фото чеков." "MinIO (S3-совместимое)" "Database"
        }

        # L1
        user -> tripSplit "Ведёт учёт расходов в поездках"
        tripSplit -> googleAuth "Аутентифицирует пользователей"

        # L2
        user          -> frontend      "Использует"                             "HTTPS"
        user          -> consoleUi     "Использует для системного тестирования" "терминал"
        frontend      -> backend       "Вызовы REST API"                        "HTTPS/JSON"
        frontend      -> googleAuth    "OAuth 2.0 flow"                         "HTTPS"
        backend       -> googleAuth    "Валидирует JWT"                         "HTTPS"
        backend       -> businessLogic "Вызывает сервисы"                       "in-process"
        consoleUi     -> businessLogic "Вызывает сервисы"                       "in-process"
        businessLogic -> dataAccess    "Через I*Repository (реализации в DA)"   "in-process"
        dataAccess    -> database      "Чтение/запись"                          "TCP/SQL"
        dataAccess    -> fileStorage   "Загрузка/чтение файлов"                 "S3 API"

        # L2 — логирование и конфигурация
        consoleUi -> logger        "Подключает через AddTripSplitLogging"  "in-process"
        backend   -> logger        "Подключает через AddTripSplitLogging"  "in-process"
        consoleUi -> configuration "Читает при старте"                     "file I/O"
        backend   -> configuration "Читает при старте"                     "file I/O"
        logger    -> logFiles      "Пишет структурированные события"       "Serilog File Sink"

        # L3 — component ↔ container / межконтейнерные вызовы компонентов
        apiClient   -> backend    "REST"          "HTTPS/JSON"
        authService -> googleAuth "OAuth"         "HTTPS"
        authMw      -> googleAuth "Проверяет JWT" "HTTPS"

        controllers -> tripService       "вызывает"
        controllers -> expenseService    "вызывает"
        controllers -> receiptService    "вызывает"
        controllers -> userService       "вызывает"
        controllers -> settlementService "вызывает"

        commands -> tripService       "вызывают"
        commands -> expenseService    "вызывают"
        commands -> receiptService    "вызывают"
        commands -> userService       "вызывают"
        commands -> settlementService "вызывают"

        tripService       -> tripRepo     "использует"
        expenseService    -> expenseRepo  "использует"
        receiptService    -> receiptRepo  "использует"
        receiptService    -> storageAdapt "сохраняет/читает файлы"
        userService       -> userRepo     "использует"
        settlementService -> tripRepo     "читает поездку"
        settlementService -> expenseRepo  "читает траты"

        connFactory  -> database    "TCP"
        storageAdapt -> fileStorage "S3 API"

        # L3 — логирование и конфигурация на уровне компонентов
        program          -> loggingExt       "AddTripSplitLogging(services, config)"
        program          -> configuration    "ConfigurationBuilder.AddJsonFile"
        program          -> settlementOptions "заполняет из IConfiguration"
        commandDecorator -> loggingExt       "пишет действия пользователя и исключения"
        loggingExt       -> logFiles         "Serilog File Sink"
        connFactory      -> configuration    "GetConnectionString(\"Postgres\")"
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

        component backend "L3_Backend" "Уровень 3 — Компоненты Backend" {
            include *
            autolayout lr
        }

        component frontend "L3_Frontend" "Уровень 3 — Компоненты Frontend" {
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
