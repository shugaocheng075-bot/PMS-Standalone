using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Notification;
using PMS.Application.Models.Notification;

namespace PMS.API.Controllers.Notification;

[ApiController]
[Route("api/notifications")]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var summary = await notificationService.GetSummaryAsync(personnelId, cancellationToken);
        return Ok(ApiResponse<NotificationSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var query = new NotificationQuery
        {
            IsRead = isRead,
            Page = page > 0 ? page : 1,
            Size = size > 0 ? size : 20
        };

        var result = await notificationService.QueryAsync(personnelId, query, cancellationToken);
        return Ok(ApiResponse<object>.Success(result));
    }

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        await notificationService.MarkAsReadAsync(personnelId, id, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { message = "已标记为已读" }));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        await notificationService.MarkAllAsReadAsync(personnelId, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { message = "已全部标记为已读" }));
    }
}
