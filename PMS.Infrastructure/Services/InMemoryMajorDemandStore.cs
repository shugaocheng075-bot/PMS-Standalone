namespace PMS.Infrastructure.Services;

public class MajorDemandComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MajorDemandLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MajorDemandWorkflowItem
{
    public string RowId { get; set; } = string.Empty;
    public string Status { get; set; } = "待评估";
    public string Owner { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<MajorDemandComment> Comments { get; set; } = [];
    public List<MajorDemandLog> Logs { get; set; } = [];
}

public class MajorDemandSnapshot
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, string>> Rows { get; set; } = [];
    public string SourceFilePath { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public List<MajorDemandWorkflowItem> WorkflowItems { get; set; } = [];
}

public static class InMemoryMajorDemandStore
{
    private const string StateKey = "major_demands";
    private static readonly object SyncRoot = new();
    private static readonly MajorDemandSnapshot Snapshot = SqliteJsonStore.LoadOrSeed(StateKey, () => new MajorDemandSnapshot());

    public static MajorDemandSnapshot GetSnapshot()
    {
        lock (SyncRoot)
        {
            return new MajorDemandSnapshot
            {
                Columns = Snapshot.Columns.ToList(),
                Rows = Snapshot.Rows
                    .Select(CloneRow)
                    .ToList(),
                SourceFilePath = Snapshot.SourceFilePath,
                SheetName = Snapshot.SheetName,
                ImportedAt = Snapshot.ImportedAt,
                WorkflowItems = Snapshot.WorkflowItems
                    .Select(CloneWorkflowItem)
                    .ToList()
            };
        }
    }

    public static void ReplaceAll(
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string>> rows,
        string sourceFilePath,
        string sheetName)
    {
        lock (SyncRoot)
        {
            Snapshot.Columns = columns
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            var workflowById = Snapshot.WorkflowItems.ToDictionary(x => x.RowId, StringComparer.OrdinalIgnoreCase);
            var nextWorkflowItems = new List<MajorDemandWorkflowItem>();
            var nextRows = new List<Dictionary<string, string>>();

            var index = 1;
            foreach (var row in rows)
            {
                var cloned = CloneRow(row);
                var rowId = ResolveRowId(cloned, index++);
                cloned["_RowId"] = rowId;
                nextRows.Add(cloned);

                if (!workflowById.TryGetValue(rowId, out var existingWorkflow))
                {
                    existingWorkflow = new MajorDemandWorkflowItem
                    {
                        RowId = rowId,
                        Status = "待评估",
                        UpdatedAt = DateTime.UtcNow,
                        Logs =
                        [
                            new MajorDemandLog
                            {
                                Action = "导入",
                                Detail = "行数据已导入重大需求模块",
                                CreatedBy = "系统",
                                CreatedAt = DateTime.UtcNow
                            }
                        ]
                    };
                }

                nextWorkflowItems.Add(CloneWorkflowItem(existingWorkflow));
            }

            Snapshot.Rows = nextRows;
            Snapshot.WorkflowItems = nextWorkflowItems;

            Snapshot.SourceFilePath = sourceFilePath;
            Snapshot.SheetName = sheetName;
            Snapshot.ImportedAt = DateTime.UtcNow;

            Persist();
        }
    }

