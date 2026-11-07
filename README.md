# СтройКонтроль — набросок WPF-приложения на C#

**Тема курсовой работы:** «Сетевое программное средство управления строительными материалами и анализа отклонений от сметы на строительном объекте».

Клиент-серверное приложение для строительной организации: справочник материалов и поставщиков, сметы по объектам, приходные накладные, акты списания и автоматический анализ отклонений «план — факт» по количеству, цене и стоимости. Все данные хранятся в общей базе **MySQL**, к которой одновременно подключаются несколько рабочих мест по сети.

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
| Данные | EF Core 8, Pomelo.EntityFrameworkCore.MySql, MySQL 8, `IDbContextFactory` |
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
│   ├── StroyControl.Data/        # доступ к MySQL
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
│       └── appsettings.json      # строка подключения к MySQL
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
- MySQL Server 8.0 (локально или в сети)

### 1) Подключить базу данных MySQL (один раз)

Подробная пошаговая инструкция — ниже, в разделе **5.1**. Кратко: установить MySQL 8, запустить службу, выполнить `database/schema.sql` под `root`, прописать строку подключения в `appsettings.json`.

Проверка, что всё получилось:
```bash
mysql -u stroycontrol -pstroycontrol -e "SHOW TABLES FROM stroycontrol;"
```
Должно вывестись 9 таблиц: `deliveries`, `delivery_items`, `estimate_items`, `estimates`, `material_usages`, `materials`, `projects`, `suppliers`, `users`.

### 1.1) Скрипты базы данных

| Файл | Что делает | Когда нужен |
|---|---|---|
| `database/schema.sql` | создаёт базу `stroycontrol`, пользователя `stroycontrol/stroycontrol`, 9 таблиц и 2 представления | при первом развёртывании и после переустановки MySQL |
| `database/reset.sql` | удаляет все таблицы и представления (данные теряются, база и пользователь остаются) | когда нужно «начать с нуля»: после сброса приложение снова создаст схему и демо-данные |

Оба скрипта идемпотентны (повторный запуск не ломает базу) и содержат в конце проверочный `SELECT` со списком таблиц.

Строка подключения задаётся в `src/StroyControl.App/appsettings.json`:
```json
{
  "Database": {
    "Provider": "MySql",
    "ServerVersion": "8.0.36",
    "ConnectionString": "Server=localhost;Port=3306;Database=stroycontrol;User=stroycontrol;Password=stroycontrol;CharSet=utf8mb4;AllowUserVariables=True;"
  }
}
```
Для работы нескольких рабочих мест достаточно указать в строке подключения IP сервера MySQL (например, `Server=192.168.1.10;Port=3306;...`) — клиентские приложения при этом используют одну и ту же базу.

