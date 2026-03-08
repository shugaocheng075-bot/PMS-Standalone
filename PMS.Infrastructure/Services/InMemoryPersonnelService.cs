using PMS.Application.Contracts.Personnel;
using PMS.Application.Models;
using PMS.Application.Models.Personnel;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace PMS.Infrastructure.Services;

public class InMemoryPersonnelService : IPersonnelService
{
    private const string TableName = "Personnel";
    private const string LegacyJsonKey = "personnel";
    private const string ExternalSyncStateKey = "personnel_external_sync_state";
    private const string ExternalPersonnelUrlEnv = "PMS_EXTERNAL_PERSONNEL_URL";
    private const string ExternalPersonnelFileEnv = "PMS_EXTERNAL_PERSONNEL_FILE";
    private const string ExternalPersonnelCookieEnv = "PMS_EXTERNAL_PERSONNEL_COOKIE";
    private const string ExternalPersonnelAuthorizationEnv = "PMS_EXTERNAL_PERSONNEL_AUTHORIZATION";
    private const string ExternalPersonnelRefererEnv = "PMS_EXTERNAL_PERSONNEL_REFERER";
    private static readonly char[] NameSeparators = [',', '，', '、', ';', '；', '/', '\\', '|', '+', '&'];
    private static readonly List<PersonnelItemDto> Personnel = SqliteTableStore.LoadAll<PersonnelItemDto>(TableName, LegacyJsonKey);
    private static readonly ExternalPersonnelSyncState SyncState = SqliteJsonStore.LoadOrSeed(ExternalSyncStateKey, () => new ExternalPersonnelSyncState());

    public Task<PersonnelSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var linkedPersonnel = Personnel.Select(HydrateWorkload).ToList();

