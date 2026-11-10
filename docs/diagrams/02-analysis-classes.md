# Диаграмма анализа: границы, управления и сущности

![Диаграмма анализа](img/02-analysis-classes.png)

```mermaid
classDiagram
    class LoginWindow {
        <<boundary>>
        +ShowDialog() bool
        +ErrorText string
    }
    class MainWindow {
        <<boundary>>
        +NavigationItems
        +ShowPageAsync(page)
    }
    class EditorWindow {
        <<boundary>>
        +ShowDialog() bool
        +Validate() string
    }
    class AuthService {
        <<control>>
        +SignInAsync(login, password) User
        +SignOut()
    }
    class SessionService {
        <<control>>
        +CurrentUser User
        +CanEditEstimates bool
        +CanViewFinance bool
    }
    class InventoryService {
        <<control>>
        +CreateDeliveryAsync(delivery) Delivery
        +CreateUsageAsync(usage) MaterialUsage
    }
    class DeviationService {
        <<control>>
        +BuildReportAsync(projectId) DeviationReport
    }
    class User {
        <<entity>>
        +Login string
        +PasswordHash string
        +Role UserRole
        +IsActive bool
    }
    class Material {
        <<entity>>
        +Code string
        +Name string
        +StockQuantity decimal
        +MinStock decimal
    }
    class Delivery {
        <<entity>>
        +Number string
        +Date DateTime
    }
    class MaterialUsage {
        <<entity>>
        +Quantity decimal
        +UnitPrice decimal
        +DocumentNumber string
    }
    class Estimate {
        <<entity>>
        +Number string
        +ProjectId int
    }
    class Project {
        <<entity>>
        +Name string
        +ContractSum decimal
        +ProgressPercent int
    }

    LoginWindow --> AuthService : запрос на вход
    MainWindow --> SessionService : права роли
    EditorWindow --> InventoryService : сохранение документа
    AuthService --> User : проверка пароля
    InventoryService --> Material : изменение остатка
    InventoryService --> Delivery
    InventoryService --> MaterialUsage
    DeviationService --> Estimate : план / факт
    DeviationService --> Project
```

Границы — окна WPF (`LoginWindow`, `MainWindow`, `EditorWindow`), управления — сервисы слоя данных и ядра, сущности — классы модели EF Core, отображаемые в таблицы SQL Server.