Альтернатива правке файла — переменная окружения `STROYCONTROL_CONNECTION`: она имеет приоритет над `appsettings.json` и удобна, когда приложение запускают на чужом компьютере или в лаборатории.
```powershell
$env:STROYCONTROL_CONNECTION = "Server=localhost;Port=3306;Database=stroycontrol;User=stroycontrol;Password=stroycontrol;CharSet=utf8mb4;AllowUserVariables=True;"
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

> Если сервер MySQL недоступен, приложение предложит **демонстрационный режим** (EF Core InMemory): интерфейс полностью работоспособен, но данные не сохраняются после закрытия. Отключается флагом `Application:DemoModeAllowed: false`.

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

### 5.1 Подключение MySQL шаг за шагом (Windows)

**Шаг 1. Установить MySQL Server 8.0.** Подойдёт MySQL Installer (Developer Default) или winget:
```powershell
winget install Oracle.MySQL
```
При установке запомните пароль пользователя `root`. Порт оставьте `3306`.

**Шаг 2. Проверить, что служба запущена.** В PowerShell от имени администратора:
```powershell
Get-Service -Name "MySQL*"          # Status должен быть Running
net start MySQL80                   # если остановлена (имя службы может быть MySQL80 или MySQL)
```
Если `net start` отвечает «Служба не найдена», посмотрите точное имя: `Get-Service | Where-Object Name -like "MySQL*"`.

**Шаг 3. Выполнить скрипт создания базы** из корня репозитория:
```powershell
cmd /c "mysql -u root -p < database\schema.sql"
```
(в PowerShell символ `<` не поддерживается, поэтому вызов идёт через `cmd /c`; в обычном `cmd.exe` достаточно `mysql -u root -p < database\schema.sql`).
В MySQL Workbench: `File → Open SQL Script…` → `database/schema.sql` → кнопка Execute (молния ⚡).

В конце скрипт печатает список созданных объектов — должно быть 9 таблиц (`BASE TABLE`) и 2 представления (`VIEW`).

**Шаг 4. Проверить вход пользователем приложения:**
```powershell
mysql -u stroycontrol -pstroycontrol -e "SHOW TABLES FROM stroycontrol; SELECT COUNT(*) AS users FROM stroycontrol.users;"
```
Если команда `mysql` не находится — добавьте в `PATH` каталог `C:\Program Files\MySQL\MySQL Server 8.0\bin` (или запускайте `mysql.exe` по полному пути).

**Шаг 5. Прописать строку подключения** в `src/StroyControl.App/appsettings.json` (по умолчанию она уже совпадает со скриптом):
```
Server=localhost;Port=3306;Database=stroycontrol;User=stroycontrol;Password=stroycontrol;CharSet=utf8mb4;AllowUserVariables=True;
```
Отдельные поля: `Server` — адрес сервера (`localhost` или IP по сети), `Port` — 3306, `Database` — `stroycontrol`, `User`/`Password` — учётка из скрипта. Для другого пароля поменяйте его и в скрипте (`CREATE USER`), и здесь.

**Шаг 6. Запустить приложение:**
```powershell
dotnet run --project src/StroyControl.App
```
Первый запуск сам создаст (при необходимости) и наполнит базу демо-данными: 5 сотрудников, поставщики, ~20 материалов, 3 объекта, сметы, поступления и списания. Повторно демо-данные не дублируются — признак «база уже наполнена» — наличие записей в `users`.

**Шаг 7. Убедиться, что режим не демонстрационный.** На странице «Настройки» внизу отображается источник данных: должно быть **`MySQL 8 (сетевая БД)`** и «Подключение активно», а не «Демо-режим (данные в памяти)».

#### Если что-то не подключается

| Симптом в приложении | Причина | Что делать |
|---|---|---|
| `Access denied for user 'stroycontrol'@'localhost'` | пользователь не создан или другой пароль | выполнить `database/schema.sql` под `root`; при смене пароля — обновить строку подключения |
| `Unknown database 'stroycontrol'` | скрипт запускался не под `root` и база не создалась | запустить `schema.sql` под `root` (`mysql -u root -p`) |
| `Unable to connect to any of the specified MySQL hosts` / таймаут | служба MySQL остановлена или неверный порт | `net start MySQL80`, проверить `Port=3306` |
| Приложение предлагает демо-режим | сервер недоступен, но `DemoModeAllowed = true` | устранить причину выше либо согласиться на демо-режим (данные не сохранятся) |
| `Table 'stroycontrol.users' doesn't exist` | схема не создана, а пользователь без прав на создание | выполнить `database/schema.sql` |
| Данные «не сохраняются» после перезапуска | работа идёт в демо-режиме (InMemory) | см. строку выше; в «Настройках» источник должен быть MySQL |

Полный сброс: `cmd /c "mysql -u root -p < database\reset.sql"`, затем снова запустить приложение.

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
| Настройки | проверка подключения к MySQL, сведения о БД, права текущей роли |

## 9. Версии пакетов и совместимость

Все версии NuGet зафиксированы в одном файле — `Directory.Packages.props` (Central Package Management). В `.csproj` версий нет: изменить версию можно только в этом файле, поэтому «случайно обновлённый» пакет в одном проекте не рассинхронизирует решение.

| Пакет | Версия | Почему именно она |
|---|---|---|
| `Microsoft.EntityFrameworkCore` (+ `.InMemory`, `.Design`, `.Tools`, `.Relational`) | 8.0.10 | ветка EF Core 8 — единственная, совместимая с `net8.0` |
| `Pomelo.EntityFrameworkCore.MySql` | 8.0.2 | провайдер MySQL для EF Core 8 (с EF 9/10 несовместим) |
| `Microsoft.Extensions.*` | 8.0.x | инфраструктура .NET 8 (DI, конфигурация) |
| `CommunityToolkit.Mvvm` | 8.2.2 | генераторы `[ObservableProperty]`, `[RelayCommand]` |
| `xunit` / `Microsoft.NET.Test.Sdk` | 2.9.0 / 17.11.1 | модульные тесты |

> **Не обновляйте EF Core до 9.x/10.x и Pomelo до 9.x/10.x.** Эти пакеты требуют `net9.0`/`net10.0`, и сборка падает с ошибкой
> «Пакет Microsoft.EntityFrameworkCore 10.0.14 несовместим с net8.0 (.NETCoreApp,Version=v8.0)».
> Если нужно перейти на EF Core 9/10 — сначала поднимите `TargetFramework` всех проектов (и `net8.0-windows` → `net9.0-windows`/`net10.0-windows` в `StroyControl.App`), затем обновите Pomelo.

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
