# СтройКонтроль — набросок WPF-приложения на C#

**Тема курсовой работы:** «Сетевое программное средство управления строительными материалами и анализа отклонений от сметы на строительном объекте».

Клиент-серверное приложение для строительной организации: справочник материалов и поставщиков, сметы по объектам, приходные накладные, акты списания и автоматический анализ отклонений «план — факт» по количеству, цене и стоимости. Все данные хранятся в общей базе **Microsoft SQL Server**, к которой одновременно подключаются несколько рабочих мест по сети.

---

## 1. Целевые пользователи (Target Users)

| Роль | Что делает в системе | Права |
|---|---|---|
| **Инженер ПТО** | Ведёт сметы и позиции смет, анализирует отклонения, экспортирует отчёты | Справочники, сметы, документы, отчёты (смету меняет только он и администратор) |
| **Снабженец** | Закупает материалы в рамках сметы: ведёт поставщиков, оформляет поступления | Поставщики и поступления; видит смету, отклонения и остаток бюджета, но смету не редактирует |
| **Прораб** | Работает с фактом на объекте: принимает материал, оформляет акты списания, контролирует остатки | Только поступления и списания; справочники и сметы — чтение |
| **Бухгалтер** | Формирует и выгружает финансовую отчётность | Просмотр всех финансовых разделов + экспорт отчётов (CSV для Excel) |
| **Администратор** | Управляет учётными записями и ролями | Полный доступ + сотрудники |

## 2. Ключевые возможности (Key Features)

- **Аутентификация и авторизация.** Вход по логину/паролю, пароли — PBKDF2-HMAC-SHA256 с солью (100 000 итераций). Разграничение прав по ролям: часть команд и пунктов меню недоступна.
- **Панель управления.** KPI (объекты, стоимость запасов, план/факт, отклонение), исполнение смет по объектам, материалы, требующие закупки, затраты по месяцам, лента последних операций.
- **Полный CRUD по ключевым сущностям:** материалы, поставщики, объекты, сметы и позиции смет, поступления, списания, сотрудники.
- **Мастер-деталь для смет:** перечень смет ↔ позиции выбранной сметы с плановой стоимостью.
- **Анализ отклонений от сметы.** Таблица «план / факт» по количеству, цене и стоимости с процентами, порогами «в норме / внимание / критично», фильтрами и экспортом в CSV.
- **Согласованные остатки.** Поступления и списания меняют остаток материала и сам документ в **одной транзакции** (откат при ошибке), включая редактирование и удаление документа.
- **Оптимистичные обновления UI.** Строка добавляется/изменяется/удаляется в списке сразу, при ошибке БД изменения откатываются и показывается уведомление.
- **Состояния интерфейса:** индикаторы загрузки, пустые состояния (нет данных / ничего не найдено), всплывающие уведомления, сообщения об ошибках, адаптивная компоновка (сворачиваемое меню, перестраиваемая сетка KPI, «резиновые» диаграммы).
- **Демо-данные.** При первом запуске база наполняется реалистичным набором: 5 сотрудников, 6 поставщиков, 16 материалов, 4 объекта, 5 смет, поступления и списания за последние полгода.

## 3. Технологии

| Слой | Технологии |
|---|---|
| Клиент | C# 12, .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), XAML-стили и ControlTemplate'ы без сторонних UI-библиотек |
| Данные | EF Core 8, Microsoft.EntityFrameworkCore.SqlServer (ADO.NET — Microsoft.Data.SqlClient), SQL Server 2019/2022 (в т.ч. бесплатный Express), `IDbContextFactory` |
| Инфраструктура | Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration (appsettings.json) |
| Тесты | xUnit (модульные тесты расчёта отклонений и хеширования паролей) |

## 4. Структура решения

