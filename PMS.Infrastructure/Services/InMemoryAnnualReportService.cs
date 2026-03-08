using System.Globalization;
using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.Infrastructure.Services;

public class InMemoryAnnualReportService : IAnnualReportService
{
    private static readonly object SyncRoot = new();
    private const string OverridesTable = "AnnualReportOverrides";
    private const string OverridesLegacyKey = "annual_report_overrides";
    private static readonly List<AnnualReportItemDto> Overrides =
        SqliteTableStore.LoadAll<AnnualReportItemDto>(OverridesTable, OverridesLegacyKey);

    public Task<AnnualReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var seed = BuildSeed();
        var thisYear = DateTime.Today.Year;
        var currentMonth = DateTime.Today.ToString("yyyy-MM");
        var summary = new AnnualReportSummaryDto
        {
            NotStartedCount = seed.Count(x => x.Status == "未开始"),
            WritingCount = seed.Count(x => x.Status == "编写中"),
            SubmittedCount = seed.Count(x => x.Status == "已提交"),
            CompletedCount = seed.Count(x => x.Status == "已完成"),
            ThisYearCount = seed.Count(x => x.ReportYear == thisYear),
            DueThisMonthCount = seed.Count(x =>
                string.Equals(x.DueMonth, currentMonth, StringComparison.OrdinalIgnoreCase)),
            OverdueCount = seed.Count(x =>
                string.Compare(x.DueMonth, currentMonth, StringComparison.Ordinal) <= 0
                && x.Status != "已完成"),
            Total = seed.Count
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<AnnualReportItemDto>> QueryAsync(AnnualReportQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<AnnualReportItemDto> filtered = BuildSeed();

        if (!string.IsNullOrWhiteSpace(query.Status))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Status, query.Status));

        if (query.ReportYear.HasValue)
            filtered = filtered.Where(x => x.ReportYear == query.ReportYear.Value);

        if (!string.IsNullOrWhiteSpace(query.DueMonth))
            filtered = filtered.Where(x =>
                string.Equals(x.DueMonth, query.DueMonth.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.GroupName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));

