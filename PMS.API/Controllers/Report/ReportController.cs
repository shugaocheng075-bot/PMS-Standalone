using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Contracts.MonthlyReport;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models.Access;
using PMS.Application.Models.Handover;
using PMS.Application.Models.Inspection;
using PMS.Application.Models.MonthlyReport;
using PMS.Application.Models.Personnel;
using PMS.Application.Models.RepairRecord;
using PMS.Application.Models.WorkHours;
using PMS.Infrastructure.Services;
using System.Text.Json;
using System.Text;

namespace PMS.API.Controllers.Report;

[ApiController]
[Route("api/reports")]
public class ReportController(
    IMonthlyReportService monthlyReportService,
    IAccessControlService accessControlService,
    IPersonnelService personnelService,
    IHandoverService handoverService,
    IInspectionService inspectionService,
    IRepairRecordService repairRecordService) : ControllerBase
{
    private static readonly char[] PersonnelSeparators = [',', '，', '、', ';', '；', '/', '\\', '|', '+', '&', '\n', '\r', '\t'];

    /// <summary>
    /// 工时报表 — 从项目台账数据生成，对应工时 Excel 格式
    /// </summary>
    [HttpGet("workhours")]
    public IActionResult GetWorkHoursReport(
        [FromQuery] string? groupName,
        [FromQuery] string? implementationStatus)
    {
        var rows = BuildWorkHoursRows(groupName, implementationStatus).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            total = rows.Count,
            rows
        }));
    }

    /// <summary>
    /// 导出工时报表（CSV）
    /// </summary>
    [HttpGet("workhours/export")]
    public IActionResult ExportWorkHoursReport(
        [FromQuery] string? groupName,
        [FromQuery] string? implementationStatus)
    {
        var rows = BuildWorkHoursRows(groupName, implementationStatus).ToList();
        var csv = BuildWorkHoursCsv(rows);
        var fileName = $"workhours_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 月报自动生成 — 从各模块聚合指定月份数据生成月报
    /// </summary>
    [HttpPost("monthly/generate")]
    public async Task<IActionResult> GenerateMonthlyReport(
        [FromBody] MonthlyReportGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReportMonth(request.ReportMonth, out var monthStart))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "reportMonth 格式错误，应为 yyyy-MM",
                Data = null
            });
        }

        var computed = await BuildMonthlyReportSourceAsync(
            request.ReportMonth,
            request.GroupName,
            monthStart,
            request.TeamDataSource,
            cancellationToken);

        var created = await monthlyReportService.CreateAsync(
            NormalizeSubmittedBy(request.SubmittedBy, request.GroupName),
            computed.UpsertDto,
            cancellationToken);

        return Ok(ApiResponse<MonthlyReportItemDto>.Success(created));
    }

    /// <summary>
    /// 月报数据来源预览 — 按月份与组别预览人员、项目、驻场扣减和主管归属。
    /// </summary>
    [HttpGet("monthly/source-preview")]
    public async Task<IActionResult> GetMonthlyReportSourcePreview(
        [FromQuery] string reportMonth,
        [FromQuery] string groupName,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReportMonth(reportMonth, out var monthStart))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "reportMonth 格式错误，应为 yyyy-MM",
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "groupName 不能为空",
                Data = null
            });
        }

        var computed = await BuildMonthlyReportSourceAsync(
            reportMonth,
            groupName,
            monthStart,
            null,
            cancellationToken);

        return Ok(ApiResponse<MonthlyReportSourcePreviewDto>.Success(computed.PreviewDto));
    }

    /// <summary>
    /// 导出月报（CSV）。若当月当组无数据且提供 submittedBy，则自动生成后导出。
    /// </summary>
    [HttpGet("monthly/export")]
    public async Task<IActionResult> ExportMonthlyReport(
        [FromQuery] string reportMonth,
        [FromQuery] string groupName,
        [FromQuery] string? submittedBy,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReportMonth(reportMonth, out var monthStart))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "reportMonth 格式错误，应为 yyyy-MM",
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "groupName 不能为空",
                Data = null
            });
        }

        var reportRows = await monthlyReportService.QueryAsync(new MonthlyReportQuery
        {
            ReportMonth = reportMonth,
            GroupName = groupName,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var items = reportRows.Items.ToList();
        if (items.Count == 0)
        {
            var computed = await BuildMonthlyReportSourceAsync(
                reportMonth,
                groupName,
                monthStart,
                null,
                cancellationToken);
            var created = await monthlyReportService.CreateAsync(
                NormalizeSubmittedBy(submittedBy, groupName),
                computed.UpsertDto,
                cancellationToken);
            items.Add(created);
        }

        var csv = BuildMonthlyReportCsv(items);
        var fileName = $"monthly_report_{reportMonth}_{groupName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    private static List<WorkHoursReportRowDto> BuildWorkHoursRows(string? groupName, string? implementationStatus)
    {
        var projects = InMemoryProjectDataStore.Projects;

        IEnumerable<Domain.Entities.ProjectEntity> filtered = projects;

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var normalizedGroup = groupName.Trim();
            filtered = filtered.Where(x => x.GroupName.Contains(normalizedGroup, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(implementationStatus))
        {
            var normalizedStatus = implementationStatus.Trim();
            filtered = filtered.Where(x => string.Equals((x.ImplementationStatus ?? string.Empty).Trim(), normalizedStatus, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.Select(p => new WorkHoursReportRowDto
        {
            OpportunityNumber = p.OpportunityNumber,
            HospitalName = p.HospitalName,
            ProductName = p.ProductName,
            Province = p.Province,
            GroupName = p.GroupName,
            SalesName = p.SalesName,
            MaintenancePersonName = p.MaintenancePersonName,
            ImplementationStatus = p.ImplementationStatus,
            WorkHoursManDays = p.WorkHoursManDays,
            PersonnelCount = p.PersonnelCount,
            Personnel1 = p.Personnel1,
            Personnel2 = p.Personnel2,
            Personnel3 = p.Personnel3,
            Personnel4 = p.Personnel4,
            Personnel5 = p.Personnel5,
            MaintenanceStartDate = p.AfterSalesStartDate,
            MaintenanceEndDate = p.AfterSalesEndDate,
            AfterSalesProjectType = p.AfterSalesProjectType,
            Remarks = p.Remarks
        }).ToList();
    }

    private async Task<MonthlyReportSourceComputationResult> BuildMonthlyReportSourceAsync(
        string reportMonth,
        string groupName,
        DateTime monthStart,
        MonthlyReportTeamDataSourceInput? sourceInput,
        CancellationToken cancellationToken)
    {
        var monthEnd = monthStart.AddMonths(1);
        var nextMonthStart = monthEnd;
        var nextMonthEnd = nextMonthStart.AddMonths(1);

        var normalizedGroup = (groupName ?? string.Empty).Trim();
        var projectRows = InMemoryProjectDataStore.Projects;
        var groupProjects = projectRows
            .Where(p => !string.IsNullOrWhiteSpace(normalizedGroup)
                ? string.Equals((p.GroupName ?? string.Empty).Trim(), normalizedGroup, StringComparison.OrdinalIgnoreCase)
                : true)
            .ToList();

        var groupProjectSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in groupProjects)
        {
            var key = $"{project.HospitalName}||{project.ProductName}";
            groupProjectSet.Add(key);
        }

        var personnelResult = await personnelService.QueryAsync(new PersonnelQuery
        {
            GroupName = groupName,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var accessResult = await accessControlService.QueryUsersAsync(new PersonnelAccessQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);
        var accessById = accessResult.Items.ToDictionary(x => x.PersonnelId);
        var supervisorName = ResolveGroupSupervisorName(personnelResult.Items, accessById);

        var personnelScopes = personnelResult.Items
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(person =>
            {
                accessById.TryGetValue(person.Id, out var accessItem);
                var assignedProjects = GetAssignedProjectsForPersonnel(groupProjects, person.Name);
                var hospitalNames = assignedProjects
                    .Select(x => x.HospitalName?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var productNames = assignedProjects
                    .Select(x => FormatProductScope(x.HospitalName, x.ProductName))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var resolvedSupervisorName = ResolvePersonnelSupervisorName(person, accessItem, supervisorName);

                return new
                {
                    Person = person,
                    PreviewItem = new MonthlyReportSourcePersonnelItemDto
                    {
                        PersonnelId = person.Id,
                        Name = person.Name,
                        Department = person.Department,
                        GroupName = person.GroupName,
                        RoleType = person.RoleType,
                        SystemRole = accessItem?.SystemRole ?? string.Empty,
                        ServiceMode = person.IsOnsite ? "驻场" : "远程/标服",
                        IsOnsite = person.IsOnsite,
                        SupervisorName = resolvedSupervisorName,
                        ResponsibilityHospitalCount = hospitalNames.Count,
                        ResponsibilityProductCount = productNames.Count,
                        HospitalNames = hospitalNames,
                        ProductNames = productNames,
                        MatchingStatus = hospitalNames.Count > 0 || productNames.Count > 0 ? "已匹配" : "未匹配项目"
                    }
                };
            })
            .ToList();

        var teamTotal = personnelResult.Items.Count;
        var onsitePersonnelCount = personnelResult.Items.Count(x => x.IsOnsite);
        var onsiteList = personnelResult.Items
            .Where(x => x.IsOnsite)
            .GroupBy(x => ResolveRegionLabel(x.Department))
            .Select(g => new { region = g.Key, count = g.Count() })
            .ToList();

        var centralStdAuto = personnelResult.Items.Count(x =>
            ContainsText(x.Department, "中")
            && !x.IsOnsite);
        var centralOnsiteAuto = personnelResult.Items.Count(x =>
            ContainsText(x.Department, "中")
            && x.IsOnsite);
        var northwestStdAuto = personnelResult.Items.Count(x =>
            (ContainsText(x.Department, "西北") || ContainsText(x.Department, "新疆"))
            && !x.IsOnsite);
        var northwestOnsiteAuto = personnelResult.Items.Count(x =>
            (ContainsText(x.Department, "西北") || ContainsText(x.Department, "新疆"))
            && x.IsOnsite);

        var authorizedHeadcount = PositiveOrDefault(sourceInput?.AuthorizedHeadcount, teamTotal);
        var centralStandardServiceCount = PositiveOrDefault(sourceInput?.CentralStandardServiceCount, centralStdAuto);
        var centralOnsiteCount = PositiveOrDefault(sourceInput?.CentralOnsiteCount, centralOnsiteAuto);
        var northwestStandardServiceCount = PositiveOrDefault(sourceInput?.NorthwestStandardServiceCount, northwestStdAuto);
        var northwestOnsiteCount = PositiveOrDefault(sourceInput?.NorthwestOnsiteCount, northwestOnsiteAuto);

        var personnelNameSet = personnelResult.Items
            .Select(x => x.Name?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var specialWorkHours = InMemoryWorkHoursService.GetSnapshot()
            .Where(x => personnelNameSet.Contains(x.PersonnelName))
            .Where(x => DateTime.TryParse(x.WorkDate, out var workDate)
                     && workDate.Date >= monthStart
                     && workDate.Date < monthEnd)
            .ToList();

        var autoSickLeaveCount = specialWorkHours
            .Where(x => string.Equals(x.WorkType, "病假", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var autoPersonalLeaveCount = specialWorkHours
            .Where(x => string.Equals(x.WorkType, "事假", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var autoOtherSpecialCount = specialWorkHours
            .Where(x => string.Equals(x.WorkType, "其他特殊", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var sickLeaveCount = PositiveOrDefault(sourceInput?.SickLeaveCount, autoSickLeaveCount);
        var personalLeaveCount = PositiveOrDefault(sourceInput?.PersonalLeaveCount, autoPersonalLeaveCount);
        var otherSpecialCount = PositiveOrDefault(sourceInput?.OtherSpecialCount, autoOtherSpecialCount);
        var otherSpecialRemark = sourceInput?.OtherSpecialRemark?.Trim() ?? string.Empty;

        var handoverResult = await handoverService.QueryAsync(new HandoverQuery
        {
            FromGroup = groupName,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var handoverItems = handoverResult.Items
            .Where(h => h.EmailSentDate.HasValue
                     && h.EmailSentDate.Value.Date >= monthStart
                     && h.EmailSentDate.Value.Date < monthEnd)
            .Select(h => new
            {
                hospitalName = h.HospitalName,
                productName = h.ProductName,
                fromPerson = h.FromOwner,
                toPerson = h.ToOwner,
                stage = h.Stage
            })
            .ToList();

        var inspectionResult = await inspectionService.QueryAsync(new InspectionQuery
        {
            GroupName = groupName,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var inspectionInMonth = inspectionResult.Items
            .Where(i => i.PlanDate.Date >= monthStart && i.PlanDate.Date < monthEnd)
            .ToList();

        var inspectionRecords = inspectionInMonth
            .Select(i => new
            {
                hospitalName = i.HospitalName,
                productName = i.ProductName,
                plannedDate = i.PlanDate.ToString("yyyy-MM-dd"),
                status = i.Status
            })
            .ToList();

        var inspectionTotal = inspectionRecords.Count;
        var inspectionCompleted = inspectionRecords.Count(x =>
            string.Equals(x.status, "已完成", StringComparison.OrdinalIgnoreCase));

        var repairResult = await repairRecordService.QueryAsync(new RepairRecordQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var repairInMonthAndGroup = repairResult.Items
            .Where(r => r.ReportedAt.HasValue
                     && r.ReportedAt.Value.Date >= monthStart
                     && r.ReportedAt.Value.Date < monthEnd
                     && groupProjectSet.Contains($"{r.HospitalName}||{r.ProductName}"))
            .ToList();

        var incidents = repairInMonthAndGroup
            .Where(r => string.Equals(r.Severity, "紧急", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(r.Severity, "严重", StringComparison.OrdinalIgnoreCase))
            .Select(r => new
            {
                hospitalName = r.HospitalName,
                description = r.Description,
                date = r.ReportedAt?.ToString("yyyy-MM-dd") ?? string.Empty,
                resolution = r.Resolution
            })
            .ToList();

        var repairTotal = repairInMonthAndGroup.Count;
        var repairResolved = repairInMonthAndGroup.Count(r =>
            string.Equals(r.Status, "已完成", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Status, "已关闭", StringComparison.OrdinalIgnoreCase));

        var nextMonthInspectionPlan = inspectionResult.Items
            .Where(i => i.PlanDate.Date >= nextMonthStart && i.PlanDate.Date < nextMonthEnd)
            .Where(i => !string.Equals(i.Status, "已完成", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(i.Status, "已取消", StringComparison.OrdinalIgnoreCase))
            .Select(i => new
            {
                hospitalName = i.HospitalName,
                productName = i.ProductName,
                plannedDate = i.PlanDate.ToString("yyyy-MM-dd")
            })
            .ToList();

        Func<object, string> json = obj => System.Text.Json.JsonSerializer.Serialize(obj);

        var weeklyRate = inspectionTotal > 0 ? (decimal)inspectionCompleted / inspectionTotal : 0m;
        var monthlyRate = repairTotal > 0 ? (decimal)repairResolved / repairTotal : 0m;

        var totalCustomers = groupProjects
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var totalProducts = groupProjects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "||", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var centralProjects = groupProjects.Where(x => ContainsText(x.Province, "北京") || ContainsText(x.Province, "天津") || ContainsText(x.Province, "河北") || ContainsText(x.GroupName, "中")).ToList();
        var northwestProjects = groupProjects.Where(x => ContainsText(x.Province, "陕西") || ContainsText(x.Province, "甘肃") || ContainsText(x.Province, "青海") || ContainsText(x.Province, "宁夏") || ContainsText(x.Province, "新疆") || ContainsText(x.GroupName, "西北") || ContainsText(x.GroupName, "新疆")).ToList();

        var centralCustomerCount = centralProjects
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var centralProductCount = centralProjects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var northwestCustomerCount = northwestProjects
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var northwestProductCount = northwestProjects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var onsiteDeductionItems = personnelScopes
            .Where(x => x.Person.IsOnsite)
            .Select(x => new MonthlyReportSourceOnsiteDeductionDto
            {
                PersonnelId = x.PreviewItem.PersonnelId,
                Name = x.PreviewItem.Name,
                DeductedHospitalCount = x.PreviewItem.ResponsibilityHospitalCount,
                DeductedProductCount = x.PreviewItem.ResponsibilityProductCount,
                HospitalNames = x.PreviewItem.HospitalNames,
                ProductNames = x.PreviewItem.ProductNames
            })
            .ToList();

        var excludedCustomerCount = onsiteDeductionItems
            .SelectMany(x => x.HospitalNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var excludedProjectCount = onsiteDeductionItems
            .SelectMany(x => x.ProductNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var specialTotal = sickLeaveCount + personalLeaveCount + otherSpecialCount;
        var onsiteTotal = onsitePersonnelCount;

        var headcountExcludeOnsite = Math.Max(1, authorizedHeadcount - onsiteTotal - specialTotal);
        var headcountIncludeOnsite = Math.Max(1, authorizedHeadcount - specialTotal);
        var customersExcludeOnsite = Math.Max(0, totalCustomers - excludedCustomerCount);
        var productsExcludeOnsite = Math.Max(0, totalProducts - excludedProjectCount);
        var unmatchedPersonnelCount = personnelScopes.Count(x => x.PreviewItem.ResponsibilityHospitalCount == 0 && x.PreviewItem.ResponsibilityProductCount == 0);

        var teamSummary = new
        {
            supervisorName,
            authorizedHeadcount,
            totalHeadcount = teamTotal,
            onsiteCount = onsitePersonnelCount,
            remoteCount = Math.Max(0, teamTotal - onsitePersonnelCount),
            centralStandardServiceCount,
            centralOnsiteCount,
            northwestStandardServiceCount,
            northwestOnsiteCount,
            unmatchedPersonnelCount,
            special = new
            {
                sickLeaveCount,
                personalLeaveCount,
                otherSpecialCount,
                otherSpecialRemark
            }
        };

        var projectOverview = new
        {
            totalCustomers,
            totalProducts,
            central = new
            {
                customerCount = centralCustomerCount,
                productCount = centralProductCount
            },
            northwest = new
            {
                customerCount = northwestCustomerCount,
                productCount = northwestProductCount
            },
            onsiteDeduction = new
            {
                customerCount = excludedCustomerCount,
                productCount = excludedProjectCount,
                personnelCount = onsiteDeductionItems.Count
            }
        };

        var perCapitaMetrics = new
        {
            allPersonnelAverage = new
            {
                headcount = headcountIncludeOnsite,
                customerCount = totalCustomers,
                productCount = totalProducts,
                customerPerPerson = Math.Round((double)totalCustomers / headcountIncludeOnsite, 1),
                productPerPerson = Math.Round((double)totalProducts / headcountIncludeOnsite, 1)
            },
            excludeOnsiteAverage = new
            {
                headcount = headcountExcludeOnsite,
                customerCount = customersExcludeOnsite,
                productCount = productsExcludeOnsite,
                customerPerPerson = Math.Round((double)customersExcludeOnsite / headcountExcludeOnsite, 1),
                productPerPerson = Math.Round((double)productsExcludeOnsite / headcountExcludeOnsite, 1),
                deductedHeadcount = onsiteTotal,
                deductedCustomerCount = excludedCustomerCount,
                deductedProductCount = excludedProjectCount
            }
        };

        var previewDto = new MonthlyReportSourcePreviewDto
        {
            ReportMonth = reportMonth ?? string.Empty,
            GroupName = groupName ?? string.Empty,
            SupervisorName = supervisorName ?? string.Empty,
            TeamSummary = new MonthlyReportSourceTeamSummaryDto
            {
                AuthorizedHeadcount = authorizedHeadcount,
                TotalHeadcount = teamTotal,
                OnsiteCount = onsitePersonnelCount,
                RemoteCount = Math.Max(0, teamTotal - onsitePersonnelCount),
                CentralStandardServiceCount = centralStandardServiceCount,
                CentralOnsiteCount = centralOnsiteCount,
                NorthwestStandardServiceCount = northwestStandardServiceCount,
                NorthwestOnsiteCount = northwestOnsiteCount,
                UnmatchedPersonnelCount = unmatchedPersonnelCount,
                SickLeaveCount = sickLeaveCount,
                PersonalLeaveCount = personalLeaveCount,
                OtherSpecialCount = otherSpecialCount,
                OtherSpecialRemark = otherSpecialRemark
            },
            ProjectSummary = new MonthlyReportSourceProjectSummaryDto
            {
                TotalCustomerCount = totalCustomers,
                TotalProductCount = totalProducts,
                CentralCustomerCount = centralCustomerCount,
                CentralProductCount = centralProductCount,
                NorthwestCustomerCount = northwestCustomerCount,
                NorthwestProductCount = northwestProductCount,
                OnsiteDeductedCustomerCount = excludedCustomerCount,
                OnsiteDeductedProductCount = excludedProjectCount
            },
            PerCapitaMetrics = new MonthlyReportSourcePerCapitaDto
            {
                AllPersonnelAverage = new MonthlyReportSourceMetricBlockDto
                {
                    Headcount = headcountIncludeOnsite,
                    CustomerCount = totalCustomers,
                    ProductCount = totalProducts,
                    CustomerPerPerson = Math.Round((double)totalCustomers / headcountIncludeOnsite, 1),
                    ProductPerPerson = Math.Round((double)totalProducts / headcountIncludeOnsite, 1)
                },
                ExcludeOnsiteAverage = new MonthlyReportSourceMetricBlockDto
                {
                    Headcount = headcountExcludeOnsite,
                    CustomerCount = customersExcludeOnsite,
                    ProductCount = productsExcludeOnsite,
                    CustomerPerPerson = Math.Round((double)customersExcludeOnsite / headcountExcludeOnsite, 1),
                    ProductPerPerson = Math.Round((double)productsExcludeOnsite / headcountExcludeOnsite, 1)
                }
            },
            PersonnelItems = personnelScopes.Select(x => x.PreviewItem).ToList(),
            OnsiteDeductionItems = onsiteDeductionItems,
            Warnings = BuildPreviewWarnings(supervisorName ?? string.Empty, teamTotal, groupProjects.Count, personnelScopes.Select(x => x.PreviewItem).ToList(), sickLeaveCount, personalLeaveCount, otherSpecialCount)
        };

        return new MonthlyReportSourceComputationResult
        {
            PreviewDto = previewDto,
            UpsertDto = new MonthlyReportUpsertDto
            {
                HospitalName = string.Empty,
                ReportMonth = reportMonth ?? string.Empty,
                GroupName = groupName ?? string.Empty,
                Title = $"{groupName ?? string.Empty} {reportMonth ?? string.Empty} 月报",
                Content = "系统自动生成",
                TeamTotal = authorizedHeadcount,
                TeamOnsiteJson = json(onsiteList),
                TeamSummaryJson = JsonSerializer.Serialize(teamSummary),
                ProjectOverviewJson = JsonSerializer.Serialize(projectOverview),
                PerCapitaMetricsJson = JsonSerializer.Serialize(perCapitaMetrics),
                HandoverItemsJson = json(handoverItems),
                WeeklyReportRate = weeklyRate,
                MonthlyReportRate = monthlyRate,
                MajorDemandAcceptanceJson = "[]",
                InspectionRecordsJson = json(inspectionRecords),
                AnnualServiceReportsJson = "[]",
                IncidentsJson = json(incidents),
                NextMonthInspectionPlanJson = json(nextMonthInspectionPlan),
                NextMonthAnnualReportPlanJson = "[]",
                NextMonthOtherPlanJson = "[]",
                Status = "draft"
            }
        };
    }

    private static bool TryParseReportMonth(string reportMonth, out DateTime monthStart)
    {
        monthStart = default;
        if (string.IsNullOrWhiteSpace(reportMonth))
        {
            return false;
        }

        if (!DateTime.TryParse($"{reportMonth.Trim()}-01", out var parsed))
        {
            return false;
        }

        monthStart = new DateTime(parsed.Year, parsed.Month, 1);
        return true;
    }

    private static string NormalizeSubmittedBy(string? submittedBy, string groupName)
    {
        if (!string.IsNullOrWhiteSpace(submittedBy))
        {
            return submittedBy.Trim();
        }

        return string.IsNullOrWhiteSpace(groupName)
            ? "系统自动生成"
            : $"{groupName.Trim()}月报自动生成";
    }

    private static string BuildWorkHoursCsv(IReadOnlyList<WorkHoursReportRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("商机编号,医院名称,产品,省份,组别,销售,运维人员,实施状态,售后项目类型,工时人天,人员数,人员1,人员2,人员3,人员4,人员5,维护开始时间,维护结束时间,备注");
        foreach (var row in rows)
        {
            var cols = new[]
            {
                Csv(row.OpportunityNumber), Csv(row.HospitalName), Csv(row.ProductName), Csv(row.Province), Csv(row.GroupName),
                Csv(row.SalesName), Csv(row.MaintenancePersonName), Csv(row.ImplementationStatus), Csv(row.AfterSalesProjectType),
                row.WorkHoursManDays.ToString("0.##"), row.PersonnelCount.ToString(),
                Csv(row.Personnel1), Csv(row.Personnel2), Csv(row.Personnel3), Csv(row.Personnel4), Csv(row.Personnel5),
                Csv(row.MaintenanceStartDate), Csv(row.MaintenanceEndDate), Csv(row.Remarks)
            };
            sb.AppendLine(string.Join(',', cols));
        }

        return sb.ToString();
    }

    private static string BuildMonthlyReportCsv(IReadOnlyList<MonthlyReportItemDto> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("ID,月份,组别,提交人,标题,团队总人数,团队结构(JSON),项目情况(JSON),人均核算(JSON),周报提交率,月报提交率,驻场人员(JSON),交接事项(JSON),巡检记录(JSON),事件报修(JSON),下月巡检计划(JSON),状态,创建时间");
        foreach (var row in rows.OrderByDescending(x => x.CreatedAt))
        {
            var cols = new[]
            {
                row.Id.ToString(), Csv(row.ReportMonth), Csv(row.GroupName), Csv(row.SubmittedBy), Csv(row.Title),
                row.TeamTotal.ToString(), Csv(row.TeamSummaryJson), Csv(row.ProjectOverviewJson), Csv(row.PerCapitaMetricsJson), row.WeeklyReportRate.ToString("0.####"), row.MonthlyReportRate.ToString("0.####"),
                Csv(row.TeamOnsiteJson), Csv(row.HandoverItemsJson), Csv(row.InspectionRecordsJson), Csv(row.IncidentsJson),
                Csv(row.NextMonthInspectionPlanJson), Csv(row.Status), Csv(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            };
            sb.AppendLine(string.Join(',', cols));
        }

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var normalized = value ?? string.Empty;
        var escaped = normalized.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static int NonNegative(int? value)
    {
        if (!value.HasValue)
        {
            return 0;
        }

        return Math.Max(0, value.Value);
    }

    private static int PositiveOrDefault(int? value, int defaultValue)
    {
        if (value.HasValue && value.Value > 0)
        {
            return value.Value;
        }

        return Math.Max(0, defaultValue);
    }

    private static bool ContainsText(string? source, string target)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(target, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveGroupSupervisorName(
        IReadOnlyCollection<PersonnelItemDto> personnelItems,
        IReadOnlyDictionary<int, PersonnelAccessItemDto> accessById)
    {
        foreach (var person in personnelItems)
        {
            if (!accessById.TryGetValue(person.Id, out var accessItem))
            {
                continue;
            }

            if (string.Equals(accessItem.SystemRole, "supervisor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(accessItem.SystemRole, "manager", StringComparison.OrdinalIgnoreCase))
            {
                return person.Name?.Trim() ?? string.Empty;
            }
        }

        return personnelItems
            .Select(person => accessById.TryGetValue(person.Id, out var accessItem) ? accessItem.SupervisorName?.Trim() ?? string.Empty : string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string ResolvePersonnelSupervisorName(
        PersonnelItemDto person,
        PersonnelAccessItemDto? accessItem,
        string groupSupervisorName)
    {
        if (accessItem is not null
            && (string.Equals(accessItem.SystemRole, "supervisor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(accessItem.SystemRole, "manager", StringComparison.OrdinalIgnoreCase)))
        {
            return person.Name?.Trim() ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(accessItem?.SupervisorName))
        {
            return accessItem!.SupervisorName!.Trim();
        }

        return groupSupervisorName;
    }

    private static List<Domain.Entities.ProjectEntity> GetAssignedProjectsForPersonnel(
        IEnumerable<Domain.Entities.ProjectEntity> projects,
        string personnelName)
    {
        return projects
            .Where(project => IsPersonnelAssigned(project, personnelName))
            .ToList();
    }

    private static bool IsPersonnelAssigned(Domain.Entities.ProjectEntity project, string personnelName)
    {
        if (string.IsNullOrWhiteSpace(personnelName))
        {
            return false;
        }

        return ContainsPersonnelName(project.MaintenancePersonName, personnelName)
            || ContainsPersonnelName(project.Personnel1, personnelName)
            || ContainsPersonnelName(project.Personnel2, personnelName)
            || ContainsPersonnelName(project.Personnel3, personnelName)
            || ContainsPersonnelName(project.Personnel4, personnelName)
            || ContainsPersonnelName(project.Personnel5, personnelName);
    }

    private static bool ContainsPersonnelName(string? source, string personnelName)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(personnelName))
        {
            return false;
        }

        var normalizedName = personnelName.Trim();
        var tokens = source
            .Split(PersonnelSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToList();

        if (tokens.Any(token => string.Equals(token, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return source.Contains(normalizedName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRegionLabel(string? department)
    {
        if (ContainsText(department, "西北") || ContainsText(department, "新疆"))
        {
            return "西北区";
        }

        if (ContainsText(department, "中"))
        {
            return "中区";
        }

        return string.IsNullOrWhiteSpace(department) ? "未分类" : department.Trim();
    }

    private static string FormatProductScope(string? hospitalName, string? productName)
    {
        var hospital = hospitalName?.Trim() ?? string.Empty;
        var product = productName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(hospital) && string.IsNullOrWhiteSpace(product))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(hospital))
        {
            return product;
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            return hospital;
        }

        return $"{hospital} / {product}";
    }

    private static List<string> BuildPreviewWarnings(
        string supervisorName,
        int teamTotal,
        int projectCount,
        IReadOnlyCollection<MonthlyReportSourcePersonnelItemDto> personnelItems,
        int sickLeaveCount,
        int personalLeaveCount,
        int otherSpecialCount)
    {
        var warnings = new List<string>();

        if (teamTotal == 0)
        {
            warnings.Add("当前组别未匹配到人员台账数据。");
        }

        if (projectCount == 0)
        {
            warnings.Add("当前组别未匹配到项目台账数据。");
        }

        if (string.IsNullOrWhiteSpace(supervisorName))
        {
            warnings.Add("当前组别未识别到主管，请检查权限配置中的上级主管与系统角色。");
        }

        var unmatchedNames = personnelItems
            .Where(item => item.ResponsibilityHospitalCount == 0 && item.ResponsibilityProductCount == 0)
            .Select(item => item.Name)
            .ToList();
        if (unmatchedNames.Count > 0)
        {
            warnings.Add($"以下人员暂未匹配到负责医院/产品：{string.Join("、", unmatchedNames)}。");
        }

        if (sickLeaveCount == 0 && personalLeaveCount == 0 && otherSpecialCount == 0)
        {
            warnings.Add("病假、事假和其他特殊情况已改为按工时管理中的对应类型自动统计；当前月份未检索到相关登记记录。");
        }

        return warnings;
    }

    private sealed class MonthlyReportSourceComputationResult
    {
        public MonthlyReportUpsertDto UpsertDto { get; init; } = new();
        public MonthlyReportSourcePreviewDto PreviewDto { get; init; } = new();
    }
}