    public static bool BatchUpdateStatus(IReadOnlyCollection<string> rowIds, string status, string actor)
    {
        lock (SyncRoot)
        {
            var normalizedIds = NormalizeRowIds(rowIds);
            if (normalizedIds.Count == 0)
            {
                return false;
            }

            var changed = false;
            foreach (var item in Snapshot.WorkflowItems.Where(x => normalizedIds.Contains(x.RowId)))
            {
                if (string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var before = item.Status;
                item.Status = status.Trim();
                item.UpdatedAt = DateTime.UtcNow;
                item.Logs.Add(new MajorDemandLog
                {
                    Action = "状态变更",
                    Detail = $"{before} -> {item.Status}",
                    CreatedBy = actor,
                    CreatedAt = DateTime.UtcNow
                });
                changed = true;
            }

            if (changed)
            {
                Persist();
            }

            return changed;
        }
    }

    public static bool BatchAssignOwner(IReadOnlyCollection<string> rowIds, string owner, string actor)
    {
        lock (SyncRoot)
        {
            var normalizedIds = NormalizeRowIds(rowIds);
            if (normalizedIds.Count == 0)
            {
                return false;
            }

            var normalizedOwner = owner?.Trim() ?? string.Empty;
            var changed = false;
            foreach (var item in Snapshot.WorkflowItems.Where(x => normalizedIds.Contains(x.RowId)))
            {
                if (string.Equals(item.Owner, normalizedOwner, StringComparison.Ordinal))
                {
                    continue;
                }

                var before = item.Owner;
                item.Owner = normalizedOwner;
                item.UpdatedAt = DateTime.UtcNow;
                item.Logs.Add(new MajorDemandLog
                {
                    Action = "负责人变更",
                    Detail = $"{before} -> {item.Owner}",
                    CreatedBy = actor,
                    CreatedAt = DateTime.UtcNow
                });
                changed = true;
            }

            if (changed)
            {
                Persist();
            }

            return changed;
        }
    }

    public static bool BatchUpdateDueDate(IReadOnlyCollection<string> rowIds, string dueDate, string actor)
    {
        lock (SyncRoot)
        {
            var normalizedIds = NormalizeRowIds(rowIds);
            if (normalizedIds.Count == 0)
            {
                return false;
            }

            var normalizedDueDate = dueDate?.Trim() ?? string.Empty;
            var changed = false;
            foreach (var item in Snapshot.WorkflowItems.Where(x => normalizedIds.Contains(x.RowId)))
            {
                if (string.Equals(item.DueDate, normalizedDueDate, StringComparison.Ordinal))
                {
                    continue;
                }

                var before = item.DueDate;
                item.DueDate = normalizedDueDate;
                item.UpdatedAt = DateTime.UtcNow;
                item.Logs.Add(new MajorDemandLog
                {
                    Action = "截止日期变更",
                    Detail = $"{before} -> {item.DueDate}",
                    CreatedBy = actor,
                    CreatedAt = DateTime.UtcNow
                });
                changed = true;
            }

            if (changed)
            {
                Persist();
            }

            return changed;
        }
    }

    public static bool AddComment(string rowId, string content, string actor)
    {
        lock (SyncRoot)
        {
            var workflow = Snapshot.WorkflowItems.FirstOrDefault(x => string.Equals(x.RowId, rowId, StringComparison.OrdinalIgnoreCase));
            if (workflow is null)
            {
                return false;
            }

            var normalizedContent = content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedContent))
            {
                return false;
            }

            workflow.Comments.Add(new MajorDemandComment
            {
                Content = normalizedContent,
                CreatedBy = actor,
                CreatedAt = DateTime.UtcNow
            });

            workflow.Logs.Add(new MajorDemandLog
            {
                Action = "新增评论",
                Detail = normalizedContent.Length > 80 ? normalizedContent[..80] + "..." : normalizedContent,
                CreatedBy = actor,
                CreatedAt = DateTime.UtcNow
            });

            workflow.UpdatedAt = DateTime.UtcNow;
            Persist();
            return true;
        }
    }

    private static Dictionary<string, string> CloneRow(IReadOnlyDictionary<string, string> row)
    {
        return row.ToDictionary(
            kv => kv.Key,
            kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static MajorDemandWorkflowItem CloneWorkflowItem(MajorDemandWorkflowItem item)
    {
        return new MajorDemandWorkflowItem
        {
            RowId = item.RowId,
            Status = item.Status,
            Owner = item.Owner,
            DueDate = item.DueDate,
            UpdatedAt = item.UpdatedAt,
            Comments = item.Comments
                .Select(x => new MajorDemandComment
                {
                    Id = x.Id,
                    Content = x.Content,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                })
                .ToList(),
            Logs = item.Logs
                .Select(x => new MajorDemandLog
                {
                    Id = x.Id,
                    Action = x.Action,
                    Detail = x.Detail,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };
    }

    private static string ResolveRowId(IReadOnlyDictionary<string, string> row, int index)
    {
        if (row.TryGetValue("_RowId", out var rowId) && !string.IsNullOrWhiteSpace(rowId))
        {
            return rowId.Trim();
        }

        if (row.TryGetValue("行号", out var rowNo) && !string.IsNullOrWhiteSpace(rowNo))
        {
            return $"MD-{rowNo.Trim()}";
        }

        return $"MD-{index:D5}";
    }

    private static HashSet<string> NormalizeRowIds(IReadOnlyCollection<string> rowIds)
    {
        return rowIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, Snapshot);
    }
}