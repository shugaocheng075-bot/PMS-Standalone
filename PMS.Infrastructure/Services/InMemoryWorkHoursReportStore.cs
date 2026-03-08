using PMS.Application.Models.WorkHours;

namespace PMS.Infrastructure.Services;

/// <summary>
/// 工时报表数据存储 — 按月份缓存工时报表快照
/// </summary>
public static class InMemoryWorkHoursReportStore
{
    private const string StateKey = "work_hours_report_monthly";
    private static readonly object SyncRoot = new();
    private static WorkHoursReportMonthlyState _state = LoadState();

    private sealed class WorkHoursReportMonthlyState
    {
        public List<WorkHoursReportMonthBucket> Buckets { get; set; } = new();
    }

    private sealed class WorkHoursReportMonthBucket
    {
        public string ReportMonth { get; set; } = string.Empty;
        public List<WorkHoursReportRowDto> Rows { get; set; } = new();
    }

    private static WorkHoursReportMonthlyState LoadState()
    {
        var state = SqliteJsonStore.LoadOrSeed(StateKey, () => new WorkHoursReportMonthlyState());
        var changed = false;

        foreach (var bucket in state.Buckets)
        {
            changed |= NormalizeRows(bucket.Rows);
        }

        if (changed)
        {
            SqliteJsonStore.Save(StateKey, state);
        }

        return state;
    }

    private static bool NormalizeRows(List<WorkHoursReportRowDto> rows)
    {
        var changed = false;
        long nextId = 1;

        foreach (var row in rows)
        {
            if (row.Id >= nextId)
            {
                nextId = row.Id + 1;
            }
        }

        foreach (var row in rows)
        {
            if (row.Id <= 0)
            {
                row.Id = nextId++;
                changed = true;
            }

            var normalizedManDays = Math.Round(row.WorkHoursManDays, 0, MidpointRounding.AwayFromZero);
            if (row.WorkHoursManDays != normalizedManDays)
            {
                row.WorkHoursManDays = normalizedManDays;
                changed = true;
            }
        }

        return changed;
    }

    private static WorkHoursReportRowDto Clone(WorkHoursReportRowDto r)
    {
        return new WorkHoursReportRowDto
        {
            Id = r.Id,
            OpportunityNumber = r.OpportunityNumber,
            HospitalName = r.HospitalName,
            ProductName = r.ProductName,
            ImplementationStatus = r.ImplementationStatus,
            WorkHoursManDays = Math.Round(r.WorkHoursManDays, 0, MidpointRounding.AwayFromZero),
            PersonnelCount = r.PersonnelCount,
            Personnel1 = r.Personnel1,
            Personnel2 = r.Personnel2,
            Personnel3 = r.Personnel3,
            Personnel4 = r.Personnel4,
            Personnel5 = r.Personnel5,
            MaintenanceStartDate = r.MaintenanceStartDate,
            MaintenanceEndDate = r.MaintenanceEndDate,
            AfterSalesProjectType = r.AfterSalesProjectType,
            Remarks = r.Remarks
        };
    }

    private static WorkHoursReportMonthBucket GetOrCreateBucket(string reportMonth)
    {
        var normalized = reportMonth.Trim();
        var bucket = _state.Buckets.FirstOrDefault(x => string.Equals(x.ReportMonth, normalized, StringComparison.OrdinalIgnoreCase));
        if (bucket is not null)
        {
            return bucket;
        }

        bucket = new WorkHoursReportMonthBucket
        {
            ReportMonth = normalized,
            Rows = new List<WorkHoursReportRowDto>()
        };
        _state.Buckets.Add(bucket);
        return bucket;
    }

    private static WorkHoursReportMonthBucket? FindBucket(string reportMonth)
    {
        var normalized = reportMonth.Trim();
        return _state.Buckets.FirstOrDefault(x => string.Equals(x.ReportMonth, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<WorkHoursReportRowDto> GetMonthRows(string reportMonth)
    {
        lock (SyncRoot)
        {
            var bucket = FindBucket(reportMonth);
            if (bucket is null)
            {
                return [];
            }

            return bucket.Rows.Select(Clone).ToList();
        }
    }

    public static IReadOnlyList<WorkHoursReportRowDto> GetLatestMonthRows()
    {
        lock (SyncRoot)
        {
            if (_state.Buckets.Count == 0)
            {
                return [];
            }

            var latest = _state.Buckets
                .Where(x => x.Rows.Count > 0)
                .OrderByDescending(x => x.ReportMonth, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (latest is null)
            {
                return [];
            }

            return latest.Rows.Select(Clone).ToList();
        }
    }

    public static IReadOnlyList<WorkHoursReportRowDto> GetOrCreateMonthRows(
        string reportMonth,
        Func<List<WorkHoursReportRowDto>> factory)
    {
        lock (SyncRoot)
        {
            var bucket = GetOrCreateBucket(reportMonth);
            if (bucket.Rows.Count == 0)
            {
                var rows = factory();
                NormalizeRows(rows);
                bucket.Rows = rows;
                SqliteJsonStore.Save(StateKey, _state);
            }

            return bucket.Rows.Select(Clone).ToList();
        }
    }

    public static void ReplaceMonthRows(string reportMonth, List<WorkHoursReportRowDto> rows)
    {
        lock (SyncRoot)
        {
            NormalizeRows(rows);
            var bucket = GetOrCreateBucket(reportMonth);
            bucket.Rows = rows;
            SqliteJsonStore.Save(StateKey, _state);
        }
    }

    public static WorkHoursReportRowDto? Update(string reportMonth, long id, Action<WorkHoursReportRowDto> updater)
    {
        lock (SyncRoot)
        {
            var bucket = GetOrCreateBucket(reportMonth);
            var row = bucket.Rows.FirstOrDefault(r => r.Id == id);
            if (row is null)
            {
                return null;
            }

            updater(row);
            NormalizeRows(bucket.Rows);
            SqliteJsonStore.Save(StateKey, _state);
            return Clone(row);
        }
    }
}
