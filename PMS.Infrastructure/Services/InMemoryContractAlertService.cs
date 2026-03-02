using PMS.Application.Contracts.Contract;
using PMS.Application.Models;
using PMS.Application.Models.Contract;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryContractAlertService : IContractAlertService
{
    public Task<ContractAlertSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var alerts = BuildAlerts(InMemoryProjectDataStore.Projects);
        var summary = new ContractAlertSummaryDto
        {
            ReminderCount = alerts.Count(x => x.AlertLevel == "提醒"),
            WarningCount = alerts.Count(x => x.AlertLevel == "警告"),
            CriticalCount = alerts.Count(x => x.AlertLevel == "严重"),
            Total = alerts.Count
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<ContractAlertItemDto>> QueryAlertsAsync(ContractAlertQuery query, CancellationToken cancellationToken = default)
    {
        var alerts = BuildAlerts(InMemoryProjectDataStore.Projects).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.AlertLevel))
        {
            alerts = alerts.Where(x => SmartTextMatcher.MatchExact(x.AlertLevel, query.AlertLevel));
        }

        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            alerts = alerts.Where(x => x.Province.Equals(query.Province, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.GroupName))
        {
            alerts = alerts.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));
        }

        if (!string.IsNullOrWhiteSpace(query.SalesName))
        {
            alerts = alerts.Where(x => SmartTextMatcher.Match(x.SalesName, query.SalesName));
        }

        var total = alerts.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = alerts
            .OrderByDescending(x => x.OverdueDays)
            .ThenBy(x => x.HospitalName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<ContractAlertItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    private static List<ContractAlertItemDto> BuildAlerts(IReadOnlyList<ProjectEntity> projects)
    {
        return projects
            .Where(x => x.OverdueDays > 0)
            .Select(x => new ContractAlertItemDto
            {
                ProjectId = x.Id,
                HospitalName = x.HospitalName,
                Province = x.Province,
                GroupName = x.GroupName,
                SalesName = x.SalesName,
                ContractStatus = x.ContractStatus,
                MaintenanceAmount = x.MaintenanceAmount,
                OverdueDays = x.OverdueDays,
                AlertLevel = GetAlertLevel(x.OverdueDays)
            })
            .ToList();
    }

    private static string GetAlertLevel(int overdueDays)
    {
        if (overdueDays > 90) return "严重";
        if (overdueDays >= 30) return "警告";
        return "提醒";
    }
}
