using PMS.Application.Models;
using PMS.Application.Models.Inspection;

namespace PMS.Application.Contracts.Inspection;

public interface IInspectionService
{
    Task<InspectionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<InspectionPlanItemDto>> QueryAsync(InspectionQuery query, CancellationToken cancellationToken = default);

    // ─── SystemAuditTool 集成 ───

    /// <summary>
    /// 接收 SystemAuditTool 推送的巡检结果。
    /// </summary>
    Task SubmitResultAsync(InspectionResultDto result, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量接收巡检结果（一次巡检多个产品）。
    /// </summary>
    Task SubmitResultsAsync(IReadOnlyList<InspectionResultDto> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询已提交的巡检结果。
    /// </summary>
    Task<PagedResult<InspectionResultDto>> QueryResultsAsync(InspectionResultQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定医院+产品的最新巡检结果。
    /// </summary>
    Task<InspectionResultDto?> GetLatestResultAsync(string hospitalName, string productName, CancellationToken cancellationToken = default);
}
