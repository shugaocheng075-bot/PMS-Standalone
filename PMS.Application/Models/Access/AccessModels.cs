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
    public string SystemRole { get; set; } = string.Empty;
}

public class PersonnelAccessItemDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string SystemRole { get; set; } = string.Empty;
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public bool IsAdmin { get; set; }
    public int PermissionCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PersonnelAccessProfileDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string SystemRole { get; set; } = string.Empty;
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public bool IsAdmin { get; set; }
    public List<string> Permissions { get; set; } = [];
    public DataScopeDto DataScope { get; set; } = new();
}

public class DataScopeDto
{
    /// <summary>own = 仅本人项目, subordinates = 含下属项目, all = 全部</summary>
    public string ScopeType { get; set; } = "all";
    public List<string> AccessiblePersonnelNames { get; set; } = [];
    /// <summary>当前用户可访问的医院名称列表（空列表 = 不限制/全部）</summary>
    public List<string> AccessibleHospitalNames { get; set; } = [];
}

public class SetHospitalScopeRequest
{
    /// <summary>要分配给该用户的医院名称列表</summary>
    public List<string> HospitalNames { get; set; } = [];
}

public class PersonnelAccessQuery
{
    public string? Name { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class SetSystemRoleRequest
{
    public string SystemRole { get; set; } = string.Empty;
}

public class SetSupervisorRequest
{
    public int? SupervisorId { get; set; }
}