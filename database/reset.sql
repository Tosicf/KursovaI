-- =====================================================================
--  СтройКонтроль — полный сброс данных
--
--  Удаляет все таблицы и представления базы stroycontrol. Сама база и
--  пользователь stroycontrol не удаляются, поэтому после сброса достаточно
--  запустить приложение: оно создаст схему и загрузит демо-данные заново.
--
--  ВНИМАНИЕ: все введённые вручную данные будут потеряны.
--
--  Запуск:
--    mysql -u root -p < database/reset.sql
--    (PowerShell: cmd /c "mysql -u root -p < database\reset.sql")
-- =====================================================================

USE stroycontrol;

SET FOREIGN_KEY_CHECKS = 0;

DROP VIEW IF EXISTS v_material_consumption;
DROP VIEW IF EXISTS v_estimate_plan;

DROP TABLE IF EXISTS material_usages;
DROP TABLE IF EXISTS delivery_items;
DROP TABLE IF EXISTS deliveries;
DROP TABLE IF EXISTS estimate_items;
DROP TABLE IF EXISTS estimates;
DROP TABLE IF EXISTS materials;
DROP TABLE IF EXISTS projects;
DROP TABLE IF EXISTS suppliers;
DROP TABLE IF EXISTS users;

SET FOREIGN_KEY_CHECKS = 1;

SELECT 'База очищена. Запустите приложение — схема и демо-данные будут созданы заново.' AS `Результат`;
