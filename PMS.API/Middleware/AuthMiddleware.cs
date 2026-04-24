using PMS.Application.Contracts.Auth;

namespace PMS.API.Middleware;

public class AuthMiddleware(RequestDelegate next)
{
    public const string AccessTokenItemKey = "AccessToken";

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsPublicPath(path))
        {
            await next(context);
            return;
        }

        var token = ResolveBearerToken(context.Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        var session = await authService.ValidateTokenAsync(token, context.RequestAborted);
        if (session is null)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        context.Items[PermissionMiddleware.CurrentPersonnelIdItemKey] = session.PersonnelId;
        context.Items[AccessTokenItemKey] = token;

        await next(context);
    }

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/health",
    };

    private static bool IsPublicPath(string path)
    {
        var trimmed = path.TrimEnd('/');
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        return PublicPaths.Contains(trimmed);
    }

    private static string? ResolveBearerToken(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorization[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new
        {
            code = 401,
            message = "unauthorized"
        });
    }
}
