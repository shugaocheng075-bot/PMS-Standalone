namespace PMS.Application.Models.Auth;
using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required(ErrorMessage = "账号不能为空")]
    [StringLength(64, MinimumLength = 2, ErrorMessage = "账号长度需在2到64个字符之间")]
    public string Account { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度需在6到128个字符之间")]
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

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "原密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "原密码长度需在6到128个字符之间")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "新密码长度需在8到128个字符之间")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "新密码需至少包含字母和数字")]
    public string NewPassword { get; set; } = string.Empty;
}
