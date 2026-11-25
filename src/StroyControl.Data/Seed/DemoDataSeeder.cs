using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Security;

namespace StroyControl.Data.Seed;

/// <summary>
/// Первичное наполнение базы реалистичными демонстрационными данными:
/// сотрудники, поставщики, материалы, объекты, сметы, поступления и списания.
/// Данные детерминированы (фиксированное зерно), поэтому отчёт об отклонениях
/// выглядит одинаково на любом рабочем месте.
/// </summary>
public static class DemoDataSeeder
{
    private const int Seed = 20251105;

    private static readonly decimal[] QuantityFactors =
        { 1.18m, 0.94m, 1.04m, 1.27m, 0.88m, 1.09m, 0.99m, 1.12m, 1.02m, 0.93m, 1.21m, 1.06m, 0.97m, 1.15m };

    private static readonly decimal[] PriceFactors =
        { 1.05m, 0.98m, 1.02m, 1.12m, 1.00m, 1.07m, 0.96m, 1.03m, 1.09m, 0.99m };

    private static readonly string[] LowStockCodes = { "ПЕНА-750", "КАБ-3x2.5" };

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        var random = new Random(Seed);
        var today = DateTime.Today;

        var users = BuildUsers();
        db.Users.AddRange(users);

        var suppliers = BuildSuppliers();
        db.Suppliers.AddRange(suppliers);

        var materials = BuildMaterials(suppliers);
        db.Materials.AddRange(materials);

        var projects = BuildProjects(today);
        db.Projects.AddRange(projects);

        await db.SaveChangesAsync(cancellationToken);

        var estimates = BuildEstimates(projects, materials, random, today);
        db.Estimates.AddRange(estimates);
        await db.SaveChangesAsync(cancellationToken);

        var usages = new List<MaterialUsage>();
        var deliveryDateByMaterial = new Dictionary<int, DateTime>();
        var documentIndex = 1;

        foreach (var estimate in estimates)
        {
            // Сметы будущих работ ещё не исполнялись — расхода материалов по ним нет.
            if (estimate.Date.Date > today) continue;

            foreach (var item in estimate.Items)
            {
                if (item.Material is null) continue;

                var materialId = item.Material.Id;

                var factor = QuantityFactors[documentIndex % QuantityFactors.Length];
                var priceFactor = PriceFactors[documentIndex % PriceFactors.Length];
                var total = Math.Round(item.PlannedQuantity * factor, 3);
                var parts = 2 + documentIndex % 2;
                var unitPrice = Math.Round(item.Material.Price * priceFactor, 2);

                var remaining = total;
                for (var part = 1; part <= parts; part++)
                {
                    var quantity = part == parts
                        ? remaining
                        : Math.Round(total / parts * (0.85m + (decimal)random.NextDouble() * 0.3m), 3);
                    quantity = Math.Min(quantity, remaining);
                    remaining = Math.Round(remaining - quantity, 3);
                    if (quantity <= 0m) continue;

                    // Расход относится к тому же месяцу, что и смета: так группировка
                    // по месяцам на главной панели выглядит согласованно.
                    var monthStart = new DateTime(estimate.Date.Year, estimate.Date.Month, 1);
                    var date = monthStart.AddDays(4 + random.Next(0, 22));

                    var usage = new MaterialUsage
                    {
                        ProjectId = estimate.ProjectId,
                        MaterialId = materialId,
                        EstimateId = estimate.Id,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Date = date > today ? today : date,
                        DocumentNumber = $"АР-{YearSuffix(date)}-{documentIndex:0000}",
                        Note = part == 1 ? "Списание по акту выполненных работ" : null,
                        CreatedByUserId = users[random.Next(0, users.Count)].Id
                    };

                    usages.Add(usage);

                    if (!deliveryDateByMaterial.TryGetValue(materialId, out var existing) || usage.Date < existing)
                        deliveryDateByMaterial[materialId] = usage.Date;

                    documentIndex++;
                }
            }
        }

