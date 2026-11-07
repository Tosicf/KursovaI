-- =====================================================================
--  СтройКонтроль — полный сброс данных (Microsoft SQL Server)
--
--  Удаляет представления и таблицы базы StroyControl. Сама база и логин
--  stroycontrol не удаляются: после сброса достаточно запустить приложение —
--  оно создаст схему (EnsureCreated) и загрузит демо-данные заново.
--
--  ВНИМАНИЕ: все введённые вручную данные будут потеряны.
--
--  Запуск:
--    sqlcmd -S .\SQLEXPRESS -E -i database\reset.sql
--    (или открыть файл в SSMS и нажать F5)
-- =====================================================================

USE StroyControl;
GO

-- Дочерние таблицы удаляются первыми (порядок важен для внешних ключей)
DROP VIEW IF EXISTS dbo.v_material_consumption;
GO
DROP VIEW IF EXISTS dbo.v_estimate_plan;
GO

DROP TABLE IF EXISTS dbo.material_usages;
DROP TABLE IF EXISTS dbo.delivery_items;
DROP TABLE IF EXISTS dbo.deliveries;
DROP TABLE IF EXISTS dbo.estimate_items;
DROP TABLE IF EXISTS dbo.estimates;
DROP TABLE IF EXISTS dbo.materials;
DROP TABLE IF EXISTS dbo.projects;
DROP TABLE IF EXISTS dbo.suppliers;
DROP TABLE IF EXISTS dbo.users;
GO

SELECT N'База очищена. Запустите приложение — схема и демо-данные будут созданы заново.' AS [Результат];
GO