```
KursovaI.sln
├── src/
│   ├── StroyControl.Core/        # домен: сущности, перечисления, DTO, интерфейсы, бизнес-правила
│   │   ├── Entities/             # User, Supplier, Material, Project, Estimate(+Item), Delivery(+Item), MaterialUsage
│   │   ├── Enums/                # роли, статусы, единицы измерения, критичность отклонений
│   │   ├── Dtos/                 # DeviationRow, DeviationReport, DeviationCalculator, DashboardData
│   │   ├── Interfaces/           # репозитории, сервисы, сессия
│   │   ├── Services/             # AuthService, UserService, SessionService, CsvReportExporter
│   │   └── Security/             # PasswordHasher
│   │
│   ├── StroyControl.Data/        # доступ к SQL Server
│   │   ├── AppDbContext.cs       # DbSet'ы, аудит, версия строки (оптимистическая блокировка)
│   │   ├── Configurations/       # IEntityTypeConfiguration на каждую сущность
│   │   ├── Repositories/         # RepositoryBase<T> + репозитории сущностей
│   │   ├── Services/             # InventoryService, DeviationService, DashboardService, DatabaseHealthService
│   │   ├── Seed/                 # DemoDataSeeder, DatabaseInitializer
│   │   └── ServiceCollectionExtensions.cs
│   │
│   └── StroyControl.App/         # WPF-клиент
│       ├── ViewModels/           # по одной модели на страницу + Editors/ для диалогов
│       ├── Views/                # LoginWindow, MainWindow, страницы, Dialogs/
│       ├── Resources/Theme/      # Colors, Converters, Controls (стили и шаблоны)
│       ├── Resources/Templates/  # ViewTemplates (VM → View), EditorTemplates (формы диалогов)
│       ├── Controls/             # LoadingOverlay, EmptyState, KpiCard, ToastHost
│       ├── Converters/, Services/, Startup/, Behaviors/, Models/
│       └── appsettings.json      # строка подключения к SQL Server
│
├── tests/StroyControl.Core.Tests # xUnit-тесты бизнес-логики
├── database/                     # schema.sql — создание БД, reset.sql — полный сброс
├── docs/architecture.md          # описание архитектуры, ER-модели и алгоритмов
├── Directory.Build.props         # общие свойства сборки (C# 12, nullable, RollForward)
└── Directory.Packages.props      # единый список версий NuGet (Central Package Management)
```

## 5. Быстрый старт

### Требования
- Windows 10/11, **.NET SDK 8.0** (сборка возможна и более новым SDK — 9.x/10.x, если установлен targeting pack .NET 8)
- **.NET Desktop Runtime 8.0** для запуска (если его нет, приложение стартует на более новом рантайме благодаря `<RollForward>Major</RollForward>`)
- Microsoft SQL Server 2019/2022, включая бесплатный **SQL Server Express** (локально или в сети); клиентские инструменты — SSMS и/или `sqlcmd`

### 1) Подключить базу данных SQL Server (один раз)

Подробная пошаговая инструкция — ниже, в разделе **5.1**. Кратко: установить SQL Server Express, выполнить `database/schema.sql`, прописать строку подключения в `appsettings.json`.

Проверка, что всё получилось:
```powershell
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT name FROM StroyControl.sys.tables ORDER BY name"
```
Должно вывестись 9 таблиц: `deliveries`, `delivery_items`, `estimate_items`, `estimates`, `material_usages`, `materials`, `projects`, `suppliers`, `users`.

### 1.1) Скрипты базы данных

| Файл | Что делает | Когда нужен |
|---|---|---|
| `database/schema.sql` | создаёт базу `StroyControl`, SQL-логин `stroycontrol`, 9 таблиц и 2 представления (T-SQL) | при первом развёртывании и после переустановки SQL Server |
| `database/reset.sql` | удаляет все таблицы и представления (данные теряются, база и логин остаются) | когда нужно «начать с нуля»: после сброса приложение снова создаст схему и демо-данные |

Скрипты идемпотентны: каждый объект создаётся с проверкой наличия (`IF DB_ID(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`), поэтому повторный запуск не ломает базу и не удаляет данные.

> Таблицы можно вообще не создавать вручную: при первом запуске приложение выполняет `EnsureCreated` и создаёт схему само. Скрипт нужен, когда базу разворачивает администратор или у учётной записи приложения нет прав на создание объектов.

Строка подключения задаётся в `src/StroyControl.App/appsettings.json`:
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=.\\SQLEXPRESS;Database=StroyControl;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```
`Trusted_Connection=True` — вход под текущей учётной записью Windows (пароль нигде не хранится). `TrustServerCertificate=True` нужен потому, что `Microsoft.Data.SqlClient` по умолчанию требует шифрование и валидный сертификат сервера — для учебного локального сервера сертификат самоподписанный.

Для работы нескольких рабочих мест укажите в строке подключения адрес сервера и SQL-логин (подробности в 5.1):
```
Server=192.168.1.10,1433;Database=StroyControl;User Id=stroycontrol;Password=stroycontrol;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Альтернатива правке файла — переменная окружения `STROYCONTROL_CONNECTION`: она имеет приоритет над `appsettings.json` и удобна, когда приложение запускают на чужом компьютере или в лаборатории.
```powershell
$env:STROYCONTROL_CONNECTION = "Server=.\SQLEXPRESS;Database=StroyControl;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run --project src/StroyControl.App
```

