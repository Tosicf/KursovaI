using Microsoft.EntityFrameworkCore;

namespace StroyControl.Data.Seed;

/// <summary>
/// Догоняющая миграция схемы для уже существующих баз: EF Core создаёт схему через
/// EnsureCreated, который не изменяет готовую базу. Поэтому при запуске приложения
/// недостающие колонки добавляются идемпотентными командами T-SQL.
/// </summary>
public static class SchemaPatcher
{
    private static readonly string[] Statements =
    [
        // Позиции сметы: работы и материалы в одной таблице.
        @"IF COL_LENGTH('dbo.estimate_items', 'IsWork') IS NULL
              ALTER TABLE dbo.estimate_items ADD IsWork BIT NOT NULL CONSTRAINT DF_estimate_items_IsWork DEFAULT 0;",

        @"IF COL_LENGTH('dbo.estimate_items', 'WorkName') IS NULL
              ALTER TABLE dbo.estimate_items ADD WorkName NVARCHAR(200) NULL;",

        @"IF COL_LENGTH('dbo.estimate_items', 'WorkUnit') IS NULL
              ALTER TABLE dbo.estimate_items ADD WorkUnit NVARCHAR(16) NOT NULL CONSTRAINT DF_estimate_items_WorkUnit DEFAULT N'';",

        // Работа может не ссылаться на материал справочника.
        @"IF EXISTS (SELECT 1 FROM sys.columns
                     WHERE object_id = OBJECT_ID(N'dbo.estimate_items') AND name = N'MaterialId' AND is_nullable = 0)
              ALTER TABLE dbo.estimate_items ALTER COLUMN MaterialId INT NULL;",

        // Коэффициенты отчёта по смете.
        @"IF COL_LENGTH('dbo.estimates', 'MarkupPercent') IS NULL
              ALTER TABLE dbo.estimates ADD MarkupPercent DECIMAL(6,2) NOT NULL CONSTRAINT DF_estimates_MarkupPercent DEFAULT 10;",

        @"IF COL_LENGTH('dbo.estimates', 'ProfitPercent') IS NULL
              ALTER TABLE dbo.estimates ADD ProfitPercent DECIMAL(6,2) NOT NULL CONSTRAINT DF_estimates_ProfitPercent DEFAULT 12;",

        @"IF COL_LENGTH('dbo.estimates', 'LaborSharePercent') IS NULL
              ALTER TABLE dbo.estimates ADD LaborSharePercent DECIMAL(6,2) NOT NULL CONSTRAINT DF_estimates_LaborSharePercent DEFAULT 35;",

        // Дата сметы: по ней сметы группируются и сортируются по месяцам во всех списках.
        @"IF COL_LENGTH('dbo.estimates', 'Date') IS NULL
              ALTER TABLE dbo.estimates ADD [Date] DATETIME2 NOT NULL CONSTRAINT DF_estimates_Date DEFAULT SYSUTCDATETIME();",

        @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_estimates_Date' AND object_id = OBJECT_ID(N'dbo.estimates'))
              CREATE INDEX IX_estimates_Date ON dbo.estimates ([Date]);",

        // Площадь объекта — для стоимости и прибыли на 1 м².
        @"IF COL_LENGTH('dbo.projects', 'TotalArea') IS NULL
              ALTER TABLE dbo.projects ADD TotalArea DECIMAL(12,2) NOT NULL CONSTRAINT DF_projects_TotalArea DEFAULT 0;",

        // Факт по работам: списание материала теперь знает, к какой позиции сметы
        // и к какому акту выполненных работ оно относится.
        @"IF COL_LENGTH('dbo.material_usages', 'EstimateItemId') IS NULL
              ALTER TABLE dbo.material_usages ADD EstimateItemId INT NULL;",

        @"IF COL_LENGTH('dbo.material_usages', 'WorkCompletionId') IS NULL
              ALTER TABLE dbo.material_usages ADD WorkCompletionId INT NULL;",

