namespace PMS.API.Middleware;

public static class HttpContextPermissionExtensions
{
    public static int GetCurrentPersonnelId(this HttpContext context)
    {
        if (context.Items.TryGetValue(PermissionMiddleware.CurrentPersonnelIdItemKey, out var value) &&
            value is int personnelId &&
            personnelId > 0)
        {
            return personnelId;
        }

        return 0;
    }

    public static string? GetAccessToken(this HttpContext context)
    {
        if (context.Items.TryGetValue(AuthMiddleware.AccessTokenItemKey, out var value) && value is string token)
        {
            return token;
        }

        return null;
    }
}