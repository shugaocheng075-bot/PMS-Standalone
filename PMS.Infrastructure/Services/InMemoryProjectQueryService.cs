using PMS.Application.Contracts;
using PMS.Application.Models;
using PMS.Domain.Entities;
using System.Globalization;

namespace PMS.Infrastructure.Services;

public class InMemoryProjectQueryService : IProjectQueryService
{
    public Task<PagedResult<ProjectEntity>> QueryAsync(ProjectQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<ProjectEntity> filtered = InMemoryProjectDataStore.Projects;

        // 数据范围过滤
        if (query.AccessiblePersonnelNames is { Count: > 0 })
        {
            var nameSet = new HashSet<string>(query.AccessiblePersonnelNames, StringComparer.Ordinal);
            filtered = filtered.Where(x => nameSet.Contains(x.MaintenancePersonName));
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.HospitalName, query.HospitalName));
        }

        if (!string.IsNullOrWhiteSpace(query.ProductName))
        {
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ProductName, query.ProductName));
        }

        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            filtered = filtered.Where(x => x.Province.Equals(query.Province, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.GroupName))
        {
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));
        }

        if (!string.IsNullOrWhiteSpace(query.SalesName))
        {
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.SalesName, query.SalesName));
        }

        if (!string.IsNullOrWhiteSpace(query.MaintenancePersonName))
        {
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.MaintenancePersonName, query.MaintenancePersonName));
        }

        if (TryParseDate(query.AfterSalesEndDateFrom, out var endDateFrom))
        {
            filtered = filtered.Where(x => TryParseDate(x.AfterSalesEndDate, out var projectEndDate) && projectEndDate >= endDateFrom);
        }

        if (TryParseDate(query.AfterSalesEndDateTo, out var endDateTo))
        {
            filtered = filtered.Where(x => TryParseDate(x.AfterSalesEndDate, out var projectEndDate) && projectEndDate <= endDateTo);
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalLevel))
        {
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.HospitalLevel, query.HospitalLevel));
        }

        if (!string.IsNullOrWhiteSpace(query.ContractStatus))
        {
            var expectedStatus = NormalizeContractStatus(query.ContractStatus, 0);
            filtered = filtered.Where(x =>
                SmartTextMatcher.MatchExact(NormalizeContractStatus(x.ContractStatus, x.OverdueDays), expectedStatus));
        }

        if (!string.IsNullOrWhiteSpace(query.ContractValidityStatus))
        {
            filtered = filtered.Where(x =>
                SmartTextMatcher.MatchExact(GetContractValidityStatus(x), query.ContractValidityStatus));
        }

        var total = filtered.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = filtered
            .OrderByDescending(x => x.OverdueDays)
            .ThenBy(x => x.HospitalName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x =>
            {
                x.ContractValidityStatus = GetContractValidityStatus(x);
                return x;
            })
            .ToList();

        return Task.FromResult(new PagedResult<ProjectEntity>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    private static bool TryParseDate(string? raw, out DateTime date)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            date = default;
            return false;
        }

        var text = raw.Trim();
        var formats = new[] { "yyyy-MM-dd", "yyyy/M/d", "yyyy/MM/dd", "yyyy-M-d" };
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string NormalizeContractStatus(string? status, int overdueDays)
    {
        var value = (status ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "未知", StringComparison.OrdinalIgnoreCase))
        {
            return overdueDays > 0 ? "超期未签署" : "未知";
        }

        if (value.Contains("停止", StringComparison.OrdinalIgnoreCase))
        {
            return "停止维护";
        }

        if (value.Contains("免费", StringComparison.OrdinalIgnoreCase))
        {
            return "免费维护期";
        }

        if (value.Contains("超期", StringComparison.OrdinalIgnoreCase)
            || value.Contains("到期", StringComparison.OrdinalIgnoreCase)
            || value.Contains("停保", StringComparison.OrdinalIgnoreCase)
            || value.Contains("脱保", StringComparison.OrdinalIgnoreCase))
        {
            return "超期未签署";
        }

        if (value.Contains("签署", StringComparison.OrdinalIgnoreCase)
            || value.Contains("签订", StringComparison.OrdinalIgnoreCase))
        {
            return "合同已签署";
        }

        return value;
    }

    private static string GetContractValidityStatus(ProjectEntity item)
    {
        var displayOverdueDays = GetDisplayOverdueDays(item);
        if (displayOverdueDays > 0)
        {
            return "已过期";
        }

        var dayDiff = GetDayDiffFromToday(item.AfterSalesEndDate);
        if (dayDiff is null)
        {
            return "有效";
        }

        if (dayDiff <= 30)
        {
            return "待续签";
        }

        return "有效";
    }

    private static int GetDisplayOverdueDays(ProjectEntity item)
    {
        if (item.OverdueDays > 0)
        {
            return item.OverdueDays;
        }

        var dayDiff = GetDayDiffFromToday(item.AfterSalesEndDate);
        if (dayDiff is null || dayDiff >= 0)
        {
            return 0;
        }

        return Math.Abs(dayDiff.Value);
    }

    private static int? GetDayDiffFromToday(string dateText)
    {
        if (!TryParseDate(dateText, out var target))
        {
            return null;
        }

        var today = DateTime.Today;
        return (int)(target.Date - today).TotalDays;
    }
}
