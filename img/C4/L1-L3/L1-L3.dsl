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

            backend = container "Backend API" "REST-API: поездки, траты, чеки, расчёт долгов." "ASP.NET Core Web API, C#" {
                controllers   = component "API Controllers" "HTTP endpoints, валидация, маппинг DTO." "ASP.NET Core Controllers"
                authMw        = component "Auth Middleware" "Проверяет JWT от Google." "ASP.NET Core Auth"

                tripService       = component "TripService" "Жизненный цикл поездки." "C#"
                expenseService    = component "ExpenseService" "CRUD трат, привязка к участникам." "C#"
                receiptService    = component "ReceiptService" "Чеки и их фотографии." "C#"
                userService       = component "UserService" "Работа с пользователями." "C#"
                settlementService = component "DebtSettlementService" "Собирает траты, считает балансы, вызывает стратегию." "C#"
                debtStrategy      = component "IDebtMinimizationStrategy (Greedy)" "Алгоритм минимизации переводов. Внедряется через DI." "C# Strategy Pattern"

                tripRepo      = component "TripRepository" "Доступ к данным поездок." "EF Core"
                expenseRepo   = component "ExpenseRepository" "Доступ к данным трат." "EF Core"
                receiptRepo   = component "ReceiptRepository" "Метаданные чеков." "EF Core"
                userRepo      = component "UserRepository" "Доступ к данным пользователей." "EF Core"
                storageAdapt  = component "FileStorageAdapter" "Загрузка/чтение файлов чеков." "MinIO SDK"

                controllers -> authMw "проходит через"
                controllers -> tripService "вызывает"
                controllers -> expenseService "вызывает"
                controllers -> receiptService "вызывает"
                controllers -> userService "вызывает"
                controllers -> settlementService "вызывает"

                tripService       -> tripRepo    "использует"
                expenseService    -> expenseRepo "использует"
                receiptService    -> receiptRepo "использует"
                receiptService    -> storageAdapt "сохраняет/читает файлы"
                userService       -> userRepo    "использует"

                settlementService -> tripRepo    "читает поездку"
                settlementService -> expenseRepo "читает траты"
                settlementService -> debtStrategy "делегирует минимизацию"
            }

            database    = container "База данных" "Пользователи, поездки, траты, метаданные чеков." "PostgreSQL" "Database"
            fileStorage = container "Хранилище файлов" "Blob-хранилище для фото чеков." "MinIO (S3-совместимое)" "Database"
        }

        # L1
        user -> tripSplit "Ведёт учёт расходов в поездках"
        tripSplit -> googleAuth "Аутентифицирует пользователей"

        # L2
        user      -> frontend    "Использует"                  "HTTPS"
        frontend  -> backend     "Вызовы REST API"             "HTTPS/JSON"
        frontend  -> googleAuth  "OAuth 2.0 flow"              "HTTPS"
        backend   -> googleAuth  "Валидирует JWT"              "HTTPS"
        backend   -> database    "Чтение/запись"               "TCP/SQL"
        backend   -> fileStorage "Загрузка/чтение файлов"      "S3 API"

        # L3 — component ↔ container
        apiClient    -> backend     "REST-вызовы" "HTTPS/JSON"
        authService  -> googleAuth  "OAuth flow"  "HTTPS"
        authMw       -> googleAuth  "Проверяет JWT" "HTTPS"
        tripRepo     -> database    "SQL"
        expenseRepo  -> database    "SQL"
        receiptRepo  -> database    "SQL"
        userRepo     -> database    "SQL"
        storageAdapt -> fileStorage "S3 API"
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