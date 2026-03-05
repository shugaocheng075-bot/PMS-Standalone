namespace PMS.Application.Models.Auth;

public class LoginRequest
{
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string SystemRole { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class AuthSessionDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int PersonnelId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class AuthAccountDto
{
    public int PersonnelId { get; set; }
    public string Account { get; set; } = string.Empty;
    public string PersonnelName { get; set; } = string.Empty;
}