### 2) Собрать и запустить
```bash
dotnet restore
dotnet build
dotnet run --project src/StroyControl.App
```

### 3) Войти
| Логин | Пароль | Роль |
|---|---|---|
| `admin` | `admin123` | Администратор |
| `smeta` | `smeta123` | Инженер ПТО |
| `snab` | `snab123` | Снабженец |
| `prorab` | `prorab123` | Прораб |
| `buh` | `buh123` | Бухгалтер |

На экране входа есть кнопки быстрого входа демонстрационными ролями.

> Если сервер SQL Server недоступен, приложение предложит **демонстрационный режим** (EF Core InMemory): интерфейс полностью работоспособен, но данные не сохраняются после закрытия. Отключается флагом `Application:DemoModeAllowed: false`.

### 4) Тесты
```bash
dotnet test
```

### 5) Миграции EF Core (по желанию)
Схема создаётся через `EnsureCreated`, поэтому миграции не обязательны. При необходимости:
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/StroyControl.Data --startup-project src/StroyControl.App
dotnet ef database update --project src/StroyControl.Data --startup-project src/StroyControl.App
```

### 5.1 Подключение SQL Server шаг за шагом (Windows)

**Шаг 1. Установить SQL Server Express.** Варианты:
- установщик с сайта Microsoft: «SQL Server 2022 Express» (тип установки «Базовая»);
- либо через winget, если пакет доступен в вашей версии каталога: `winget search "SQL Server"`.

По умолчанию создаётся именованный экземпляр `.\SQLEXPRESS` (то есть `ИМЯ_ПК\SQLEXPRESS`), служба называется `MSSQL$SQLEXPRESS`. Для управления удобно поставить **SQL Server Management Studio (SSMS)** — она же пригодится для проверки таблиц и прав.

**Шаг 2. Проверить, что служба запущена** (PowerShell от имени администратора):
```powershell
Get-Service -Name "MSSQL*"       # Status должен быть Running
net start 'MSSQL$SQLEXPRESS'     # если остановлена
```
Проверка подключения:
```powershell
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT @@VERSION"
```
`-E` — Windows-аутентификация (вход под текущим пользователем Windows). Установившаяся по умолчанию учётная запись администратора SQL Server имеет права `sysadmin`, поэтому ей доступны создание базы и таблиц.

**Шаг 3. Создать базу и схему.** Из корня репозитория:
```powershell
sqlcmd -S .\SQLEXPRESS -E -i database\schema.sql
```
В SSMS: `File → Open → File…` → `database/schema.sql` → **Execute (F5)**.
Скрипт создаёт базу `StroyControl`, SQL-логин `stroycontrol`, 9 таблиц и 2 представления; в конце печатает список объектов (`BASE TABLE` — 9, `VIEW` — 2).

**Шаг 4. Проверить результат:**
```powershell
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT name FROM StroyControl.sys.tables ORDER BY name"
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT COUNT(*) AS Users FROM StroyControl.dbo.users"
```
Если `sqlcmd` не найден — он входит в пакет SQL Server (каталог `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn`); либо используйте SSMS.

**Шаг 5. Прописать строку подключения** в `src/StroyControl.App/appsettings.json` (по умолчанию она уже такая):
```
Server=.\SQLEXPRESS;Database=StroyControl;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```
Разбор полей: `Server` — экземпляр (`.\SQLEXPRESS`, `localhost`, `192.168.1.10,1433`), `Database` — имя базы, `Trusted_Connection=True` — вход под учётной записью Windows, `TrustServerCertificate=True` — не проверять самоподписанный сертификат, `MultipleActiveResultSets=True` — несколько активных запросов в одном подключении.

**Шаг 6. Запустить приложение:**
```powershell
dotnet run --project src/StroyControl.App
```
Первый запуск создаёт (если нужно) таблицы и наполняет базу демо-данными: 5 сотрудников, поставщики, ~20 материалов, 3 объекта, сметы, поступления и списания. Повторно они не дублируются — признак «база наполнена» — записи в `dbo.users`.

**Шаг 7. Убедиться, что режим не демонстрационный.** «Настройки» → внизу источник данных: должно быть **`Microsoft SQL Server (сетевая БД)`** и «Подключение активно», а не «Демо-режим (данные в памяти)».

#### Доступ с других рабочих мест (несколько ПК по сети)

Важно: **Windows-аутентификация по сети работает только в домене Active Directory.** В рабочей группе удалённые клиенты должны подключаться по SQL-логину — он уже создан скриптом (`stroycontrol` / `stroycontrol`), но нужно разрешить смешанный режим и сетевой доступ.

1. **Включить TCP/IP и фиксированный порт.** SQL Server Configuration Manager → `SQL Server Network Configuration → Protocols for SQLEXPRESS → TCP/IP` → `Enabled`; вкладка `IP Addresses` → раздел `IPAll`: `TCP Port = 1433`, `TCP Dynamic Ports` — очистить. Перезапустить службу `SQL Server (SQLEXPRESS)`.
2. **Служба SQL Server Browser** (нужна для подключения по имени экземпляра): `Set-Service SQLBrowser -StartupType Automatic; Start-Service SQLBrowser`.
3. **Смешанный режим аутентификации.** SSMS → правый клик по серверу → `Properties → Security → SQL Server and Windows Authentication mode` → OK → перезапуск службы:
   ```powershell
   net stop 'MSSQL$SQLEXPRESS'; net start 'MSSQL$SQLEXPRESS'
   ```
4. **Брандмауэр:**
   ```powershell
   New-NetFirewallRule -DisplayName "SQL Server 1433" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
   New-NetFirewallRule -DisplayName "SQL Server Browser" -Direction Inbound -Protocol UDP -LocalPort 1434 -Action Allow
   ```
5. **Проверить с другой машины:**
   ```powershell
   sqlcmd -S 192.168.1.10,1433 -U stroycontrol -P stroycontrol -Q "SELECT DB_NAME(), SUSER_NAME()"
   ```
6. **Строка подключения на клиентах** (`appsettings.json` на каждом ПК):
   ```
   Server=192.168.1.10,1433;Database=StroyControl;User Id=stroycontrol;Password=stroycontrol;TrustServerCertificate=True;MultipleActiveResultSets=True;
   ```
   Пароль поменяйте и в SSMS (`ALTER LOGIN stroycontrol WITH PASSWORD = N'...'`), и в строке подключения.

#### Если что-то не подключается

| Симптом | Причина | Что делать |
|---|---|---|
| `Login failed for user 'ИМЯ_ПК\Пользователь'` | у учётной записи Windows нет прав на экземпляр | подключиться под администратором SQL Server или использовать SQL-логин |
| `Login failed for user 'stroycontrol'` | сервер в режиме «только Windows-аутентификация» / другой пароль | включить смешанный режим (шаг 3 выше), сверить пароль |
| `A network-related or instance-specific error...` / таймаут | служба остановлена, TCP/IP выключен, порт занят | `Get-Service MSSQL*`, включить TCP/IP, проверить порт 1433 и брандмауэр |
| `Cannot open database "StroyControl" requested by the login` | база не создана или у логина нет пользователя в ней | выполнить `database/schema.sql`; проверить `CREATE USER ... FOR LOGIN` |
| `The certificate chain was issued by an authority that is not trusted` | `Microsoft.Data.SqlClient` требует шифрование | добавить `TrustServerCertificate=True` в строку подключения |
| `Cannot open database ... The login failed` при первом запуске на чистом сервере | базы ещё нет, а у учётной записи нет прав `dbcreator` | выполнить `database/schema.sql` под администратором |
| Приложение предлагает демо-режим | сервер недоступен, а `DemoModeAllowed = true` | устранить причину выше либо согласиться (данные не сохранятся) |
| Данные «не сохраняются» после перезапуска | работа идёт в демо-режиме (InMemory) | в «Настройках» источник должен быть `Microsoft SQL Server` |

Полный сброс: `sqlcmd -S .\SQLEXPRESS -E -i database\reset.sql`, затем снова запустить приложение (схема и демо-данные создадутся заново).

## 6. Как считается отклонение от сметы

Для каждой пары «строительный объект — материал»:

```
план:  PlanQty = Σ позиций смет, PlanPrice = PlanCost / PlanQty, PlanCost = Σ(Qty × Price)
факт:  FactQty = Σ списаний,        FactCost = Σ(Qty × UnitPrice списания)
Δ кол-во  = FactQty  − PlanQty
Δ цена    = FactPrice − PlanPrice
Δ стоимость = FactCost − PlanCost
Δ %       = Δ стоимость / PlanCost × 100
```

Классификация: **< 5 % — в норме**, **5–10 % — внимание**, **≥ 10 % — критично** (экономия отображается отрицательным значением).

## 7. Бизнес-правила и технические решения

- **Транзакционность.** `InventoryService` оформляет поступление/списание, изменение и удаление документа в транзакции вместе с корректировкой остатка (`StockQuantity`). При сбое сети данные остаются согласованными.
- **Оптимистическая блокировка.** У каждой сущности есть `RowVersion` (concurrency token). Если два пользователя изменили одну запись, второй получит сообщение «Запись уже изменена другим пользователем» вместо потери данных.
- **Аудит.** `CreatedAt` / `UpdatedAt` проставляются автоматически в `AppDbContext.SaveChanges`.
- **Ссылочная целостность.** Удаление материала/объекта/поставщика, на который ссылаются документы, блокируется на уровне внешних ключей (`RESTRICT`) и сопровождается понятным сообщением.
- **Пароли.** В БД хранятся только хеш и соль, пароли не логируются и не передаются между клиентами.
- **Деньги.** `decimal(18,2)` для сумм и цен, `decimal(18,3)` для количеств.

## 8. Что реализовано в интерфейсе

| Страница | Возможности |
|---|---|
| Вход | логин/пароль, быстрый вход по ролям, ошибки подключения |
| Панель управления | 4 KPI-карточки, исполнение смет, дефицит материалов, диаграмма затрат, лента операций |
| Материалы | поиск, фильтр по категории и остатку, CRUD, остатки и статусы, стоимость запасов |
| Поставщики | поиск, рейтинг надёжности, CRUD, количество закреплённых материалов |
| Объекты | статусы, сроки, готовность, договорная стоимость, CRUD |
| Сметы | список смет + позиции выбранной сметы, CRUD сметы и позиций, плановая стоимость |
| Поступления | накладные с раскрывающимся составом, CRUD, пересчёт остатков |
| Списания | акты расхода, предупреждение о превышении остатка, CRUD |
| Анализ отклонений | фильтры по объекту и критичности, детализация план/факт, экспорт CSV |
| Сотрудники | CRUD учётных записей, роли, блокировка, сброс пароля |
| Настройки | проверка подключения к SQL Server, сведения о БД, права текущей роли |

## 9. Версии пакетов и совместимость

Все версии NuGet зафиксированы в одном файле — `Directory.Packages.props` (Central Package Management). В `.csproj` версий нет: изменить версию можно только в этом файле, поэтому «случайно обновлённый» пакет в одном проекте не рассинхронизирует решение.

| Пакет | Версия | Почему именно она |
|---|---|---|
| `Microsoft.EntityFrameworkCore` (+ `.InMemory`, `.Design`, `.Tools`, `.Relational`) | 8.0.10 | ветка EF Core 8 — единственная, совместимая с `net8.0` |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.10 | провайдер Microsoft SQL Server для EF Core 8 (тянет `Microsoft.Data.SqlClient`) |
| `Microsoft.Extensions.*` | 8.0.x | инфраструктура .NET 8 (DI, конфигурация) |
| `CommunityToolkit.Mvvm` | 8.2.2 | генераторы `[ObservableProperty]`, `[RelayCommand]` |
| `xunit` / `Microsoft.NET.Test.Sdk` | 2.9.0 / 17.11.1 | модульные тесты |

> **Не обновляйте EF Core и его провайдеры до 9.x/10.x.** Эти пакеты требуют `net9.0`/`net10.0`, и сборка падает с ошибкой
> «Пакет Microsoft.EntityFrameworkCore 10.0.14 несовместим с net8.0 (.NETCoreApp,Version=v8.0)».
> Если нужно перейти на EF Core 9/10 — сначала поднимите `TargetFramework` всех проектов (и `net8.0-windows` → `net9.0-windows`/`net10.0-windows` в `StroyControl.App`), затем обновите `Microsoft.EntityFrameworkCore.SqlServer`.

### Если ошибка про несовместимый EF Core уже появилась

1. Проверьте, что в `.csproj` **нет** атрибутов `Version` у `PackageReference` (версии живут только в `Directory.Packages.props`).
2. Удалите следы прежнего восстановления и кэш пакетов:
   ```bash
   dotnet nuget locals all --clear
   rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj
   dotnet restore
   dotnet build
   ```
3. В Visual Studio: «Сервис → Параметры → NuGet…» не должно быть источника с предустановленной версией EF, а в «Управление пакетами NuGet» для решения не должно быть обновлений EF Core выше 8.0.x. Если IDE успела вписать `Version="10.0.14"` в `.csproj` — просто удалите этот атрибут, версия возьмётся из `Directory.Packages.props`.

### Частые ошибки сборки и их причина

| Ошибка | Причина | Решение |
|---|---|---|
| `CS1061: "UnitOfMeasure" не содержит определения "ToDisplayName"` | `ToDisplayName()` — метод расширения из `StroyControl.Core.Enums`, в файле не подключено пространство имён | `using StroyControl.Core.Enums;` в начале файла (уже сделано в `EstimateItem.cs`, `MaterialUsage.cs`) |
| `CS0103: Имя "Path" не существует в текущем контексте` | зависимость от неявного `using System.IO` / конфликт с `System.Windows.Shapes.Path` | в коде используется полное имя `System.IO.Path` (и `System.IO.File`) |
| `NU1605: Обнаружено понижение версии пакета Microsoft.Extensions.DependencyInjection.Abstractions` | EF Core 8.0.10 → `Caching.Memory 8.0.1` требует `Abstractions >= 8.0.2` | версия зафиксирована `8.0.2` в `Directory.Packages.props` |
| `NU1008: Projects that use central package version management should not define the version` | в `.csproj` остался `Version` у `PackageReference` | удалить атрибут `Version` — версии задаются в `Directory.Packages.props` |
| `NU1010: PackageReference не соответствует PackageVersion` | новая ссылка в `.csproj` добавлена без версии в `Directory.Packages.props` | добавить `<PackageVersion Include="..." Version="..." />` в `Directory.Packages.props` |
| `CS0246: Не удалось найти тип или имя пространства имён "ServiceCollection"` (и подобные) | ошибка восстановления пакетов (например, `NU1010`/`NU1605`) — ссылки на пакеты не разрешились | сначала устранить ошибку `NU*`, затем `dotnet restore` |
| `FileNotFoundException: Could not load file or assembly "Microsoft.EntityFrameworkCore.Relational"` | `Design`/`Tools` подключены приватно (`PrivateAssets=all`), поэтому их зависимость `Relational` не копируется в выход приложения | публичная ссылка `<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />` в `StroyControl.Data` + версия в `Directory.Packages.props` |
| `CS1061: "string" не содержит определения "Admin"` (и других ролей) | свойство `UserRole` в `SettingsViewModel` затеняет тип-перечисление `UserRole` | в `SettingsViewModel` используется псевдоним `using RoleKind = StroyControl.Core.Enums.UserRole;` |
| `CS1537: Псевдоним using "RoleKind" ранее встречался в этом пространстве имён` | строка `using RoleKind = ...` попала в файл дважды (ручная правка + merge из `main`) | удалить дубликат: директива должна быть ровно одна |
| `NU1504: Обнаружены повторяющиеся элементы "PackageReference"` | в `.csproj` две одинаковые ссылки на пакет (ручная правка + merge) | оставить одну `<PackageReference Include="..." />`; версия при этом всё равно берётся из `Directory.Packages.props` |
| `InvalidOperationException: Привязка типа "TwoWay" не может работать с доступным только для чтения свойством "..."` (диалог «Произошла непредвиденная ошибка», приложение закрывается) | `Run.Text` и `DataGridTextColumn.Binding` в WPF по умолчанию создаются как `TwoWay`, а свойство-источник — вычисляемое (`StockDisplay`, `DeviationPercent`, `Total`, `AuthorName`, …) | во всех таких привязках явно указывать `Mode=OneWay` (`Binding="{Binding Cost, StringFormat='{}{0:N2}', Mode=OneWay}"`); `IsReadOnly="True"` у `DataGrid` **не помогает** — режим фиксируется при создании `Binding` |

## 10. Ограничения наброска и направления развития

- Отчёты строятся по текущим данным без исторических срезов и версий смет.
- Нет импорта смет из Excel и интеграции с 1С/бухгалтерией.
- Не реализованы уведомления между рабочими местами (SignalR) и журнал аудита действий пользователей.
- Экспорт реализован в CSV; печатные формы (PDF/Word) — следующий шаг.
- Для продуктивного развёртывания нужны EF-миграции вместо `EnsureCreated` и хранение секретов вне `appsettings.json`.

Подробное описание архитектуры, ER-модели и алгоритмов — в [docs/architecture.md](docs/architecture.md).
