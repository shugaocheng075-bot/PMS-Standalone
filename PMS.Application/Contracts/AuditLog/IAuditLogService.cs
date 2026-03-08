using PMS.Application.Models;
using PMS.Application.Models.AuditLog;

namespace PMS.Application.Contracts.AuditLog;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogItemDto>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task LogAsync(string operatorName, long operatorId, string action, string module, string target, string detail = "", string ipAddress = "", CancellationToken cancellationToken = default);
}