        db.MaterialUsages.AddRange(usages);

        var deliveries = BuildDeliveries(materials, suppliers, projects, usages, deliveryDateByMaterial, random, today);
        db.Deliveries.AddRange(deliveries);

        await db.SaveChangesAsync(cancellationToken);

        await UpdateStockAsync(db, materials, cancellationToken);
    }

    private static string YearSuffix(DateTime date) => date.ToString("yy");

    private static List<User> BuildUsers()
    {
        return new List<User>
        {
            CreateUser("admin", "Иванов Алексей Петрович", UserRole.Admin, "admin123", "a.ivanov@stroycontrol.by", "+375 (29) 114-22-08"),
            CreateUser("prorab", "Дорошевич Сергей Кузьмич", UserRole.Foreman, "prorab123", "s.doroshevich@stroycontrol.by", "+375 (29) 553-71-40"),
            CreateUser("smeta", "Ковалёва Елена Викторовна", UserRole.Engineer, "smeta123", "e.kovaleva@stroycontrol.by", "+375 (33) 602-15-77"),
            CreateUser("snab", "Романов Дмитрий Олегович", UserRole.SupplyManager, "snab123", "d.romanov@stroycontrol.by", "+375 (25) 748-90-31"),
            CreateUser("buh", "Лебедева Наталья Сергеевна", UserRole.Accountant, "buh123", "n.lebedeva@stroycontrol.by", "+375 (17) 270-44-12")
        };
    }

    private static User CreateUser(string login, string name, UserRole role, string password, string email, string phone)
    {
        var (hash, salt) = PasswordHasher.CreateHash(password);
        return new User
        {
            Login = login,
            DisplayName = name,
            Role = role,
            Email = email,
            Phone = phone,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow.AddDays(-2)
        };
    }

    private static List<Supplier> BuildSuppliers()
    {
        return new List<Supplier>
        {
            new()
            {
                Name = "ОАО «СтройБаза №1»", Unp = "100234567", ContactPerson = "Мельник Павел Игоревич",
                Phone = "+375 (17) 344-12-90", Email = "sales@stroybaza1.by", Address = "г. Минск, ул. Промышленная, 12",
                Rating = 5, Note = "Основной поставщик вяжущих и инертных материалов, отсрочка 30 дней."
            },
            new()
            {
                Name = "ООО «БелКерамика»", Unp = "190876543", ContactPerson = "Сидорова Анна Львовна",
                Phone = "+375 (17) 298-55-41", Email = "order@belkeramika.by", Address = "г. Минск, пр-т Партизанский, 95",
                Rating = 4, Note = "Кирпич и керамическая плитка, доставка 2–3 дня."
            },
            new()
            {
                Name = "СООО «МеталлТоргСервис»", Unp = "191234567", ContactPerson = "Кузьмин Артём Валерьевич",
                Phone = "+375 (17) 336-70-18", Email = "zakaz@mts.by", Address = "г. Минск, ул. Тимирязева, 65",
                Rating = 4, Note = "Металлопрокат, резка по размерам."
            },
            new()
            {
                Name = "ООО «БетонГрупп»", Unp = "192345678", ContactPerson = "Тарасевич Игорь Петрович",
                Phone = "+375 (17) 512-33-07", Email = "beton@betongroup.by", Address = "г. Минск, ул. Кабушкина, 78",
                Rating = 3, Note = "Товарный бетон, бетононасос. Возможны сдвиги графика."
            },
            new()
            {
                Name = "ЧУП «Отделочные материалы Плюс»", Unp = "193456789", ContactPerson = "Гурская Марина Олеговна",
                Phone = "+375 (29) 611-08-52", Email = "info@otdelplus.by", Address = "г. Минск, ул. Притыцкого, 29",
                Rating = 5, Note = "ГКЛ, профили, краски, плитка."
            },
            new()
            {
                Name = "ООО «ДревПром»", Unp = "194567890", ContactPerson = "Лисовский Олег Николаевич",
                Phone = "+375 (177) 74-20-05", Email = "sale@drevprom.by", Address = "г. Борисов, ул. Днепровская, 4",
                Rating = 4, Note = "Плиты OSB, пиломатериалы, рубероид."
            }
        };
    }

    private static List<Material> BuildMaterials(List<Supplier> suppliers)
    {
        var definitions = new (string Code, string Name, string Category, UnitOfMeasure Unit, decimal Price, decimal MinStock, int Supplier, string? Note)[]
        {
            ("ЦЕМ-500", "Цемент ПЦ 500-Д0, мешок 50 кг", "Вяжущие", UnitOfMeasure.Bag, 18.50m, 800m, 0, "Хранение в сухом складе, срок годности 60 суток."),
            ("БЕТ-B25", "Бетон товарный B25 W4", "Бетон", UnitOfMeasure.CubicMeter, 185.00m, 80m, 3, "Поставка автобетоносмесителем, минимум 5 м³."),
            ("АРМ-500-12", "Арматура А500С Ø12 мм", "Металлопрокат", UnitOfMeasure.Kilogram, 4.20m, 5000m, 2, "Сертификат соответствия СТБ 1704."),
            ("КИР-1НФ", "Кирпич керамический полнотелый 1НФ", "Стеновые", UnitOfMeasure.Piece, 1.15m, 20000m, 1, "Партии по 5 000 шт. на поддоне."),
            ("БЛОК-D600", "Блок газосиликатный D600", "Стеновые", UnitOfMeasure.CubicMeter, 132.00m, 90m, 0, null),
            ("ПЕС-МЫТ", "Песок строительный мытый", "Инертные", UnitOfMeasure.Ton, 22.00m, 200m, 0, null),
            ("ЩЕБ-5-20", "Щебень гранитный 5–20 мм", "Инертные", UnitOfMeasure.Ton, 38.00m, 180m, 0, null),
            ("ГКЛ-12.5", "Гипсокартон 12,5 мм 2500×1200", "Отделочные", UnitOfMeasure.Sheet, 12.80m, 300m, 4, null),
            ("ПРОФ-CD60", "Профиль CD-60, 3 м", "Отделочные", UnitOfMeasure.Piece, 6.40m, 400m, 4, null),
            ("МВ-100", "Минеральная вата 100 мм", "Изоляция", UnitOfMeasure.SquareMeter, 8.90m, 500m, 4, null),
            ("ПЛИТ-300", "Плитка керамическая 300×300", "Отделочные", UnitOfMeasure.SquareMeter, 28.40m, 300m, 1, "Расход клея — 4,5 кг/м²."),
            ("КРАС-ФАС", "Краска фасадная акриловая", "Отделочные", UnitOfMeasure.Kilogram, 14.20m, 250m, 4, null),
            ("ОСБ-18", "Плита OSB-3, 18 мм", "Листовые", UnitOfMeasure.Sheet, 32.00m, 120m, 5, null),
            ("РУБ-350", "Рубероид РКП-350", "Кровельные", UnitOfMeasure.Roll, 21.00m, 120m, 5, null),
            ("ПЕНА-750", "Пена монтажная 750 мл", "Расходные", UnitOfMeasure.Piece, 9.80m, 150m, 4, null),
            ("КАБ-3x2.5", "Кабель ВВГнг 3×2,5", "Электрика", UnitOfMeasure.LinearMeter, 3.10m, 2000m, 2, null)
        };

        return definitions
            .Select(d => new Material
            {
                Code = d.Code,
                Name = d.Name,
                Category = d.Category,
                Unit = d.Unit,
                Price = d.Price,
                MinStock = d.MinStock,
                Supplier = suppliers[d.Supplier],
                IsActive = true,
                Note = d.Note
            })
            .ToList();
    }

    private static List<Project> BuildProjects(DateTime today)
    {
        return new List<Project>
        {
            new()
            {
                Code = "ОБ-2025-01",
                Name = "ЖК «Северный берег», корпус 3",
                Address = "г. Минск, ул. Кальварийская, 24",
                Customer = "ОАО «Городской квартал»",
                ManagerName = "Дорошевич С. К.",
                ContractSum = 4_850_000m,
                TotalArea = 8_640m,
                StartDate = today.AddMonths(-8),
                PlannedEndDate = today.AddMonths(10),
                Status = ProjectStatus.InProgress,
                ProgressPercent = 42,
                Note = "Монолитный каркас, 14 этажей, два подземных уровня."
            },
            new()
            {
                Code = "ОБ-2025-02",
                Name = "Средняя школа №4 на 720 мест",
                Address = "г. Борисов, ул. Гагарина, 15",
                Customer = "УКС Борисовского райисполкома",
                ManagerName = "Ковалёва Е. В.",
                ContractSum = 6_200_000m,
                TotalArea = 7_250m,
                StartDate = today.AddMonths(-10),
                PlannedEndDate = today.AddMonths(7),
                Status = ProjectStatus.InProgress,
                ProgressPercent = 61,
                Note = "Объект социального назначения, финансирование из бюджета."
            },
            new()
            {
                Code = "ОБ-2024-07",
                Name = "Логистический центр «ЛогоПарк-Смолевичи»",
                Address = "г. Смолевичи, промышленная зона, участок 7",
                Customer = "ООО «ЛогоПарк»",
                ManagerName = "Романов Д. О.",
                ContractSum = 3_400_000m,
                TotalArea = 11_800m,
                StartDate = today.AddMonths(-16),
                PlannedEndDate = today.AddMonths(2),
                Status = ProjectStatus.InProgress,
                ProgressPercent = 78,
                Note = "Склад класса A, каркас из металлоконструкций."
            },
            new()
            {
                Code = "ОБ-2026-03",
                Name = "Реконструкция котельной №2",
                Address = "г. Полоцк, ул. Октябрьская, 8",
                Customer = "РУП «Полоцктеплоэнерго»",
                ManagerName = "Иванов А. П.",
                ContractSum = 1_150_000m,
                TotalArea = 1_240m,
                StartDate = today.AddMonths(2),
                PlannedEndDate = today.AddMonths(14),
                Status = ProjectStatus.Planning,
                ProgressPercent = 5,
                Note = "Сметная документация передана на проверку, работы не начаты."
            },
            new()
            {
                Code = "ОБ-2025-04",
                Name = "Реконструкция инженерных сетей микрорайона «Запад-2»",
                Address = "г. Минск, ул. Притыцкого, 60 — ул. Кунцевщина, 12",
                Customer = "УП «Минскводоканал»",
                ManagerName = "Романов Д. О.",
                ContractSum = 2_180_000m,
                TotalArea = 3_150m,
                StartDate = today.AddMonths(-5),
                PlannedEndDate = today.AddMonths(6),
                Status = ProjectStatus.InProgress,
                ProgressPercent = 34,
                Note = "Замена магистральных трубопроводов без остановки водоснабжения."
            },
            new()
            {
                Code = "ОБ-2025-05",
                Name = "Детский сад на 240 мест",
                Address = "г. Дзержинск, ул. Первомайская, 41",
                Customer = "УКС Дзержинского райисполкома",
                ManagerName = "Ковалёва Е. В.",
                ContractSum = 5_640_000m,
                TotalArea = 4_980m,
                StartDate = today.AddMonths(-12),
                PlannedEndDate = today.AddMonths(3),
                Status = ProjectStatus.InProgress,
                ProgressPercent = 87,
                Note = "Три этажа, бассейн, благоустройство участка площадью 1,2 га."
            },
            new()
            {
                Code = "ОБ-2024-11",
                Name = "Административное здание районного исполкома",
                Address = "г. Вилейка, ул. Советская, 3",
                Customer = "Вилейский райисполком",
                ManagerName = "Иванов А. П.",
                ContractSum = 1_920_000m,
                TotalArea = 2_640m,
                StartDate = today.AddMonths(-20),
                PlannedEndDate = today.AddMonths(-2),
                Status = ProjectStatus.Completed,
                ProgressPercent = 100,
                Note = "Объект принят в эксплуатацию, сметы перенесены в архив."
            },
            new()
            {
                Code = "ОБ-2026-01",
                Name = "Физкультурно-оздоровительный комплекс с бассейном",
                Address = "г. Барановичи, ул. Фабричная, 9",
                Customer = "ОАО «Барановичистройсервис»",
                ManagerName = "Дорошевич С. К.",
                ContractSum = 7_300_000m,
                TotalArea = 9_400m,
                StartDate = today.AddMonths(4),
                PlannedEndDate = today.AddMonths(22),
                Status = ProjectStatus.Planning,
                ProgressPercent = 0,
                Note = "Проектная документация проходит государственную экспертизу."
            }
        };
    }

    /// <summary>Типовые работы для демонстрационных смет (по разделам).</summary>
    private static List<(string Name, string Unit, string Section, decimal Quantity, decimal Price)> WorksFor(
        IReadOnlyList<string> sections)
    {
        var works = new List<(string Name, string Unit, string Section, decimal Quantity, decimal Price)>();

        if (sections.Contains("Фундамент"))
        {
            works.Add(("Разработка грунта экскаватором", "м3", "Фундамент", 420m, 12.40m));
            works.Add(("Устройство бетонной подготовки", "м3", "Фундамент", 68m, 96.00m));
            works.Add(("Армирование фундаментной плиты", "т", "Фундамент", 12.5m, 640.00m));
        }

        if (sections.Contains("Стены"))
        {
            works.Add(("Кладка наружных стен из блоков", "м3", "Стены", 310m, 78.50m));
            works.Add(("Монтаж перемычек", "шт", "Стены", 96m, 24.00m));
        }

        if (sections.Contains("Кровля"))
            works.Add(("Устройство пароизоляции кровли", "м2", "Кровля", 640m, 6.30m));

        if (sections.Contains("Кладка"))
        {
            works.Add(("Кладка стен из кирпича", "м3", "Кладка", 640m, 92.00m));
            works.Add(("Устройство вентиляционных каналов", "шт", "Кладка", 48m, 63.00m));
        }

        if (sections.Contains("Каркас"))
        {
            works.Add(("Монтаж металлокаркаса", "т", "Каркас", 86m, 980.00m));
            works.Add(("Антикоррозийная защита конструкций", "м2", "Каркас", 1240m, 7.40m));
        }

        if (sections.Contains("Отделка"))
        {
            works.Add(("Штукатурка стен", "м2", "Отделка", 2400m, 9.80m));
            works.Add(("Облицовка плиткой", "м2", "Отделка", 620m, 18.60m));
            works.Add(("Окраска потолков", "м2", "Отделка", 1150m, 5.40m));
        }

        if (sections.Contains("Электрика"))
            works.Add(("Прокладка кабельных трасс", "м", "Электрика", 3200m, 4.10m));

        if (sections.Contains("Основания"))
        {
            works.Add(("Устройство песчано-щебёночного основания", "м2", "Основания", 2800m, 6.80m));
            works.Add(("Уплотнение грунта", "м3", "Основания", 1900m, 3.20m));
        }

        if (sections.Contains("Благоустройство"))
        {
            works.Add(("Устройство асфальтобетонного покрытия", "м2", "Благоустройство", 3400m, 14.20m));
            works.Add(("Озеленение территории", "м2", "Благоустройство", 2100m, 4.60m));
        }

        if (sections.Contains("Наружные сети"))
        {
            works.Add(("Разработка траншей", "м3", "Наружные сети", 2600m, 11.80m));
            works.Add(("Укладка трубопроводов", "м", "Наружные сети", 1850m, 22.40m));
        }

        if (works.Count == 0)
            works.Add(("Общестроительные работы", "раб.", "Общестроительные работы", 1m, 1500.00m));

        return works;
    }

    private static List<Estimate> BuildEstimates(
        List<Project> projects,
        List<Material> materials,
        Random random,
        DateTime today)
    {
        Estimate Create(
            string projectCode,
            string number,
            string name,
            DateTime date,
            EstimateStatus status,
            params (string Material, string Section, decimal Quantity)[] items)
        {
            var project = projects.First(p => p.Code == projectCode);
            var estimate = new Estimate
            {
                Number = number,
                Name = name,
                Project = project,
                Date = date,
                Status = status,
                Note = status switch
                {
                    EstimateStatus.Approved => "Утверждена заказчиком.",
                    EstimateStatus.Archived => "Работы завершены, документ перенесён в архив.",
                    _ => null
                }
            };

            foreach (var item in items)
            {
                var material = materials.First(m => m.Code == item.Material);
                estimate.Items.Add(new EstimateItem
                {
                    MaterialId = material.Id,
                    Material = material,
                    Section = item.Section,
                    PlannedQuantity = item.Quantity,
                    PlannedUnitPrice = Math.Round(material.Price * (0.96m + (decimal)random.NextDouble() * 0.08m), 2)
                });
            }

            // Работы: в демо-данных они идут отдельными позициями, чтобы отчёт
            // по смете показывал и работы, и материалы (как в специализированных программах).
            foreach (var work in WorksFor(items.Select(i => i.Section).Distinct().ToList()))
            {
                estimate.Items.Add(new EstimateItem
                {
                    IsWork = true,
                    WorkName = work.Name,
                    WorkUnit = work.Unit,
                    Section = work.Section,
                    PlannedQuantity = work.Quantity,
                    PlannedUnitPrice = work.Price
                });
            }

            return estimate;
        }

        // Даты смет распределены по месяцам: на главной панели и в списках видно,
        // как документы группируются по периодам, а не сваливаются в один список.
        return new List<Estimate>
        {
            Create("ОБ-2025-01", "СМ-01/25", "Смета на общестроительные работы", today.AddMonths(-8), EstimateStatus.Approved,
                ("ЦЕМ-500", "Фундамент", 24000m),
                ("АРМ-500-12", "Фундамент", 185000m),
                ("БЕТ-B25", "Фундамент", 3200m),
                ("КИР-1НФ", "Стены", 420000m),
                ("БЛОК-D600", "Стены", 2400m),
                ("ПЕС-МЫТ", "Фундамент", 1800m),
                ("ЩЕБ-5-20", "Фундамент", 1500m)),

            Create("ОБ-2025-01", "СМ-02/25", "Смета на отделочные работы", today.AddMonths(-1), EstimateStatus.OnReview,
                ("ГКЛ-12.5", "Отделка", 6400m),
                ("ПРОФ-CD60", "Отделка", 8200m),
                ("МВ-100", "Отделка", 5400m),
                ("КРАС-ФАС", "Отделка", 2600m),
                ("ПЛИТ-300", "Отделка", 1800m)),

            Create("ОБ-2025-02", "СМ-11/25", "Смета на возведение корпуса", today.AddMonths(-10), EstimateStatus.Approved,
                ("КИР-1НФ", "Кладка", 960000m),
                ("ЦЕМ-500", "Кладка", 31000m),
                ("АРМ-500-12", "Каркас", 240000m),
                ("БЕТ-B25", "Каркас", 4100m),
                ("ОСБ-18", "Кровля", 2600m),
                ("РУБ-350", "Кровля", 2200m),
                ("МВ-100", "Кровля", 4800m)),

            Create("ОБ-2025-02", "СМ-12/25", "Смета на инженерные сети", today, EstimateStatus.Draft,
                ("КАБ-3x2.5", "Электрика", 4200m),
                ("ПЕНА-750", "Электрика", 180m)),

            Create("ОБ-2024-07", "СМ-21/24", "Смета на каркас и кровлю", today.AddMonths(-16), EstimateStatus.Approved,
                ("АРМ-500-12", "Каркас", 310000m),
                ("БЕТ-B25", "Каркас", 5200m),
                ("ЩЕБ-5-20", "Основания", 2600m),
                ("ПЕС-МЫТ", "Основания", 2400m),
                ("РУБ-350", "Кровля", 1800m),
                ("МВ-100", "Кровля", 6200m),
                ("ОСБ-18", "Кровля", 3000m)),

            Create("ОБ-2025-04", "СМ-31/25", "Смета на наружные сети водоснабжения", today.AddMonths(-4), EstimateStatus.Approved,
                ("ПЕС-МЫТ", "Наружные сети", 3200m),
                ("ЩЕБ-5-20", "Наружные сети", 2800m),
                ("ЦЕМ-500", "Наружные сети", 6400m),
                ("КАБ-3x2.5", "Электрика", 1500m)),

            Create("ОБ-2025-04", "СМ-32/25", "Смета на восстановление благоустройства", today.AddMonths(-1), EstimateStatus.Draft,
                ("ПЕС-МЫТ", "Благоустройство", 900m),
                ("ЩЕБ-5-20", "Благоустройство", 700m)),

            Create("ОБ-2025-05", "СМ-41/25", "Смета на общестроительные работы", today.AddMonths(-11), EstimateStatus.Approved,
                ("КИР-1НФ", "Кладка", 540000m),
                ("ЦЕМ-500", "Кладка", 18000m),
                ("АРМ-500-12", "Каркас", 120000m),
                ("БЕТ-B25", "Каркас", 2400m),
                ("ГКЛ-12.5", "Отделка", 3200m),
                ("ПЛИТ-300", "Отделка", 1400m),
                ("КРАС-ФАС", "Отделка", 1800m)),

            Create("ОБ-2025-05", "СМ-42/25", "Смета на благоустройство территории", today, EstimateStatus.OnReview,
                ("ПЕС-МЫТ", "Благоустройство", 1400m),
                ("ЩЕБ-5-20", "Благоустройство", 1200m),
                ("БЛОК-D600", "Благоустройство", 60m)),

            Create("ОБ-2024-11", "СМ-51/24", "Смета на реконструкцию здания", today.AddMonths(-19), EstimateStatus.Archived,
                ("КИР-1НФ", "Кладка", 210000m),
                ("ЦЕМ-500", "Кладка", 9500m),
                ("ГКЛ-12.5", "Отделка", 2100m),
                ("КРАС-ФАС", "Отделка", 1200m),
                ("МВ-100", "Отделка", 1600m)),

            Create("ОБ-2026-03", "СМ-61/26", "Смета на замену котельного оборудования", today.AddMonths(2), EstimateStatus.OnReview,
                ("АРМ-500-12", "Каркас", 18000m),
                ("ЦЕМ-500", "Фундамент", 2400m),
                ("КАБ-3x2.5", "Электрика", 900m)),

            Create("ОБ-2026-01", "СМ-71/26", "Смета на каркас и кровлю комплекса", today.AddMonths(4), EstimateStatus.Draft,
                ("АРМ-500-12", "Каркас", 420000m),
                ("БЕТ-B25", "Каркас", 6800m),
                ("ОСБ-18", "Кровля", 4200m),
                ("РУБ-350", "Кровля", 3600m),
                ("МВ-100", "Кровля", 7800m))
        };
    }

    private static List<Delivery> BuildDeliveries(
        List<Material> materials,
        List<Supplier> suppliers,
        List<Project> projects,
        List<MaterialUsage> usages,
        Dictionary<int, DateTime> firstUsageDate,
        Random random,
        DateTime today)
    {
        var deliveries = new List<Delivery>();
        var number = 1;
        var activeProjects = projects.Where(p => p.Status == ProjectStatus.InProgress).ToList();

        foreach (var material in materials)
        {
            var used = usages.Where(u => u.MaterialId == material.Id).Sum(u => u.Quantity);
            var isLowStock = LowStockCodes.Contains(material.Code);
            var targetStock = isLowStock
                ? Math.Round(material.MinStock * 0.55m, 3)
                : Math.Round(used * 0.22m + material.MinStock * 2.4m, 3);

            var quantity = Math.Round(used + targetStock, 3);
            if (quantity <= 0m) quantity = material.MinStock;

            var project = activeProjects.FirstOrDefault(p => usages.Any(u => u.MaterialId == material.Id && u.ProjectId == p.Id));
            var date = firstUsageDate.TryGetValue(material.Id, out var firstDate)
                ? firstDate.AddDays(-14)
                : today.AddMonths(-5);

            deliveries.Add(new Delivery
            {
                Number = $"ПН-{date:yy}-{number++:0000}",
                Date = date,
                SupplierId = material.SupplierId ?? suppliers[0].Id,
                ProjectId = project?.Id,
                InvoiceNumber = $"ТТН-{random.Next(100000, 999999)}",
                Note = "Ввод остатков и плановая поставка по договору.",
                Items =
                {
                    new DeliveryItem { MaterialId = material.Id, Quantity = quantity, UnitPrice = Math.Round(material.Price * 0.97m, 2) }
                }
            });
        }

        // Дополнительные поставки в течение последних месяцев — для «живой» ленты операций.
        var extraMaterials = materials.Where(m => !LowStockCodes.Contains(m.Code)).Take(8).ToList();
        foreach (var material in extraMaterials)
        {
            var project = activeProjects[random.Next(0, activeProjects.Count)];
            var date = today.AddDays(-random.Next(5, 120));
            var quantity = Math.Round(material.MinStock * (0.8m + (decimal)random.NextDouble() * 1.4m), 3);

            deliveries.Add(new Delivery
            {
                Number = $"ПН-{date:yy}-{number++:0000}",
                Date = date,
                SupplierId = material.SupplierId ?? suppliers[0].Id,
                ProjectId = project.Id,
                InvoiceNumber = $"ТТН-{random.Next(100000, 999999)}",
                Note = null,
                Items =
                {
                    new DeliveryItem { MaterialId = material.Id, Quantity = quantity, UnitPrice = Math.Round(material.Price * (0.98m + (decimal)random.NextDouble() * 0.1m), 2) }
                }
            });
        }

        return deliveries;
    }

    /// <summary>Остаток на складе = сумма поступлений − сумма списаний (данные согласованы).</summary>
    private static async Task UpdateStockAsync(AppDbContext db, List<Material> materials, CancellationToken cancellationToken)
    {
        var delivered = await db.DeliveryItems
            .AsNoTracking()
            .GroupBy(i => i.MaterialId)
            .Select(g => new { MaterialId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, cancellationToken);

        var used = await db.MaterialUsages
            .AsNoTracking()
            .GroupBy(u => u.MaterialId)
            .Select(g => new { MaterialId = g.Key, Quantity = g.Sum(u => u.Quantity) })
            .ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, cancellationToken);

        var tracked = await db.Materials.Where(m => materials.Select(x => x.Id).Contains(m.Id)).ToListAsync(cancellationToken);
        foreach (var material in tracked)
        {
            var incoming = delivered.TryGetValue(material.Id, out var d) ? d : 0m;
            var outgoing = used.TryGetValue(material.Id, out var u) ? u : 0m;
            material.StockQuantity = Math.Round(incoming - outgoing, 3);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
