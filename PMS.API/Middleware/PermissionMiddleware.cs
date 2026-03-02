using PMS.Application.Contracts.Access;

namespace PMS.API.Middleware;

public class PermissionMiddleware(RequestDelegate next)
{
    public const string CurrentPersonnelIdItemKey = "CurrentPersonnelId";

    public async Task InvokeAsync(HttpContext context, IAccessControlService accessControlService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var personnelId = context.GetCurrentPersonnelId();
        if (personnelId <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                code = 401,
                message = "unauthorized"
            });
            return;
        }

        var requiredPermission = accessControlService.ResolveRequiredPermission(
            context.Request.Method,
            path);

        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            await next(context);
            return;
        }

        var hasPermission = await accessControlService.HasPermissionAsync(personnelId, requiredPermission, context.RequestAborted);
        if (hasPermission)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            code = 403,
            message = "forbidden",
            data = new
            {
                requiredPermission,
                personnelId
            }
        });
    }

}