        if (!string.IsNullOrWhiteSpace(query.ServicePerson))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ServicePerson, query.ServicePerson));

        var list = filtered.ToList();
        var total = list.Count;
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = list
            .OrderBy(x => x.DueMonth)
            .ThenByDescending(x => x.ReportYear)
            .ThenBy(x => x.HospitalName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<AnnualReportItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<AnnualReportItemDto?> UpdateAsync(long id, AnnualReportUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var current = BuildSeed().FirstOrDefault(x => x.Id == id);
            if (current is null)
            {
                return Task.FromResult<AnnualReportItemDto?>(null);
            }

            if (dto.OpportunityNumber is not null) current.OpportunityNumber = dto.OpportunityNumber.Trim();
            if (dto.HospitalName is not null) current.HospitalName = dto.HospitalName.Trim();
            if (dto.ProductName is not null) current.ProductName = dto.ProductName.Trim();
            if (dto.Province is not null) current.Province = dto.Province.Trim();
            if (dto.GroupName is not null) current.GroupName = dto.GroupName.Trim();
            if (dto.ServicePerson is not null) current.ServicePerson = dto.ServicePerson.Trim();
            if (dto.ImplementationStatus is not null) current.ImplementationStatus = dto.ImplementationStatus.Trim();
            if (dto.MaintenanceStartDate is not null) current.MaintenanceStartDate = dto.MaintenanceStartDate.Trim();
            if (dto.MaintenanceEndDate is not null)
            {
                current.MaintenanceEndDate = dto.MaintenanceEndDate.Trim();
                current.DueMonth = ComputeDueMonth(dto.MaintenanceEndDate.Trim());
            }
            if (dto.ReportYear.HasValue) current.ReportYear = dto.ReportYear.Value;
            if (dto.Status is not null) current.Status = dto.Status.Trim();
            current.SubmitDate = dto.SubmitDate;
            if (dto.Remarks is not null) current.Remarks = dto.Remarks.Trim();

            var existingIndex = Overrides.FindIndex(x => x.Id == id);
            if (existingIndex >= 0)
            {
                Overrides[existingIndex] = current;
            }
            else
            {
                Overrides.Add(current);
            }

            PersistOverrides();

            // 联动同步到项目台账
            SyncBackToProject(current);

            return Task.FromResult<AnnualReportItemDto?>(current);
        }
    }

    /// <summary>
    /// 编辑年报后同步关键字段回项目台账
    /// </summary>
    private static void SyncBackToProject(AnnualReportItemDto item)
    {
        InMemoryProjectDataStore.UpdateSingleProject(item.Id, project =>
        {
            if (!string.IsNullOrEmpty(item.GroupName))
                project.GroupName = item.GroupName;
            if (!string.IsNullOrEmpty(item.ServicePerson))
                project.MaintenancePersonName = item.ServicePerson;
            if (!string.IsNullOrEmpty(item.MaintenanceStartDate))
                project.AfterSalesStartDate = item.MaintenanceStartDate;
            if (!string.IsNullOrEmpty(item.MaintenanceEndDate))
                project.AfterSalesEndDate = item.MaintenanceEndDate;
            if (!string.IsNullOrEmpty(item.ImplementationStatus))
                project.ImplementationStatus = item.ImplementationStatus;
        });
    }

    private static void PersistOverrides()
    {
        SqliteTableStore.ReplaceAll(OverridesTable, Overrides);
    }

    /// <summary>
    /// 根据维护结束日期计算年报到期月份（提前一个月）。
    /// 例如 AfterSalesEndDate = "2026-01-19" → DueMonth = "2025-12"
    /// </summary>
    private static string ComputeDueMonth(string endDate)
    {
        if (string.IsNullOrWhiteSpace(endDate)) return string.Empty;
        if (DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt.AddMonths(-1).ToString("yyyy-MM");
        }
        // 尝试提取前 7 位 "YYYY-MM"
        if (endDate.Length >= 7) return endDate[..7];
        return string.Empty;
    }

    /// <summary>
    /// 根据维护周期计算报告年度。
    /// 维护周期 2025-02-01 ~ 2026-01-31，服务覆盖的主要年度 = 起始年份 = 2025
    /// </summary>
    private static int ComputeReportYear(string startDate, string endDate)
    {
        if (DateTime.TryParse(startDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            return start.Year;
        }
        if (DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return end.Year - 1;
        }
        return DateTime.Today.Year;
    }

    private static List<AnnualReportItemDto> BuildSeed()
    {
        var seed = InMemoryProjectDataStore.Projects
            .Where(p => !string.IsNullOrWhiteSpace(p.AfterSalesEndDate))
            .OrderBy(x => x.Id)
            .Select(project =>
            {
                var dueMonth = ComputeDueMonth(project.AfterSalesEndDate);
                var reportYear = ComputeReportYear(project.AfterSalesStartDate, project.AfterSalesEndDate);
                var servicePerson = string.IsNullOrWhiteSpace(project.MaintenancePersonName)
                    ? "未分配"
                    : project.MaintenancePersonName.Trim();

                // 到期月份在 2025-12 之前的年报默认视为已完成（当前活跃批次为 2025-12）
                var defaultStatus = string.Compare(dueMonth, "2025-12", StringComparison.Ordinal) < 0
                    ? "已完成"
                    : "未开始";

                return new AnnualReportItemDto
                {
                    Id = project.Id,
                    OpportunityNumber = project.OpportunityNumber,
                    HospitalName = project.HospitalName,
                    ProductName = project.ProductName,
                    Province = project.Province,
                    GroupName = project.GroupName,
                    ServicePerson = servicePerson,
                    ImplementationStatus = project.ImplementationStatus,
                    MaintenanceStartDate = project.AfterSalesStartDate,
                    MaintenanceEndDate = project.AfterSalesEndDate,
                    DueMonth = dueMonth,
                    ReportYear = reportYear,
                    Status = defaultStatus,
                };
            })
            .ToList();

        if (Overrides.Count == 0)
        {
            return seed;
        }

        foreach (var item in seed)
        {
            var ov = Overrides.FirstOrDefault(x => x.Id == item.Id);
            if (ov is null) continue;

            if (!string.IsNullOrEmpty(ov.OpportunityNumber)) item.OpportunityNumber = ov.OpportunityNumber;
            if (!string.IsNullOrEmpty(ov.HospitalName)) item.HospitalName = ov.HospitalName;
            if (!string.IsNullOrEmpty(ov.ProductName)) item.ProductName = ov.ProductName;
            if (!string.IsNullOrEmpty(ov.Province)) item.Province = ov.Province;
            if (!string.IsNullOrEmpty(ov.GroupName)) item.GroupName = ov.GroupName;
            if (!string.IsNullOrEmpty(ov.ServicePerson)) item.ServicePerson = ov.ServicePerson;
            if (!string.IsNullOrEmpty(ov.ImplementationStatus)) item.ImplementationStatus = ov.ImplementationStatus;
            if (!string.IsNullOrEmpty(ov.MaintenanceStartDate)) item.MaintenanceStartDate = ov.MaintenanceStartDate;
            if (!string.IsNullOrEmpty(ov.MaintenanceEndDate))
            {
                item.MaintenanceEndDate = ov.MaintenanceEndDate;
                item.DueMonth = ComputeDueMonth(ov.MaintenanceEndDate);
            }
            if (ov.ReportYear > 0) item.ReportYear = ov.ReportYear;
            if (!string.IsNullOrEmpty(ov.Status)) item.Status = ov.Status;
            item.SubmitDate = ov.SubmitDate;
            if (!string.IsNullOrEmpty(ov.Remarks)) item.Remarks = ov.Remarks;
        }

        return seed;
    }
}
