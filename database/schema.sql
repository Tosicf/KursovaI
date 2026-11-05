-- =====================================================================
--  СтройКонтроль — схема базы данных MySQL 8
--  Сетевое средство управления строительными материалами и анализа
--  отклонений от сметы на строительном объекте.
--
--  Скрипт необязателен: приложение создаёт схему автоматически
--  (EF Core, Database.EnsureCreated) и наполняет её демо-данными.
--  Используйте скрипт, если требуется развернуть БД вручную.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS stroycontrol
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'stroycontrol'@'%' IDENTIFIED BY 'stroycontrol';
GRANT ALL PRIVILEGES ON stroycontrol.* TO 'stroycontrol'@'%';
FLUSH PRIVILEGES;

USE stroycontrol;

-- ---------------------------------------------------------------------
-- Учётные записи
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    Id            INT            NOT NULL AUTO_INCREMENT,
    RowVersion    INT            NOT NULL DEFAULT 1,
    CreatedAt     DATETIME(6)    NOT NULL,
    UpdatedAt     DATETIME(6)    NULL,
    Login         VARCHAR(64)    NOT NULL,
    DisplayName   VARCHAR(150)   NOT NULL,
    PasswordHash  VARCHAR(256)   NOT NULL,
    PasswordSalt  VARCHAR(256)   NOT NULL,
    Role          INT            NOT NULL,
    IsActive      TINYINT(1)     NOT NULL DEFAULT 1,
    LastLoginAt   DATETIME(6)    NULL,
    Email         VARCHAR(150)   NULL,
    Phone         VARCHAR(32)    NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_users_Login (Login)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Поставщики
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS suppliers (
    Id            INT            NOT NULL AUTO_INCREMENT,
    RowVersion    INT            NOT NULL DEFAULT 1,
    CreatedAt     DATETIME(6)    NOT NULL,
    UpdatedAt     DATETIME(6)    NULL,
    Name          VARCHAR(200)   NOT NULL,
    Unp           VARCHAR(16)    NULL,
    ContactPerson VARCHAR(120)   NULL,
    Phone         VARCHAR(32)    NULL,
    Email         VARCHAR(150)   NULL,
    Address       VARCHAR(250)   NULL,
    Rating        INT            NOT NULL DEFAULT 5,
    Note          VARCHAR(500)   NULL,
    PRIMARY KEY (Id),
    KEY IX_suppliers_Name (Name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Материалы
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS materials (
    Id            INT            NOT NULL AUTO_INCREMENT,
    RowVersion    INT            NOT NULL DEFAULT 1,
    CreatedAt     DATETIME(6)    NOT NULL,
    UpdatedAt     DATETIME(6)    NULL,
    Code          VARCHAR(32)    NOT NULL,
    Name          VARCHAR(200)   NOT NULL,
    Category      VARCHAR(80)    NULL,
    Unit          INT            NOT NULL,
    Price         DECIMAL(18,2)  NOT NULL,
    StockQuantity DECIMAL(18,3)  NOT NULL,
    MinStock      DECIMAL(18,3)  NOT NULL,
    IsActive      TINYINT(1)     NOT NULL DEFAULT 1,
    SupplierId    INT            NULL,
    Note          VARCHAR(500)   NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_materials_Code (Code),
    KEY IX_materials_SupplierId (SupplierId),
    CONSTRAINT FK_materials_suppliers FOREIGN KEY (SupplierId)
        REFERENCES suppliers (Id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Строительные объекты
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS projects (
    Id              INT            NOT NULL AUTO_INCREMENT,
    RowVersion      INT            NOT NULL DEFAULT 1,
    CreatedAt       DATETIME(6)    NOT NULL,
    UpdatedAt       DATETIME(6)    NULL,
    Code            VARCHAR(32)    NOT NULL,
    Name            VARCHAR(200)   NOT NULL,
    Address         VARCHAR(250)   NULL,
    Customer        VARCHAR(200)   NULL,
    ManagerName     VARCHAR(150)   NULL,
    ContractSum     DECIMAL(18,2)  NOT NULL,
    StartDate       DATETIME(6)    NOT NULL,
    PlannedEndDate  DATETIME(6)    NOT NULL,
    Status          INT            NOT NULL,
    ProgressPercent INT            NOT NULL,
    Note            VARCHAR(500)   NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_projects_Code (Code)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Сметы и позиции смет
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS estimates (
    Id         INT           NOT NULL AUTO_INCREMENT,
    RowVersion INT           NOT NULL DEFAULT 1,
    CreatedAt  DATETIME(6)   NOT NULL,
    UpdatedAt  DATETIME(6)   NULL,
    Number     VARCHAR(32)   NOT NULL,
    Name       VARCHAR(200)  NOT NULL,
    ProjectId  INT           NOT NULL,
    Status     INT           NOT NULL,
    Note       VARCHAR(500)  NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_estimates_Number (Number),
    KEY IX_estimates_ProjectId (ProjectId),
    CONSTRAINT FK_estimates_projects FOREIGN KEY (ProjectId)
        REFERENCES projects (Id) ON DELETE CASCADE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

CREATE TABLE IF NOT EXISTS estimate_items (
    Id                INT            NOT NULL AUTO_INCREMENT,
    RowVersion        INT            NOT NULL DEFAULT 1,
    CreatedAt         DATETIME(6)    NOT NULL,
    UpdatedAt         DATETIME(6)    NULL,
    EstimateId        INT            NOT NULL,
    MaterialId        INT            NOT NULL,
    Section           VARCHAR(120)   NULL,
    PlannedQuantity   DECIMAL(18,3)  NOT NULL,
    PlannedUnitPrice  DECIMAL(18,2)  NOT NULL,
    Note              VARCHAR(300)   NULL,
    PRIMARY KEY (Id),
    KEY IX_estimate_items_EstimateId (EstimateId),
    KEY IX_estimate_items_MaterialId (MaterialId),
    CONSTRAINT FK_estimate_items_estimates FOREIGN KEY (EstimateId)
        REFERENCES estimates (Id) ON DELETE CASCADE,
    CONSTRAINT FK_estimate_items_materials FOREIGN KEY (MaterialId)
        REFERENCES materials (Id) ON DELETE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Поступления (приходные накладные)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS deliveries (
    Id             INT           NOT NULL AUTO_INCREMENT,
    RowVersion     INT           NOT NULL DEFAULT 1,
    CreatedAt      DATETIME(6)   NOT NULL,
    UpdatedAt      DATETIME(6)   NULL,
    Number         VARCHAR(32)   NOT NULL,
    Date           DATETIME(6)   NOT NULL,
    SupplierId     INT           NOT NULL,
    ProjectId      INT           NULL,
    InvoiceNumber  VARCHAR(64)   NULL,
    Note           VARCHAR(500)  NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_deliveries_Number (Number),
    KEY IX_deliveries_SupplierId (SupplierId),
    KEY IX_deliveries_ProjectId (ProjectId),
    CONSTRAINT FK_deliveries_suppliers FOREIGN KEY (SupplierId)
        REFERENCES suppliers (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_deliveries_projects FOREIGN KEY (ProjectId)
        REFERENCES projects (Id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

CREATE TABLE IF NOT EXISTS delivery_items (
    Id          INT            NOT NULL AUTO_INCREMENT,
    RowVersion  INT            NOT NULL DEFAULT 1,
    CreatedAt   DATETIME(6)    NOT NULL,
    UpdatedAt   DATETIME(6)    NULL,
    DeliveryId  INT            NOT NULL,
    MaterialId  INT            NOT NULL,
    Quantity    DECIMAL(18,3)  NOT NULL,
    UnitPrice   DECIMAL(18,2)  NOT NULL,
    PRIMARY KEY (Id),
    KEY IX_delivery_items_DeliveryId (DeliveryId),
    KEY IX_delivery_items_MaterialId (MaterialId),
    CONSTRAINT FK_delivery_items_deliveries FOREIGN KEY (DeliveryId)
        REFERENCES deliveries (Id) ON DELETE CASCADE,
    CONSTRAINT FK_delivery_items_materials FOREIGN KEY (MaterialId)
        REFERENCES materials (Id) ON DELETE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Фактические списания материалов (акты расхода)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS material_usages (
    Id              INT            NOT NULL AUTO_INCREMENT,
    RowVersion      INT            NOT NULL DEFAULT 1,
    CreatedAt       DATETIME(6)    NOT NULL,
    UpdatedAt       DATETIME(6)    NULL,
    ProjectId       INT            NOT NULL,
    MaterialId      INT            NOT NULL,
    EstimateId      INT            NULL,
    Quantity        DECIMAL(18,3)  NOT NULL,
    UnitPrice       DECIMAL(18,2)  NOT NULL,
    Date            DATETIME(6)    NOT NULL,
    DocumentNumber  VARCHAR(32)    NOT NULL,
    Note            VARCHAR(500)   NULL,
    CreatedByUserId INT            NULL,
    PRIMARY KEY (Id),
    KEY IX_material_usages_ProjectId_MaterialId (ProjectId, MaterialId),
    KEY IX_material_usages_MaterialId (MaterialId),
    KEY IX_material_usages_EstimateId (EstimateId),
    KEY IX_material_usages_CreatedByUserId (CreatedByUserId),
    CONSTRAINT FK_material_usages_projects FOREIGN KEY (ProjectId)
        REFERENCES projects (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_material_usages_materials FOREIGN KEY (MaterialId)
        REFERENCES materials (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_material_usages_estimates FOREIGN KEY (EstimateId)
        REFERENCES estimates (Id) ON DELETE SET NULL,
    CONSTRAINT FK_material_usages_users FOREIGN KEY (CreatedByUserId)
        REFERENCES users (Id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- ---------------------------------------------------------------------
-- Полезные представления для отчётов (необязательно)
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW v_material_consumption AS
SELECT
    p.Id                AS ProjectId,
    p.Name              AS ProjectName,
    m.Id                AS MaterialId,
    m.Name              AS MaterialName,
    m.Unit              AS Unit,
    SUM(u.Quantity)     AS ActualQuantity,
    SUM(u.Quantity * u.UnitPrice) AS ActualCost
FROM material_usages u
         JOIN projects p ON p.Id = u.ProjectId
         JOIN materials m ON m.Id = u.MaterialId
GROUP BY p.Id, p.Name, m.Id, m.Name, m.Unit;

CREATE OR REPLACE VIEW v_estimate_plan AS
SELECT
    e.ProjectId         AS ProjectId,
    i.MaterialId        AS MaterialId,
    SUM(i.PlannedQuantity)                  AS PlannedQuantity,
    SUM(i.PlannedQuantity * i.PlannedUnitPrice) AS PlannedCost
FROM estimate_items i
         JOIN estimates e ON e.Id = i.EstimateId
GROUP BY e.ProjectId, i.MaterialId;
