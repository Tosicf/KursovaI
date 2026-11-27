using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Services;

namespace StroyControl.Data.Services;

/// <summary>
/// Собирает показатели для главной панели: KPI, бюджет объектов, ленту операций, остатки.
/// Панель всегда ограничена периодом: без выбранного месяца показываются последние
/// по дате объекты (не более <see cref="MaxBudgetRows"/>), поэтому при росте числа
/// объектов главная страница не разрастается.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    /// <summary>Сколько объектов попадает в блок «Исполнение смет по объектам».</summary>
    public const int MaxBudgetRows = 10;

    /// <summary>Сколько месяцев показывает график при выборе конкретного месяца.</summary>
    private const int MonthWindow = 6;

    /// <summary>Предел глубины графика для «всего времени» — больше не читается.</summary>
    private const int MaxMonthWindow = 24;

    private readonly IDbContextFactory<AppDbContext> _factory;

    public DashboardService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<DashboardData> LoadAsync(PeriodFilter? period = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var projects = await db.Projects.AsNoTracking().ToListAsync(cancellationToken);
        var materials = await db.Materials.AsNoTracking().Include(m => m.Supplier).ToListAsync(cancellationToken);
        // Действующая смета — только та, что передана в работу: черновик, смета на
        // проверке, утверждённая без запуска и архивная в KPI и на панели не участвуют.
        var estimates = await db.Estimates
            .AsNoTracking()
            .Where(e => e.Status == EstimateStatus.InWork)
            .Include(e => e.Items)
            .ToListAsync(cancellationToken);
        var deliveries = await db.Deliveries
            .AsNoTracking()
            .Include(d => d.Supplier)
            .Include(d => d.Items)
            .ToListAsync(cancellationToken);
        var usages = await db.MaterialUsages
            .AsNoTracking()
            .Include(u => u.Material)
            .Include(u => u.Project)
            .ToListAsync(cancellationToken);

        // Работы учитываются в фактических затратах так же, как материалы: акт
        // выполненных работ по смете в работе даёт стоимость выполненных работ.
        var completions = await db.WorkCompletions
            .AsNoTracking()
            .Where(c => c.Estimate != null && c.Estimate.Status == EstimateStatus.InWork)
            .Include(c => c.Project)
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);

        var filter = period ?? PeriodFilter.All;
        var periodEnd = filter.End;

        // Операции выбранного периода: по ним считаются структура затрат, лента и диаграмма.
        var usagesInPeriod = usages.Where(u => filter.Contains(u.Date)).ToList();
        var completionsInPeriod = completions.Where(c => filter.Contains(c.Date)).ToList();
        var deliveriesInPeriod = deliveries.Where(d => filter.Contains(d.Date)).ToList();

        // Исполнение бюджета объекта считается накопленным итогом по конец периода,
        // иначе «план» брался бы за весь срок объекта, а «факт» — только за период.
        var estimatesToDate = periodEnd is null
            ? estimates
            : estimates.Where(e => e.Date <= periodEnd.Value).ToList();

        var usagesToDate = periodEnd is null
            ? usages
            : usages.Where(u => u.Date <= periodEnd.Value).ToList();

        var plannedByProject = estimatesToDate
            .GroupBy(e => e.ProjectId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Items.Sum(i => i.PlannedQuantity * i.PlannedUnitPrice)));

        var actualByProject = usagesToDate
            .GroupBy(u => u.ProjectId)
            .ToDictionary(g => g.Key, g => g.Sum(u => u.Quantity * u.UnitPrice));

        // Стоимость выполненных работ добавляется к фактическим затратам объекта.
        var completionsToDate = periodEnd is null
            ? completions
            : completions.Where(c => c.Date <= periodEnd.Value).ToList();

        foreach (var group in completionsToDate.GroupBy(c => c.ProjectId))
            actualByProject[group.Key] = actualByProject.GetValueOrDefault(group.Key) + group.Sum(c => c.WorksCost);

        // Месяц объекта — месяц его последней операции (смета, поступление, списание),
        // а если операций ещё нет — месяц начала работ.
        var lastEstimate = estimates
            .GroupBy(e => e.ProjectId)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Date));

        var lastUsage = usages
            .GroupBy(u => u.ProjectId)
            .ToDictionary(g => g.Key, g => g.Max(u => u.Date));

        var lastDelivery = deliveries
            .Where(d => d.ProjectId.HasValue)
            .GroupBy(d => d.ProjectId!.Value)
            .ToDictionary(g => g.Key, g => g.Max(d => d.Date));

        DateTime ProjectMonth(Project project)
        {
            var candidates = new List<DateTime> { project.StartDate };

            if (lastEstimate.TryGetValue(project.Id, out var estimateDate)) candidates.Add(estimateDate);
            if (lastUsage.TryGetValue(project.Id, out var usageDate)) candidates.Add(usageDate);
            if (lastDelivery.TryGetValue(project.Id, out var deliveryDate)) candidates.Add(deliveryDate);

            return MonthPeriod.Start(candidates.Max());
        }

        var allBudgets = projects
            .Select(p => new ProjectBudgetRow(
                p.Id,
                p.Code,
                p.Name,
                plannedByProject.TryGetValue(p.Id, out var planned) ? planned : 0m,
                actualByProject.TryGetValue(p.Id, out var actual) ? actual : 0m,
                p.Status,
                ProjectMonth(p)))
            .ToList();

        // Объект попадает в период, если в этом году/месяце по нему были документы —
        // сметы, поступления или списания. Поэтому долгострой показывается в каждом
        // месяце, в котором велись работы, а не только в месяце последней операции.
        // Объект без документов виден в месяце начала работ.
        var activityMonths = new Dictionary<int, List<DateTime>>();

        void Remember(int projectId, DateTime date)
        {
            if (!activityMonths.TryGetValue(projectId, out var dates))
                activityMonths[projectId] = dates = new List<DateTime>();

            dates.Add(date);
        }

        foreach (var usage in usages) Remember(usage.ProjectId, usage.Date);
        foreach (var completion in completions) Remember(completion.ProjectId, completion.Date);
        foreach (var estimate in estimates) Remember(estimate.ProjectId, estimate.Date);

        foreach (var delivery in deliveries)
        {
            if (delivery.ProjectId is { } deliveryProjectId)
                Remember(deliveryProjectId, delivery.Date);
        }

        var startMonths = projects.ToDictionary(p => p.Id, p => MonthPeriod.Start(p.StartDate));

        bool InPeriod(ProjectBudgetRow row)
        {
            if (filter.IsAll) return true;
            if (startMonths.TryGetValue(row.ProjectId, out var startMonth) && filter.Contains(startMonth)) return true;

            return activityMonths.TryGetValue(row.ProjectId, out var dates) && dates.Any(filter.Contains);
        }

        // В выбранном периоде подписываем строку месяцем последней операции внутри
        // периода, иначе у долгостроя в списке стоял бы месяц из другого периода.
        var budgetsInPeriod = allBudgets
            .Where(InPeriod)
            .Select(row =>
            {
                if (filter.IsAll) return row;

                var dates = activityMonths.TryGetValue(row.ProjectId, out var list) ? list : new List<DateTime>();
                var lastInPeriod = dates.Where(filter.Contains).DefaultIfEmpty(DateTime.MinValue).Max();

                var month = lastInPeriod == DateTime.MinValue
                    ? startMonths[row.ProjectId]
                    : MonthPeriod.Start(lastInPeriod);

                return row with { Month = month };
            })
            .ToList();

        // Сортировка по месяцам: свежие месяцы сверху, внутри месяца — крупные сметы.
        var budgets = budgetsInPeriod
            .OrderByDescending(b => b.Month)
            .ThenByDescending(b => b.PlannedCost)
            .Take(MaxBudgetRows)
            .ToList();

        var activity = usagesInPeriod
            .Select(u => new ActivityItem(
                u.Date,
                $"Списание · {u.Material?.Name ?? "материал"}",
                $"{u.Project?.Name ?? "—"} · {u.Quantity:N2} {u.Material?.UnitDisplay} · {u.DocumentNumber}",
                "usage"))
            .Concat(completionsInPeriod.Select(c => new ActivityItem(
                c.Date,
                $"Акт работ · {c.DocumentNumber}",
                $"{c.Project?.Name ?? "—"} · {c.ItemsCount} поз. · {c.WorksCost:N2} BYN",
                "delivery")))
            .Concat(deliveriesInPeriod.Select(d => new ActivityItem(
                d.Date,
                $"Поступление · {d.Number}",
                $"{d.Supplier?.Name ?? "—"} · {d.ItemsCount} поз. · {d.Total:N2} BYN",
                "delivery")))
            .OrderByDescending(a => a.Date)
            .Take(8)
            .ToList();

        var alerts = materials
            .Where(m => m.IsActive && m.StockQuantity <= m.MinStock)
            .OrderBy(m => m.StockQuantity)
            .ThenBy(m => m.Name)
            .Take(6)
            .ToList();

        var categoryCosts = usagesInPeriod
            .Select(u => (Category: string.IsNullOrWhiteSpace(u.Material?.Category) ? "Прочее" : u.Material!.Category,
                          Cost: u.Quantity * u.UnitPrice))
            .ToList();

        // Работы — отдельная статья структуры затрат, рядом с категориями материалов.
        if (completionsInPeriod.Count > 0)
            categoryCosts.Add(("Работы", completionsInPeriod.Sum(c => c.WorksCost)));

        var categories = BuildCategorySeries(categoryCosts);

        // Окно графика следует за периодом: месяц — последние полгода, год — все его
        // месяцы, «всё время» — от первого документа до последнего (не больше двух лет).
        var monthly = BuildMonthlySeries(
            usages.Select(u => (u.Date, Cost: u.Quantity * u.UnitPrice))
                .Concat(completions.Select(c => (c.Date, Cost: c.WorksCost)))
                .ToList(),
            filter);

        // Список месяцев для выпадающего фильтра — по всем документам, независимо от выбранного периода.
        var periodMonths = usages.Select(u => (DateTime?)u.Date)
            .Concat(deliveries.Select(d => (DateTime?)d.Date))
            .Concat(completions.Select(c => (DateTime?)c.Date))
            .Concat(estimates.Select(e => (DateTime?)e.Date))
            .Where(d => d.HasValue)
            .Select(d => MonthPeriod.Start(d!.Value))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        return new DashboardData
        {
            ActiveProjects = projects.Count(p => p.Status is ProjectStatus.InProgress or ProjectStatus.Planning),
            MaterialsCount = materials.Count,
            LowStockCount = materials.Count(m => m.IsActive && m.StockQuantity <= m.MinStock),
            StockValue = materials.Sum(m => m.StockQuantity * m.Price),
            PlannedCost = budgetsInPeriod.Sum(b => b.PlannedCost),
            ActualCost = budgetsInPeriod.Sum(b => b.ActualCost),
            UsagesCount = usagesInPeriod.Count,
            DeliveriesCount = deliveriesInPeriod.Count,
            ProjectBudgets = budgets,
            BudgetRowsTotal = budgetsInPeriod.Count,
            RecentActivity = activity,
            StockAlerts = alerts,
            MonthlyCosts = monthly,
            CategoryCosts = categories,
            PeriodMonths = periodMonths
        };
    }

    /// <summary>Затраты по категориям материалов с долей в процентах (топ-5 + «Прочие»).</summary>
    private static IReadOnlyList<CategoryCostRow> BuildCategorySeries(IReadOnlyList<(string Category, decimal Cost)> usages)
    {
        var total = usages.Sum(u => u.Cost);
        if (total <= 0m) return Array.Empty<CategoryCostRow>();

        var grouped = usages
            .GroupBy(u => u.Category)
            .Select(g => new { Category = g.Key, Cost = g.Sum(x => x.Cost) })
            .OrderByDescending(x => x.Cost)
            .ToList();

        var top = grouped.Take(5).ToList();
        var rest = grouped.Skip(5).Sum(x => x.Cost);
        if (rest > 0m) top.Add(new { Category = "Прочие", Cost = rest });

        return top
            .Select(x => new CategoryCostRow(
                x.Category,
                Math.Round(x.Cost, 2),
                Math.Round(x.Cost / total * 100m, 1)))
            .ToList();
    }

    /// <summary>
    /// Факт затрат по месяцам и среднемесячный план. Окно зависит от выбранного
    /// периода: месяц — последние <see cref="MonthWindow"/> месяцев, год — все месяцы
    /// года, «всё время» — весь срок данных, но не глубже <see cref="MaxMonthWindow"/>.
    /// </summary>
    private static IReadOnlyList<MonthlyCostRow> BuildMonthlySeries(
        IReadOnlyList<(DateTime Date, decimal Cost)> usages,
        PeriodFilter filter)
    {
        var today = MonthPeriod.Start(DateTime.Today);

        DateTime start;
        DateTime end;

        if (filter.Year is { } year && filter.Month is { } month)
        {
            end = new DateTime(year, month, 1);
            start = end.AddMonths(-(MonthWindow - 1));
        }
        else if (filter.Year is { } yearOnly)
        {
            start = new DateTime(yearOnly, 1, 1);
            end = yearOnly >= DateTime.Today.Year ? today : new DateTime(yearOnly, 12, 1);
        }
        else if (usages.Count == 0)
        {
            start = today.AddMonths(-(MonthWindow - 1));
            end = today;
        }
        else
        {
            start = MonthPeriod.Start(usages.Min(u => u.Date));
            end = MonthPeriod.Start(usages.Max(u => u.Date));

            // Будущих месяцев в данных быть не может, но график не должен их показывать.
            if (end > today) end = today;
            if (start > end) start = end;

            if (MonthsBetween(start, end) > MaxMonthWindow)
                start = end.AddMonths(-(MaxMonthWindow - 1));
        }

        if (start > end) start = end;

        var months = new List<DateTime>();
        for (var current = start; current <= end; current = current.AddMonths(1))
            months.Add(current);

        var actual = usages
            .GroupBy(u => MonthPeriod.Start(u.Date))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Cost));

        // Среднемесячный план — по фактическим затратам внутри окна.
        var total = months.Sum(m => actual.TryGetValue(m, out var cost) ? cost : 0m);
        var plannedMonthly = months.Count == 0 ? 0m : Math.Round(total / months.Count / 1.05m, 2);

        return months
            .Select(m => new MonthlyCostRow(
                m,
                plannedMonthly,
                actual.TryGetValue(m, out var cost) ? Math.Round(cost, 2) : 0m))
            .ToList();
    }

    /// <summary>Сколько месяцев между началом и концом окна включительно.</summary>
    private static int MonthsBetween(DateTime start, DateTime end) =>
        (end.Year - start.Year) * 12 + end.Month - start.Month + 1;
}
