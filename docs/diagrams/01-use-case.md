# Диаграмма вариантов использования

![Диаграмма вариантов использования](img/01-use-case.png)

```mermaid
flowchart LR
    admin(["Администратор"])
    engineer(["Инженер ПТО"])
    supply(["Снабженец"])
    foreman(["Прораб"])
    accountant(["Бухгалтер"])

    subgraph syst["СтройКонтроль"]
        auth(("Авторизация в системе"))
        rights(("Проверка прав доступа"))
        catalog(("Ведение справочников<br/>материалы, поставщики, объекты"))
        estimates(("Работа со сметами<br/>и позициями плана"))
        deliveries(("Оформление поступлений"))
        usages(("Оформление списаний материалов"))
        stock(("Контроль остатков и дефицита"))
        deviation(("Анализ отклонений<br/>план — факт"))
        export(("Экспорт отчёта в CSV"))
        users(("Управление сотрудниками"))
        settings(("Настройка подключения<br/>к SQL Server"))
    end

    engineer --> auth
    supply --> auth
    foreman --> auth
    accountant --> auth
    admin --> auth
    auth -. "include" .-> rights

    engineer --> catalog
    engineer --> estimates
    engineer --> deliveries
    engineer --> usages
    engineer --> deviation
    engineer --> settings

    supply --> catalog
    supply --> deliveries
    supply --> deviation

    foreman --> deliveries
    foreman --> usages
    foreman --> stock

    accountant --> deviation
    accountant --> export

    admin --> users
    admin --> settings
    deviation -. "extend" .-> export
```

Пять акторов соответствуют пяти ролям системы. Общими для всех являются авторизация и проверка прав доступа; редактирование смет доступно только инженеру ПТО и администратору, прораб работает с поступлениями и списаниями, бухгалтер — только просмотр и экспорт отчётов.
