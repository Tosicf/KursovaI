-- =====================================================================
--  СтройКонтроль — установка базы данных Microsoft SQL Server
--  Сетевое средство управления строительными материалами и анализа
--  отклонений от сметы на строительном объекте.
--
--  ПОДДЕРЖИВАЕМЫЕ ВЕРСИИ
--    SQL Server 2019 / 2022, включая бесплатный SQL Server Express.
--
--  КАК ЗАПУСТИТЬ
--    1) из консоли (cmd/PowerShell) — от имени администратора:
--         sqlcmd -S .\SQLEXPRESS -E -i database\schema.sql
--       где -E — вход по Windows-аутентификации;
--       для SQL-логина:  sqlcmd -S .\SQLEXPRESS -U sa -P <пароль> -i database\schema.sql
--    2) в SQL Server Management Studio (SSMS):
--       File -> Open -> File... -> database\schema.sql -> Execute (F5)
--    3) в Visual Studio: SQL Server Object Explorer -> правый клик -> New Query
--
--  ЧТО ДЕЛАЕТ СКРИПТ
--    * создаёт базу StroyControl (collation Cyrillic_General_100_CI_AS);
--    * создаёт SQL-логин stroycontrol/stroycontrol и пользователя в базе с ролью
--      db_owner — нужен рабочим местам, которые подключаются по сети (в рабочей
--      группе Windows-аутентификация по сети не работает, нужен домен);
--    * создаёт 9 таблиц и 2 представления для отчётов.
--
--  Скрипт можно запускать повторно: все объекты создаются с проверкой наличия,
--  существующие данные не удаляются. Полный сброс — database/reset.sql.
--
--  Таблицы можно и не создавать вручную: приложение при первом запуске вызывает
--  EF Core EnsureCreated и создаёт схему само (пользователю нужны права
--  db_owner или db_ddladmin). Скрипт нужен, если базу разворачивает администратор
--  или у приложения нет прав на создание объектов.
-- =====================================================================

SET NOCOUNT ON;
GO

-- ---------------------------------------------------------------------
-- 1. База данных
-- ---------------------------------------------------------------------
USE master;
GO

IF DB_ID(N'StroyControl') IS NULL
BEGIN
    CREATE DATABASE StroyControl COLLATE Cyrillic_General_100_CI_AS;
END;
GO

-- ---------------------------------------------------------------------
-- 2. SQL-логин для сетевых клиентов
--    Если сервер работает только в режиме Windows-аутентификации, логин
--    создастся, но подключиться под ним будет нельзя: включите смешанный
--    режим (SSMS -> Server Properties -> Security -> SQL Server and Windows
--    Authentication mode) и перезапустите службу.
--    Пароль здесь и в appsettings.json должны совпадать.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'stroycontrol')
BEGIN
    CREATE LOGIN stroycontrol
        WITH PASSWORD = N'stroycontrol',
             CHECK_POLICY = OFF,
             CHECK_EXPIRATION = OFF,
             DEFAULT_DATABASE = StroyControl;
END;
GO

USE StroyControl;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'stroycontrol')
BEGIN
    CREATE USER stroycontrol FOR LOGIN stroycontrol;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
             JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
             JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_owner' AND m.name = N'stroycontrol')
BEGIN
    ALTER ROLE db_owner ADD MEMBER stroycontrol;
END;
GO

