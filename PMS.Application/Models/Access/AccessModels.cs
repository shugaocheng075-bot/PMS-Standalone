using PMS.Application.Models;

namespace PMS.Application.Models.Access;

public class PermissionDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PersonnelActorDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
}

public class PersonnelAccessItemDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int PermissionCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PersonnelAccessProfileDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<string> Permissions { get; set; } = [];
}

public class PersonnelAccessQuery
{
    public string? Name { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}