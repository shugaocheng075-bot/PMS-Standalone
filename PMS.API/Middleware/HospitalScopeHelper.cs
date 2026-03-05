using PMS.Application.Models.Access;

namespace PMS.API.Middleware;

/// <summary>
/// 医院范围验证辅助类：用于业务 Controller 中检查当前用户是否有权访问指定医院。
/// manager/admin 不受限；operator/supervisor 受 AccessibleHospitalNames 约束。
/// </summary>
public static class HospitalScopeHelper
{
    /// <summary>
    /// 检查当前用户的 DataScope 是否允许访问指定医院。
    /// 返回 true = 允许访问，false = 无权限。
    /// </summary>
    public static bool IsHospitalAccessible(DataScopeDto dataScope, string hospitalName)
    {
        if (dataScope is null || string.IsNullOrWhiteSpace(hospitalName))
        {
            return false;
        }

        // ScopeType == "all" 表示 manager/admin，不受限
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // AccessibleHospitalNames 为空 = 未分配任何医院，不允许
        if (dataScope.AccessibleHospitalNames is null || dataScope.AccessibleHospitalNames.Count == 0)
        {
            return false;
        }

        return dataScope.AccessibleHospitalNames.Contains(hospitalName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 对列表进行医院范围过滤。
    /// manager/admin（ScopeType=all）不过滤，其他角色只保留在 AccessibleHospitalNames 中的记录。
    /// </summary>
    public static IEnumerable<T> FilterByHospitalScope<T>(
        DataScopeDto dataScope,
        IEnumerable<T> items,
        Func<T, string> hospitalNameSelector)
    {
        if (dataScope is null)
        {
            return Enumerable.Empty<T>();
        }

        // manager/admin 不过滤
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return items;
        }

        // 未分配医院的用户看不到任何数据
        if (dataScope.AccessibleHospitalNames is null || dataScope.AccessibleHospitalNames.Count == 0)
        {
            return Enumerable.Empty<T>();
        }

        var allowedSet = new HashSet<string>(dataScope.AccessibleHospitalNames, StringComparer.OrdinalIgnoreCase);
        return items.Where(item =>
        {
            var name = hospitalNameSelector(item);
            return !string.IsNullOrWhiteSpace(name) && allowedSet.Contains(name);
        });
    }
}