-- ---------------------------------------------------------------------
-- 3. Учётные записи сотрудников
--    Пароль не хранится: только PBKDF2-хеш и индивидуальная соль (Base64).
--    Role: 0 — Admin, 1 — Engineer, 2 — SupplyManager, 3 — Accountant, 4 — Foreman.
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.users', N'U') IS NULL
CREATE TABLE dbo.users (
    Id            INT            IDENTITY (1,1) NOT NULL,
    RowVersion    INT            NOT NULL CONSTRAINT DF_users_RowVersion DEFAULT 1,
    CreatedAt     DATETIME2(6)   NOT NULL,
    UpdatedAt     DATETIME2(6)   NULL,
    Login         NVARCHAR(64)   NOT NULL,
    DisplayName   NVARCHAR(150)  NOT NULL,
    PasswordHash  NVARCHAR(256)  NOT NULL,
    PasswordSalt  NVARCHAR(256)  NOT NULL,
    Role          INT            NOT NULL,
    IsActive      BIT            NOT NULL CONSTRAINT DF_users_IsActive DEFAULT 1,
    LastLoginAt   DATETIME2(6)   NULL,
    Email         NVARCHAR(150)  NULL,
    Phone         NVARCHAR(32)   NULL,
    CONSTRAINT PK_users PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_users_Login' AND object_id = OBJECT_ID(N'dbo.users'))
    CREATE UNIQUE INDEX IX_users_Login ON dbo.users (Login);
GO

-- ---------------------------------------------------------------------
-- 4. Поставщики
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.suppliers', N'U') IS NULL
CREATE TABLE dbo.suppliers (
    Id            INT            IDENTITY (1,1) NOT NULL,
    RowVersion    INT            NOT NULL CONSTRAINT DF_suppliers_RowVersion DEFAULT 1,
    CreatedAt     DATETIME2(6)   NOT NULL,
    UpdatedAt     DATETIME2(6)   NULL,
    Name          NVARCHAR(200)  NOT NULL,
    Unp           NVARCHAR(16)   NULL,
    ContactPerson NVARCHAR(120)  NULL,
    Phone         NVARCHAR(32)   NULL,
    Email         NVARCHAR(150)  NULL,
    [Address]     NVARCHAR(250)  NULL,
    Rating        INT            NOT NULL CONSTRAINT DF_suppliers_Rating DEFAULT 5,
    Note          NVARCHAR(500)  NULL,
    CONSTRAINT PK_suppliers PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_suppliers_Name' AND object_id = OBJECT_ID(N'dbo.suppliers'))
    CREATE INDEX IX_suppliers_Name ON dbo.suppliers (Name);
GO

-- ---------------------------------------------------------------------
-- 5. Материалы (номенклатура и текущий остаток)
--    Unit: 0 — шт, 1 — м, 2 — м2, 3 — м3, 4 — кг, 5 — т, 6 — л, 7 — уп, 8 — меш.
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.materials', N'U') IS NULL
CREATE TABLE dbo.materials (
    Id            INT            IDENTITY (1,1) NOT NULL,
    RowVersion    INT            NOT NULL CONSTRAINT DF_materials_RowVersion DEFAULT 1,
    CreatedAt     DATETIME2(6)   NOT NULL,
    UpdatedAt     DATETIME2(6)   NULL,
    Code          NVARCHAR(32)   NOT NULL,
    Name          NVARCHAR(200)  NOT NULL,
    Category      NVARCHAR(80)   NULL,
    Unit          INT            NOT NULL,
    Price         DECIMAL(18,2)  NOT NULL,
    StockQuantity DECIMAL(18,3)  NOT NULL,
    MinStock      DECIMAL(18,3)  NOT NULL,
    IsActive      BIT            NOT NULL CONSTRAINT DF_materials_IsActive DEFAULT 1,
    SupplierId    INT            NULL,
    Note          NVARCHAR(500)  NULL,
    CONSTRAINT PK_materials PRIMARY KEY (Id),
    CONSTRAINT FK_materials_suppliers FOREIGN KEY (SupplierId)
        REFERENCES dbo.suppliers (Id) ON DELETE SET NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_materials_Code' AND object_id = OBJECT_ID(N'dbo.materials'))
    CREATE UNIQUE INDEX IX_materials_Code ON dbo.materials (Code);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_materials_SupplierId' AND object_id = OBJECT_ID(N'dbo.materials'))
    CREATE INDEX IX_materials_SupplierId ON dbo.materials (SupplierId);
GO

-- ---------------------------------------------------------------------
-- 6. Строительные объекты
--    Status: 0 — Planned, 1 — InProgress, 2 — Suspended, 3 — Completed.
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.projects', N'U') IS NULL
CREATE TABLE dbo.projects (
    Id              INT            IDENTITY (1,1) NOT NULL,
    RowVersion      INT            NOT NULL CONSTRAINT DF_projects_RowVersion DEFAULT 1,
    CreatedAt       DATETIME2(6)   NOT NULL,
    UpdatedAt       DATETIME2(6)   NULL,
    Code            NVARCHAR(32)   NOT NULL,
    Name            NVARCHAR(200)  NOT NULL,
    [Address]       NVARCHAR(250)  NULL,
    Customer        NVARCHAR(200)  NULL,
    ManagerName     NVARCHAR(150)  NULL,
    ContractSum     DECIMAL(18,2)  NOT NULL,
    StartDate       DATETIME2(6)   NOT NULL,
    PlannedEndDate  DATETIME2(6)   NOT NULL,
    Status          INT            NOT NULL,
    ProgressPercent INT            NOT NULL,
    Note            NVARCHAR(500)  NULL,
    CONSTRAINT PK_projects PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_projects_Code' AND object_id = OBJECT_ID(N'dbo.projects'))
    CREATE UNIQUE INDEX IX_projects_Code ON dbo.projects (Code);
GO

-- ---------------------------------------------------------------------
-- 7. Сметы и позиции смет (план)
--    Status сметы: 0 — Draft, 1 — Approved, 2 — Archived.
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.estimates', N'U') IS NULL
CREATE TABLE dbo.estimates (
    Id         INT            IDENTITY (1,1) NOT NULL,
    RowVersion INT            NOT NULL CONSTRAINT DF_estimates_RowVersion DEFAULT 1,
    CreatedAt  DATETIME2(6)   NOT NULL,
    UpdatedAt  DATETIME2(6)   NULL,
    Number     NVARCHAR(32)   NOT NULL,
    Name       NVARCHAR(200)  NOT NULL,
    ProjectId  INT            NOT NULL,
    Status     INT            NOT NULL,
    Note       NVARCHAR(500)  NULL,
    CONSTRAINT PK_estimates PRIMARY KEY (Id),
    CONSTRAINT FK_estimates_projects FOREIGN KEY (ProjectId)
        REFERENCES dbo.projects (Id) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_estimates_Number' AND object_id = OBJECT_ID(N'dbo.estimates'))
    CREATE UNIQUE INDEX IX_estimates_Number ON dbo.estimates (Number);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_estimates_ProjectId' AND object_id = OBJECT_ID(N'dbo.estimates'))
    CREATE INDEX IX_estimates_ProjectId ON dbo.estimates (ProjectId);
GO

IF OBJECT_ID(N'dbo.estimate_items', N'U') IS NULL
CREATE TABLE dbo.estimate_items (
    Id                INT            IDENTITY (1,1) NOT NULL,
    RowVersion        INT            NOT NULL CONSTRAINT DF_estimate_items_RowVersion DEFAULT 1,
    CreatedAt         DATETIME2(6)   NOT NULL,
    UpdatedAt         DATETIME2(6)   NULL,
    EstimateId        INT            NOT NULL,
    MaterialId        INT            NOT NULL,
    Section           NVARCHAR(120)  NULL,
    PlannedQuantity   DECIMAL(18,3)  NOT NULL,
    PlannedUnitPrice  DECIMAL(18,2)  NOT NULL,
    Note              NVARCHAR(300)  NULL,
    CONSTRAINT PK_estimate_items PRIMARY KEY (Id),
    CONSTRAINT FK_estimate_items_estimates FOREIGN KEY (EstimateId)
        REFERENCES dbo.estimates (Id) ON DELETE CASCADE,
    CONSTRAINT FK_estimate_items_materials FOREIGN KEY (MaterialId)
        REFERENCES dbo.materials (Id) ON DELETE NO ACTION
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_estimate_items_EstimateId' AND object_id = OBJECT_ID(N'dbo.estimate_items'))
    CREATE INDEX IX_estimate_items_EstimateId ON dbo.estimate_items (EstimateId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_estimate_items_MaterialId' AND object_id = OBJECT_ID(N'dbo.estimate_items'))
    CREATE INDEX IX_estimate_items_MaterialId ON dbo.estimate_items (MaterialId);
GO

-- ---------------------------------------------------------------------
-- 8. Поступления (приходные накладные) и их строки
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.deliveries', N'U') IS NULL
CREATE TABLE dbo.deliveries (
    Id             INT            IDENTITY (1,1) NOT NULL,
    RowVersion     INT            NOT NULL CONSTRAINT DF_deliveries_RowVersion DEFAULT 1,
    CreatedAt      DATETIME2(6)   NOT NULL,
    UpdatedAt      DATETIME2(6)   NULL,
    Number         NVARCHAR(32)   NOT NULL,
    [Date]         DATETIME2(6)   NOT NULL,
    SupplierId     INT            NOT NULL,
    ProjectId      INT            NULL,
    InvoiceNumber  NVARCHAR(64)   NULL,
    Note           NVARCHAR(500)  NULL,
    CONSTRAINT PK_deliveries PRIMARY KEY (Id),
    CONSTRAINT FK_deliveries_suppliers FOREIGN KEY (SupplierId)
        REFERENCES dbo.suppliers (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_deliveries_projects FOREIGN KEY (ProjectId)
        REFERENCES dbo.projects (Id) ON DELETE SET NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_deliveries_Number' AND object_id = OBJECT_ID(N'dbo.deliveries'))
    CREATE UNIQUE INDEX IX_deliveries_Number ON dbo.deliveries (Number);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_deliveries_SupplierId' AND object_id = OBJECT_ID(N'dbo.deliveries'))
    CREATE INDEX IX_deliveries_SupplierId ON dbo.deliveries (SupplierId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_deliveries_ProjectId' AND object_id = OBJECT_ID(N'dbo.deliveries'))
    CREATE INDEX IX_deliveries_ProjectId ON dbo.deliveries (ProjectId);
GO

IF OBJECT_ID(N'dbo.delivery_items', N'U') IS NULL
CREATE TABLE dbo.delivery_items (
    Id          INT            IDENTITY (1,1) NOT NULL,
    RowVersion  INT            NOT NULL CONSTRAINT DF_delivery_items_RowVersion DEFAULT 1,
    CreatedAt   DATETIME2(6)   NOT NULL,
    UpdatedAt   DATETIME2(6)   NULL,
    DeliveryId  INT            NOT NULL,
    MaterialId  INT            NOT NULL,
    Quantity    DECIMAL(18,3)  NOT NULL,
    UnitPrice   DECIMAL(18,2)  NOT NULL,
    CONSTRAINT PK_delivery_items PRIMARY KEY (Id),
    CONSTRAINT FK_delivery_items_deliveries FOREIGN KEY (DeliveryId)
        REFERENCES dbo.deliveries (Id) ON DELETE CASCADE,
    CONSTRAINT FK_delivery_items_materials FOREIGN KEY (MaterialId)
        REFERENCES dbo.materials (Id) ON DELETE NO ACTION
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_delivery_items_DeliveryId' AND object_id = OBJECT_ID(N'dbo.delivery_items'))
    CREATE INDEX IX_delivery_items_DeliveryId ON dbo.delivery_items (DeliveryId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_delivery_items_MaterialId' AND object_id = OBJECT_ID(N'dbo.delivery_items'))
    CREATE INDEX IX_delivery_items_MaterialId ON dbo.delivery_items (MaterialId);
GO

-- ---------------------------------------------------------------------
-- 9. Фактические списания материалов (акты расхода)
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.material_usages', N'U') IS NULL
CREATE TABLE dbo.material_usages (
    Id              INT            IDENTITY (1,1) NOT NULL,
    RowVersion      INT            NOT NULL CONSTRAINT DF_material_usages_RowVersion DEFAULT 1,
    CreatedAt       DATETIME2(6)   NOT NULL,
    UpdatedAt       DATETIME2(6)   NULL,
    ProjectId       INT            NOT NULL,
    MaterialId      INT            NOT NULL,
    EstimateId      INT            NULL,
    Quantity        DECIMAL(18,3)  NOT NULL,
    UnitPrice       DECIMAL(18,2)  NOT NULL,
    [Date]          DATETIME2(6)   NOT NULL,
    DocumentNumber  NVARCHAR(32)   NOT NULL,
    Note            NVARCHAR(500)  NULL,
    CreatedByUserId INT            NULL,
    CONSTRAINT PK_material_usages PRIMARY KEY (Id),
    CONSTRAINT FK_material_usages_projects FOREIGN KEY (ProjectId)
        REFERENCES dbo.projects (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_material_usages_materials FOREIGN KEY (MaterialId)
        REFERENCES dbo.materials (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_material_usages_estimates FOREIGN KEY (EstimateId)
        REFERENCES dbo.estimates (Id) ON DELETE SET NULL,
    CONSTRAINT FK_material_usages_users FOREIGN KEY (CreatedByUserId)
        REFERENCES dbo.users (Id) ON DELETE SET NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_material_usages_ProjectId_MaterialId' AND object_id = OBJECT_ID(N'dbo.material_usages'))
    CREATE INDEX IX_material_usages_ProjectId_MaterialId ON dbo.material_usages (ProjectId, MaterialId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_material_usages_MaterialId' AND object_id = OBJECT_ID(N'dbo.material_usages'))
    CREATE INDEX IX_material_usages_MaterialId ON dbo.material_usages (MaterialId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_material_usages_EstimateId' AND object_id = OBJECT_ID(N'dbo.material_usages'))
    CREATE INDEX IX_material_usages_EstimateId ON dbo.material_usages (EstimateId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_material_usages_CreatedByUserId' AND object_id = OBJECT_ID(N'dbo.material_usages'))
    CREATE INDEX IX_material_usages_CreatedByUserId ON dbo.material_usages (CreatedByUserId);
GO

-- ---------------------------------------------------------------------
-- 10. Представления для отчётов (те же формулы, что считает приложение)
--     В T-SQL каждое представление должно быть первым оператором пакета,
--     поэтому перед каждым CREATE VIEW стоит GO.
-- ---------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.v_material_consumption AS
SELECT
    p.Id                          AS ProjectId,
    p.Name                        AS ProjectName,
    m.Id                          AS MaterialId,
    m.Name                        AS MaterialName,
    m.Unit                        AS Unit,
    SUM(u.Quantity)               AS ActualQuantity,
    SUM(u.Quantity * u.UnitPrice) AS ActualCost
FROM dbo.material_usages u
         JOIN dbo.projects p ON p.Id = u.ProjectId
         JOIN dbo.materials m ON m.Id = u.MaterialId
GROUP BY p.Id, p.Name, m.Id, m.Name, m.Unit;
GO

CREATE OR ALTER VIEW dbo.v_estimate_plan AS
SELECT
    e.ProjectId                                 AS ProjectId,
    i.MaterialId                                AS MaterialId,
    SUM(i.PlannedQuantity)                      AS PlannedQuantity,
    SUM(i.PlannedQuantity * i.PlannedUnitPrice) AS PlannedCost
FROM dbo.estimate_items i
         JOIN dbo.estimates e ON e.Id = i.EstimateId
GROUP BY e.ProjectId, i.MaterialId;
GO

-- ---------------------------------------------------------------------
-- 11. Проверка результата
-- ---------------------------------------------------------------------
SELECT TABLE_SCHEMA AS [Схема], TABLE_NAME AS [Таблица], TABLE_TYPE AS [Тип]
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_CATALOG = N'StroyControl'
ORDER BY TABLE_TYPE, TABLE_NAME;
GO

SELECT N'Готово: база StroyControl создана, логин stroycontrol/stroycontrol настроен.' AS [Результат];
GO
