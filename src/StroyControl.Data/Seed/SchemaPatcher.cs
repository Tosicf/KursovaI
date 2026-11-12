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
    {
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

        // Площадь объекта — для стоимости и прибыли на 1 м².
        @"IF COL_LENGTH('dbo.projects', 'TotalArea') IS NULL
              ALTER TABLE dbo.projects ADD TotalArea DECIMAL(12,2) NOT NULL CONSTRAINT DF_projects_TotalArea DEFAULT 0;"
    };

    /// <summary>Применяет недостающие изменения схемы. Для InMemory (демо-режим) ничего не делает.</summary>
    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational() || !db.Database.IsSqlServer()) return;

        foreach (var statement in Statements)
            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
    }
}