        var summary = new PersonnelSummaryDto
        {
            Total = linkedPersonnel.Count,
            ServiceCount = linkedPersonnel.Count(x => x.RoleType == "服务"),
            ImplementationCount = linkedPersonnel.Count(x => x.RoleType == "实施"),
            OnsiteCount = linkedPersonnel.Count(x => x.IsOnsite)
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<PersonnelItemDto>> QueryAsync(PersonnelQuery query, CancellationToken cancellationToken = default)
    {
        var list = Personnel
            .Select(HydrateWorkload)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.Name, query.Name));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.Department, query.Department));
        }

        if (!string.IsNullOrWhiteSpace(query.GroupName))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));
        }

        if (!string.IsNullOrWhiteSpace(query.RoleType))
        {
            list = list.Where(x => SmartTextMatcher.MatchExact(x.RoleType, query.RoleType));
        }

        if (query.IsOnsite.HasValue)
        {
            list = list.Where(x => x.IsOnsite == query.IsOnsite.Value);
        }

        var total = list.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = list
            .OrderBy(x => x.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<PersonnelItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<PersonnelItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = Personnel.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item is null ? null : HydrateWorkload(item));
    }

    public async Task<PersonnelExternalSyncResultDto> SyncFromExternalAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var attemptedAt = DateTime.Now;

        if (!force && SyncState.LastSuccessAt.HasValue)
        {
            return new PersonnelExternalSyncResultDto
            {
                Success = true,
                Skipped = true,
                Reason = "已执行过一次全量同步，如需重跑请使用 force=true",
                ParsedCount = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                AttemptedAt = attemptedAt,
                LastSuccessAt = SyncState.LastSuccessAt,
                SourceUrl = "external-once"
            };
        }

        var source = ResolveExternalSource();
        SyncState.LastAttemptAt = attemptedAt;

        if (source is null)
        {
            SqliteJsonStore.Save(ExternalSyncStateKey, SyncState);
            return new PersonnelExternalSyncResultDto
            {
                Success = false,
                Skipped = false,
                Reason = $"未配置外部数据源，请设置环境变量 {ExternalPersonnelUrlEnv} 或 {ExternalPersonnelFileEnv}",
                ParsedCount = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                AttemptedAt = attemptedAt,
                LastSuccessAt = SyncState.LastSuccessAt,
                SourceUrl = string.Empty
            };
        }

        List<ExternalPersonnelRecord> externalRecords;
        try
        {
            externalRecords = await LoadExternalRecordsAsync(source, cancellationToken);
        }
        catch (Exception ex)
        {
            SqliteJsonStore.Save(ExternalSyncStateKey, SyncState);
            return new PersonnelExternalSyncResultDto
            {
                Success = false,
                Skipped = false,
                Reason = $"外部数据读取失败: {ex.Message}",
                ParsedCount = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                AttemptedAt = attemptedAt,
                LastSuccessAt = SyncState.LastSuccessAt,
                SourceUrl = source.Display
            };
        }

        if (externalRecords.Count == 0)
        {
            SqliteJsonStore.Save(ExternalSyncStateKey, SyncState);
            return new PersonnelExternalSyncResultDto
            {
                Success = false,
                Skipped = false,
                Reason = "外部数据为空，未执行导入",
                ParsedCount = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                AttemptedAt = attemptedAt,
                LastSuccessAt = SyncState.LastSuccessAt,
                SourceUrl = source.Display
            };
        }

        var result = UpsertExternalRecords(externalRecords);

        SyncState.LastSuccessAt = attemptedAt;
        SyncState.LastAddedCount = result.added;
        SqliteJsonStore.Save(ExternalSyncStateKey, SyncState);

        return new PersonnelExternalSyncResultDto
        {
            Success = true,
            Skipped = false,
            Reason = "一次性全量同步完成",
            ParsedCount = externalRecords.Count,
            AddedCount = result.added,
            UpdatedCount = result.updated,
            AttemptedAt = attemptedAt,
            LastSuccessAt = SyncState.LastSuccessAt,
            SourceUrl = source.Display
        };
    }

    public Task<PersonnelExternalSyncResultDto> ImportJsonAsync(string jsonData, bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var attemptedAt = DateTime.Now;

        List<ExternalPersonnelRecord> records;
        try
        {
            records = ParseJsonRecords(jsonData);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PersonnelExternalSyncResultDto
            {
                Success = false,
                Skipped = false,
                Reason = $"JSON 解析失败: {ex.Message}",
                ParsedCount = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                AttemptedAt = attemptedAt,
                SourceUrl = "import-json"
            });
        }

        if (records.Count == 0)
        {
            return Task.FromResult(new PersonnelExternalSyncResultDto
            {
                Success = false,
                Skipped = false,
                Reason = "JSON 数据为空，未执行导入",
                ParsedCount = 0,
                AddedCount = 0,
                UpdatedCount = 0,
                AttemptedAt = attemptedAt,
                SourceUrl = "import-json"
            });
        }

        if (clearExisting)
        {
            Personnel.Clear();
            Persist();
        }

        var result = UpsertExternalRecords(records);

        SyncState.LastSuccessAt = attemptedAt;
        SyncState.LastAddedCount = result.added;
        SqliteJsonStore.Save(ExternalSyncStateKey, SyncState);

        return Task.FromResult(new PersonnelExternalSyncResultDto
        {
            Success = true,
            Skipped = false,
            Reason = clearExisting ? "清空后重新导入完成" : "增量导入完成",
            ParsedCount = records.Count,
            AddedCount = result.added,
            UpdatedCount = result.updated,
            AttemptedAt = attemptedAt,
            LastSuccessAt = attemptedAt,
            SourceUrl = "import-json"
        });
    }

    public Task<PersonnelItemDto> CreateAsync(PersonnelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var nextId = Personnel.Count == 0 ? 1 : Personnel.Max(x => x.Id) + 1;
        var item = new PersonnelItemDto
        {
            Id = nextId,
            Name = dto.Name,
            Department = dto.Department,
            GroupName = dto.GroupName,
            RoleType = dto.RoleType,
            Phone = dto.Phone,
            IsOnsite = dto.IsOnsite,
            ProjectCount = dto.ProjectCount,
            OverdueCount = dto.OverdueCount,
            CreatedAt = DateTime.Now,
            SourceColumns = dto.SourceColumns is not null
                ? new Dictionary<string, string>(dto.SourceColumns, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        Personnel.Add(item);
        SqliteTableStore.Insert(TableName, item);
        return Task.FromResult(HydrateWorkload(item));
    }

    public Task<PersonnelItemDto?> UpdateAsync(int id, PersonnelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var current = Personnel.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult<PersonnelItemDto?>(null);
        }

        current.Name = dto.Name;
        current.Department = dto.Department;
        current.GroupName = dto.GroupName;
        current.RoleType = dto.RoleType;
        current.Phone = dto.Phone;
        current.IsOnsite = dto.IsOnsite;
        current.ProjectCount = dto.ProjectCount;
        current.OverdueCount = dto.OverdueCount;

        if (dto.SourceColumns is not null)
        {
            current.SourceColumns = new Dictionary<string, string>(dto.SourceColumns, StringComparer.OrdinalIgnoreCase);
        }

        SqliteTableStore.Update(TableName, current, current.Id);

        return Task.FromResult<PersonnelItemDto?>(HydrateWorkload(current));
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var current = Personnel.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult(false);
        }

        Personnel.Remove(current);
        SqliteTableStore.Delete(TableName, current.Id);
        return Task.FromResult(true);
    }

    private static void Persist()
    {
        SqliteTableStore.ReplaceAll(TableName, Personnel);
    }

    private static ExternalSource? ResolveExternalSource()
    {
        var url = Environment.GetEnvironmentVariable(ExternalPersonnelUrlEnv)?.Trim();
        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return new ExternalSource { Kind = "url", Value = uri.ToString(), Display = uri.ToString() };
        }

        var filePath = Environment.GetEnvironmentVariable(ExternalPersonnelFileEnv)?.Trim();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var resolved = ResolveFilePath(filePath);
        return new ExternalSource { Kind = "file", Value = resolved, Display = resolved };
    }

    private static string ResolveFilePath(string rawPath)
    {
        if (Path.IsPathRooted(rawPath))
        {
            return rawPath;
        }

        var appBase = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(appBase, rawPath));
        if (File.Exists(candidate))
        {
            return candidate;
        }

        return Path.GetFullPath(rawPath);
    }

    private static async Task<List<ExternalPersonnelRecord>> LoadExternalRecordsAsync(ExternalSource source, CancellationToken cancellationToken)
    {
        string raw;
        if (source.Kind == "url")
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var request = new HttpRequestMessage(HttpMethod.Get, source.Value);
            var cookie = Environment.GetEnvironmentVariable(ExternalPersonnelCookieEnv)?.Trim();
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
            }

            var authorization = Environment.GetEnvironmentVariable(ExternalPersonnelAuthorizationEnv)?.Trim();
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            }

            var referer = Environment.GetEnvironmentVariable(ExternalPersonnelRefererEnv)?.Trim();
            if (!string.IsNullOrWhiteSpace(referer))
            {
                request.Headers.Referrer = Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) ? refererUri : null;
            }

            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) PMS-Standalone/1.0");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            raw = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        else
        {
            if (!File.Exists(source.Value))
            {
                throw new FileNotFoundException("外部人员文件不存在", source.Value);
            }

            raw = await File.ReadAllTextAsync(source.Value, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var isJson = raw.TrimStart().StartsWith("[") || raw.TrimStart().StartsWith("{");
        if (isJson)
        {
            return ParseJsonRecords(raw);
        }

        var looksLikeHtml = raw.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("<table", StringComparison.OrdinalIgnoreCase);

        return looksLikeHtml ? ParseHtmlRecords(raw) : ParseCsvRecords(raw);
    }

    private static List<ExternalPersonnelRecord> ParseJsonRecords(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        JsonElement array;

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            array = doc.RootElement;
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            array = TryGetArrayProperty(doc.RootElement, "data")
                ?? TryGetArrayProperty(doc.RootElement, "items")
                ?? TryGetArrayProperty(doc.RootElement, "rows")
                ?? TryGetArrayProperty(doc.RootElement, "list")
                ?? throw new InvalidDataException("JSON 根对象中未找到可解析的数据数组(data/items/rows/list)");
        }
        else
        {
            throw new InvalidDataException("JSON 结构不支持，必须是数组或包含 data/items/rows/list 的对象");
        }

        var list = new List<ExternalPersonnelRecord>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(item, "name", "Name", "人员姓名", "姓名", "maintenancePersonName", "userName");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var roleType = GetString(item, "roleType", "RoleType", "角色类型", "岗位类型");
            var groupName = GetString(item, "groupName", "GroupName", "组别", "分组", "班组");
            if (string.IsNullOrWhiteSpace(roleType))
            {
                roleType = InferRoleType(groupName);
            }

            list.Add(new ExternalPersonnelRecord
            {
                Name = name.Trim(),
                Department = GetString(item, "department", "Department", "部门", "科室").Trim(),
                GroupName = (groupName ?? string.Empty).Trim(),
                RoleType = (roleType ?? string.Empty).Trim(),
                Phone = GetString(item, "phone", "mobile", "tel", "联系方式", "手机号").Trim(),
                IsOnsite = GetBool(item, "isOnsite", "onsite", "驻场", "是否驻场"),
                SourceColumns = item.EnumerateObject().ToDictionary(
                    x => x.Name,
                    x => JsonElementToString(x.Value),
                    StringComparer.OrdinalIgnoreCase)
            });
        }

        return list;
    }

    private static List<ExternalPersonnelRecord> ParseCsvRecords(string raw)
    {
        var lines = raw
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (lines.Count < 2)
        {
            return [];
        }

        var headerCells = ParseDelimitedLine(lines[0]);
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerCells.Count; i++)
        {
            var key = headerCells[i].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !headerMap.ContainsKey(key))
            {
                headerMap[key] = i;
            }
        }

        var list = new List<ExternalPersonnelRecord>();
        for (var lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            var cells = ParseDelimitedLine(lines[lineIndex]);
            var name = GetCellByHeader(cells, headerMap, "name", "Name", "人员姓名", "姓名").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var groupName = GetCellByHeader(cells, headerMap, "groupName", "GroupName", "组别", "分组", "班组");
            var roleType = GetCellByHeader(cells, headerMap, "roleType", "RoleType", "角色类型", "岗位类型");
            if (string.IsNullOrWhiteSpace(roleType))
            {
                roleType = InferRoleType(groupName);
            }

            list.Add(new ExternalPersonnelRecord
            {
                Name = name,
                Department = GetCellByHeader(cells, headerMap, "department", "Department", "部门", "科室").Trim(),
                GroupName = groupName.Trim(),
                RoleType = roleType.Trim(),
                Phone = GetCellByHeader(cells, headerMap, "phone", "mobile", "tel", "联系方式", "手机号").Trim(),
                IsOnsite = ParseBool(GetCellByHeader(cells, headerMap, "isOnsite", "onsite", "驻场", "是否驻场")),
                SourceColumns = headerCells
                    .Select((x, index) => new { Header = x?.Trim() ?? string.Empty, Index = index })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Header) && x.Index < cells.Count)
                    .ToDictionary(x => x.Header, x => cells[x.Index], StringComparer.OrdinalIgnoreCase)
            });
        }

        return list;
    }

    private static List<string> ParseDelimitedLine(string line)
    {
        var separator = line.Contains('\t') ? '\t' : ',';
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (ch == separator && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static List<ExternalPersonnelRecord> ParseHtmlRecords(string raw)
    {
        var match = Regex.Match(raw, "<table[^>]*>(?<content>[\\s\\S]*?)</table>", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return [];
        }

        var tableHtml = match.Groups["content"].Value;
        var rowMatches = Regex.Matches(tableHtml, "<tr[^>]*>(?<row>[\\s\\S]*?)</tr>", RegexOptions.IgnoreCase);
        if (rowMatches.Count == 0)
        {
            return [];
        }

        var headers = new List<string>();
        var bodyRows = new List<List<string>>();

        foreach (Match rowMatch in rowMatches)
        {
            var rowHtml = rowMatch.Groups["row"].Value;
            var headerCells = Regex.Matches(rowHtml, "<th[^>]*>(?<cell>[\\s\\S]*?)</th>", RegexOptions.IgnoreCase)
                .Select(x => CleanHtmlCell(x.Groups["cell"].Value))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (headerCells.Count > 0 && headers.Count == 0)
            {
                headers = headerCells;
                continue;
            }

            var dataCells = Regex.Matches(rowHtml, "<td[^>]*>(?<cell>[\\s\\S]*?)</td>", RegexOptions.IgnoreCase)
                .Select(x => CleanHtmlCell(x.Groups["cell"].Value))
                .ToList();

            if (dataCells.Count > 0)
            {
                bodyRows.Add(dataCells);
            }
        }

        if (headers.Count == 0 && bodyRows.Count > 0)
        {
            headers = Enumerable.Range(1, bodyRows.Max(x => x.Count)).Select(i => $"列{i}").ToList();
        }

        var records = new List<ExternalPersonnelRecord>();
        foreach (var row in bodyRows)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count && i < row.Count; i++)
            {
                map[headers[i]] = row[i];
            }

            var record = MapRecordFromSourceColumns(map);
            if (!string.IsNullOrWhiteSpace(record.Name))
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static ExternalPersonnelRecord MapRecordFromSourceColumns(Dictionary<string, string> sourceColumns)
    {
        var name = GetFromMap(sourceColumns, "name", "Name", "人员姓名", "姓名", "姓名/名称", "用户名称");
        if (string.IsNullOrWhiteSpace(name) && sourceColumns.Count > 0)
        {
            name = sourceColumns.First().Value;
        }

        var groupName = GetFromMap(sourceColumns, "groupName", "GroupName", "组别", "分组", "班组");
        var roleType = GetFromMap(sourceColumns, "roleType", "RoleType", "角色类型", "岗位类型");
        if (string.IsNullOrWhiteSpace(roleType))
        {
            roleType = InferRoleType(groupName);
        }

        return new ExternalPersonnelRecord
        {
            Name = name.Trim(),
            Department = GetFromMap(sourceColumns, "department", "Department", "部门", "科室").Trim(),
            GroupName = (groupName ?? string.Empty).Trim(),
            RoleType = (roleType ?? string.Empty).Trim(),
            Phone = GetFromMap(sourceColumns, "phone", "mobile", "tel", "联系方式", "手机号").Trim(),
            IsOnsite = ParseBool(GetFromMap(sourceColumns, "isOnsite", "onsite", "驻场", "是否驻场")),
            SourceColumns = sourceColumns
        };
    }

    private static string GetFromMap(IReadOnlyDictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var value))
            {
                return value ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string CleanHtmlCell(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var withoutTags = Regex.Replace(html, "<[^>]+>", " ", RegexOptions.IgnoreCase);
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded ?? string.Empty, "\\s+", " ").Trim();
    }

    private static JsonElement? TryGetArrayProperty(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Array)
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string GetString(JsonElement element, params string[] keys)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!keys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => string.Empty
            };
        }

        return string.Empty;
    }

    private static string JsonElementToString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }

    private static bool GetBool(JsonElement element, params string[] keys)
    {
        var value = GetString(element, keys);
        return ParseBool(value);
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "1" or "true" or "yes" or "y" or "是" or "驻场";
    }

    private static string GetCellByHeader(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerMap, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!headerMap.TryGetValue(key, out var index))
            {
                continue;
            }

            if (index >= 0 && index < cells.Count)
            {
                return cells[index];
            }
        }

        return string.Empty;
    }

    private static (int added, int updated) UpsertExternalRecords(IEnumerable<ExternalPersonnelRecord> records)
    {
        var changed = false;
        var added = 0;
        var updated = 0;
        var nextId = Personnel.Count == 0 ? 1 : Personnel.Max(x => x.Id) + 1;

        var existing = Personnel
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var name = record.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var roleType = string.IsNullOrWhiteSpace(record.RoleType) ? InferRoleType(record.GroupName) : record.RoleType;
            var department = string.IsNullOrWhiteSpace(record.Department)
                ? (roleType == "实施" ? "实施部" : "服务部")
                : record.Department;
            var groupName = string.IsNullOrWhiteSpace(record.GroupName) ? $"{name}组" : record.GroupName;

            if (existing.TryGetValue(name, out var current))
            {
                var hasChange = false;
                var incomingPhone = record.Phone ?? string.Empty;

                if (!string.Equals(current.Department, department, StringComparison.Ordinal))
                {
                    current.Department = department;
                    hasChange = true;
                }

                if (!string.Equals(current.GroupName, groupName, StringComparison.Ordinal))
                {
                    current.GroupName = groupName;
                    hasChange = true;
                }

                if (!string.Equals(current.RoleType, roleType, StringComparison.Ordinal))
                {
                    current.RoleType = roleType;
                    hasChange = true;
                }

                if (!string.Equals(current.Phone, incomingPhone, StringComparison.Ordinal))
                {
                    current.Phone = incomingPhone;
                    hasChange = true;
                }

                var incomingSourceColumns = record.SourceColumns ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!DictionaryEquals(current.SourceColumns, incomingSourceColumns))
                {
                    current.SourceColumns = new Dictionary<string, string>(incomingSourceColumns, StringComparer.OrdinalIgnoreCase);
                    hasChange = true;
                }

                if (current.IsOnsite != record.IsOnsite)
                {
                    current.IsOnsite = record.IsOnsite;
                    hasChange = true;
                }

                if (hasChange)
                {
                    changed = true;
                    updated++;
                }

                continue;
            }

            var item = new PersonnelItemDto
            {
                Id = nextId++,
                Name = name,
                Department = department,
                GroupName = groupName,
                RoleType = roleType,
                Phone = record.Phone ?? string.Empty,
                IsOnsite = record.IsOnsite,
                ProjectCount = 0,
                OverdueCount = 0,
                CreatedAt = DateTime.Now,
                SourceColumns = new Dictionary<string, string>(record.SourceColumns ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
            };

            Personnel.Add(item);
            existing[name] = item;
            changed = true;
            added++;
        }

        if (changed)
        {
            Persist();
        }

        return (added, updated);
    }

    private static bool DictionaryEquals(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
    {
        left ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        right ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var kv in left)
        {
            if (!right.TryGetValue(kv.Key, out var value))
            {
                return false;
            }

            if (!string.Equals(kv.Value ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static int ImportFromProjectStore()
    {
        var changed = false;
        var added = 0;
        var nextId = Personnel.Count == 0 ? 1 : Personnel.Max(x => x.Id) + 1;
        var existingNames = Personnel
            .Select(x => x.Name.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var project in InMemoryProjectDataStore.Projects)
        {
            if (string.IsNullOrWhiteSpace(project.MaintenancePersonName))
            {
                continue;
            }

            foreach (var rawName in project.MaintenancePersonName.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var name = rawName.Trim();
                if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
                {
                    continue;
                }

                if (existingNames.Contains(name))
                {
                    continue;
                }

                var roleType = InferRoleType(project.GroupName);
                var department = roleType == "实施" ? "实施部" : "服务部";

                Personnel.Add(new PersonnelItemDto
                {
                    Id = nextId++,
                    Name = name,
                    Department = department,
                    GroupName = string.IsNullOrWhiteSpace(project.GroupName) ? $"{name}组" : project.GroupName.Trim(),
                    RoleType = roleType,
                    Phone = string.Empty,
                    IsOnsite = false,
                    ProjectCount = 0,
                    OverdueCount = 0,
                    CreatedAt = DateTime.Now
                });

                existingNames.Add(name);
                changed = true;
                added++;
            }
        }

        if (changed)
        {
            Persist();
        }

        return added;
    }

    private static string InferRoleType(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return "服务";
        }

        return groupName.Contains("实施", StringComparison.OrdinalIgnoreCase) ? "实施" : "服务";
    }

    private static PersonnelItemDto HydrateWorkload(PersonnelItemDto item)
    {
        return new PersonnelItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Department = item.Department,
            GroupName = item.GroupName,
            RoleType = item.RoleType,
            Phone = item.Phone,
            IsOnsite = item.IsOnsite,
            ProjectCount = InMemoryProjectDataStore.CountProjectsForPersonnel(item.Name, item.GroupName),
            OverdueCount = InMemoryProjectDataStore.CountOverdueProjectsForPersonnel(item.Name, item.GroupName),
            CreatedAt = item.CreatedAt,
            SourceColumns = new Dictionary<string, string>(item.SourceColumns ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
        };
    }

    private static List<PersonnelItemDto> BuildSeedData()
    {
        return
        [
            new() { Id = 1, Name = "李贝", Department = "服务一部", GroupName = "李贝组", RoleType = "服务", Phone = "13800010001", IsOnsite = false, ProjectCount = 28, OverdueCount = 3, CreatedAt = DateTime.Now.AddMonths(-8), SourceColumns = new(StringComparer.OrdinalIgnoreCase) },
            new() { Id = 2, Name = "何道飞", Department = "服务一部", GroupName = "何道飞组", RoleType = "服务", Phone = "13800010002", IsOnsite = true, ProjectCount = 35, OverdueCount = 2, CreatedAt = DateTime.Now.AddMonths(-7), SourceColumns = new(StringComparer.OrdinalIgnoreCase) },
            new() { Id = 3, Name = "张茹", Department = "服务一部", GroupName = "张茹组", RoleType = "服务", Phone = "13800010003", IsOnsite = false, ProjectCount = 24, OverdueCount = 4, CreatedAt = DateTime.Now.AddMonths(-6), SourceColumns = new(StringComparer.OrdinalIgnoreCase) },
            new() { Id = 4, Name = "陈宇", Department = "实施二组", GroupName = "实施二组", RoleType = "实施", Phone = "13800010004", IsOnsite = false, ProjectCount = 19, OverdueCount = 1, CreatedAt = DateTime.Now.AddMonths(-5), SourceColumns = new(StringComparer.OrdinalIgnoreCase) },
            new() { Id = 5, Name = "侯海亮", Department = "实施一组", GroupName = "实施一组", RoleType = "实施", Phone = "13800010005", IsOnsite = true, ProjectCount = 22, OverdueCount = 2, CreatedAt = DateTime.Now.AddMonths(-4), SourceColumns = new(StringComparer.OrdinalIgnoreCase) },
            new() { Id = 6, Name = "舒高成", Department = "服务一部", GroupName = "舒高成组", RoleType = "服务", Phone = "13800010006", IsOnsite = false, ProjectCount = 17, OverdueCount = 1, CreatedAt = DateTime.Now.AddMonths(-3), SourceColumns = new(StringComparer.OrdinalIgnoreCase) }
        ];
    }

    private sealed class ExternalPersonnelSyncState
    {
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? LastSuccessAt { get; set; }
        public int LastAddedCount { get; set; }
    }

    private sealed class ExternalSource
    {
        public string Kind { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }

    private sealed class ExternalPersonnelRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsOnsite { get; set; }
        public Dictionary<string, string> SourceColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