        // Политика NO ACTION обязательна: у material_usages уже есть каскадная цепочка
        // от сметы (EstimateId → SET NULL), а смета каскадно связана с позициями и
        // актами, поэтому SET NULL на второй связи даёт ошибку SQL Server 1785
        // «may cause cycles or multiple cascade paths». Связанные записи удаляют сервисы.
        // Если связь успела создаться с каскадом (delete_referential_action <> 0),
        // она пересоздаётся: иначе SQL Server снова ответит ошибкой 1785.
        @"IF EXISTS (SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_material_usages_estimate_items_EstimateItemId'
                      AND parent_object_id = OBJECT_ID(N'dbo.material_usages')
                      AND delete_referential_action <> 0)
              ALTER TABLE dbo.material_usages DROP CONSTRAINT FK_material_usages_estimate_items_EstimateItemId;",

        @"IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                        WHERE name = N'FK_material_usages_estimate_items_EstimateItemId'
                          AND parent_object_id = OBJECT_ID(N'dbo.material_usages'))
              ALTER TABLE dbo.material_usages WITH CHECK
                  ADD CONSTRAINT FK_material_usages_estimate_items_EstimateItemId
                  FOREIGN KEY (EstimateItemId) REFERENCES dbo.estimate_items (Id) ON DELETE NO ACTION;",

        // Акт выполненных работ по смете.
        @"IF OBJECT_ID(N'dbo.work_completions', N'U') IS NULL
              CREATE TABLE dbo.work_completions (
                  Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_work_completions PRIMARY KEY,
                  EstimateId INT NOT NULL,
                  ProjectId INT NOT NULL,
                  [Date] DATETIME2 NOT NULL,
                  DocumentNumber NVARCHAR(32) NOT NULL,
                  Note NVARCHAR(500) NULL,
                  CreatedByUserId INT NULL,
                  RowVersion INT NOT NULL CONSTRAINT DF_work_completions_RowVersion DEFAULT 0,
                  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_work_completions_CreatedAt DEFAULT SYSUTCDATETIME(),
                  UpdatedAt DATETIME2 NULL,
                  CONSTRAINT FK_work_completions_estimates_EstimateId
                      FOREIGN KEY (EstimateId) REFERENCES dbo.estimates (Id) ON DELETE NO ACTION,
                  CONSTRAINT FK_work_completions_projects_ProjectId
                      FOREIGN KEY (ProjectId) REFERENCES dbo.projects (Id),
                  CONSTRAINT FK_work_completions_users_CreatedByUserId
                      FOREIGN KEY (CreatedByUserId) REFERENCES dbo.users (Id) ON DELETE SET NULL);",

        @"IF NOT EXISTS (SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_work_completions_DocumentNumber'
                          AND object_id = OBJECT_ID(N'dbo.work_completions'))
              CREATE UNIQUE INDEX IX_work_completions_DocumentNumber ON dbo.work_completions (DocumentNumber);",

        @"IF NOT EXISTS (SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_work_completions_Date'
                          AND object_id = OBJECT_ID(N'dbo.work_completions'))
              CREATE INDEX IX_work_completions_Date ON dbo.work_completions ([Date]);",

        // Строки выполненных работ.
        @"IF OBJECT_ID(N'dbo.work_completion_items', N'U') IS NULL
              CREATE TABLE dbo.work_completion_items (
                  Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_work_completion_items PRIMARY KEY,
                  WorkCompletionId INT NOT NULL,
                  EstimateItemId INT NOT NULL,
                  Quantity DECIMAL(18,3) NOT NULL,
                  UnitPrice DECIMAL(18,2) NOT NULL,
                  RowVersion INT NOT NULL CONSTRAINT DF_work_completion_items_RowVersion DEFAULT 0,
                  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_work_completion_items_CreatedAt DEFAULT SYSUTCDATETIME(),
                  UpdatedAt DATETIME2 NULL,
                  CONSTRAINT FK_work_completion_items_work_completions_WorkCompletionId
                      FOREIGN KEY (WorkCompletionId) REFERENCES dbo.work_completions (Id) ON DELETE CASCADE,
                  CONSTRAINT FK_work_completion_items_estimate_items_EstimateItemId
                      FOREIGN KEY (EstimateItemId) REFERENCES dbo.estimate_items (Id));",

        // Внешний ключ списаний на акт добавляем после создания таблицы актов.
        // Как и ссылка на позицию сметы, он объявлен без каскада: иначе к material_usages
        // ведут две цепочки (смета → списания и смета → акты → списания).
        @"IF EXISTS (SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_material_usages_work_completions_WorkCompletionId'
                      AND parent_object_id = OBJECT_ID(N'dbo.material_usages')
                      AND delete_referential_action <> 0)
              ALTER TABLE dbo.material_usages DROP CONSTRAINT FK_material_usages_work_completions_WorkCompletionId;",

        @"IF COL_LENGTH('dbo.material_usages', 'WorkCompletionId') IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                              WHERE name = N'FK_material_usages_work_completions_WorkCompletionId'
                                AND parent_object_id = OBJECT_ID(N'dbo.material_usages'))
              ALTER TABLE dbo.material_usages WITH CHECK
                  ADD CONSTRAINT FK_material_usages_work_completions_WorkCompletionId
                  FOREIGN KEY (WorkCompletionId) REFERENCES dbo.work_completions (Id) ON DELETE NO ACTION;"
    ];

    /// <summary>Применяет недостающие изменения схемы. Для InMemory (демо-режим) ничего не делает.</summary>
    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational() || !db.Database.IsSqlServer()) return;

        foreach (var statement in Statements)
            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
    }
}
