namespace PMS.Domain.Entities;

/// <summary>
/// 月报实体 — 对应月报 Excel 的多段结构
/// </summary>
public class MonthlyReportEntity
{
    public long Id { get; set; }

    /// <summary>报告月份 yyyy-MM</summary>
    public string ReportMonth { get; set; } = string.Empty;

    /// <summary>提交人（组长姓名）</summary>
    public string SubmittedBy { get; set; } = string.Empty;

    /// <summary>组名</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>月报标题</summary>
    public string Title { get; set; } = string.Empty;

    // ─── 第1段：团队基本信息 ───
    /// <summary>总人数</summary>
    public int TeamTotal { get; set; }
    /// <summary>驻场人员明细 JSON [{ region, count }]</summary>
    public string TeamOnsiteJson { get; set; } = "[]";
    /// <summary>团队人数结构 JSON</summary>
    public string TeamSummaryJson { get; set; } = "{}";
    /// <summary>项目情况 JSON（客户数/产品数）</summary>
    public string ProjectOverviewJson { get; set; } = "{}";
    /// <summary>人均指标 JSON</summary>
    public string PerCapitaMetricsJson { get; set; } = "{}";

    // ─── 第2段：项目交接情况 ───
    /// <summary>本月交接项目 JSON [{ hospitalName, productName, fromPerson, toPerson, date }]</summary>
    public string HandoverItemsJson { get; set; } = "[]";

    // ─── 第3段：周报/月报完成情况 ───
    /// <summary>周报完成率</summary>
    public decimal WeeklyReportRate { get; set; }
    /// <summary>月报完成率</summary>
    public decimal MonthlyReportRate { get; set; }

    // ─── 第4段：重大需求验收 ───
    /// <summary>重大需求验收 JSON [{ hospitalName, demandName, acceptDate, contractAmount, remark }]</summary>
    public string MajorDemandAcceptanceJson { get; set; } = "[]";

    // ─── 第5段：巡检情况 ───
    /// <summary>巡检记录 JSON [{ hospitalName, productName, startDate, endDate, emailSent, specialIssue }]</summary>
    public string InspectionRecordsJson { get; set; } = "[]";

    // ─── 第6段：年度服务报告 ───
    /// <summary>年度服务报告 JSON [{ hospitalName, reportDate, status }]</summary>
    public string AnnualServiceReportsJson { get; set; } = "[]";

    // ─── 第7段：突发事件 ───
    /// <summary>突发事件 JSON [{ hospitalName, description, date, resolution }]</summary>
    public string IncidentsJson { get; set; } = "[]";

    // ─── 第8段：下月计划 ───
    /// <summary>巡检计划 JSON [{ hospitalName, productName, plannedDate }]</summary>
    public string NextMonthInspectionPlanJson { get; set; } = "[]";
    /// <summary>年度服务报告计划 JSON</summary>
    public string NextMonthAnnualReportPlanJson { get; set; } = "[]";
    /// <summary>其他工作计划</summary>
    public string NextMonthOtherPlanJson { get; set; } = "[]";

    /// <summary>附件路径列表（JSON 序列化存储）</summary>
    public List<string> Attachments { get; set; } = [];

    /// <summary>状态：draft / submitted / approved / rejected</summary>
    public string Status { get; set; } = "draft";

    /// <summary>审批人姓名</summary>
    public string ApprovedBy { get; set; } = string.Empty;
    /// <summary>审批时间</summary>
    public DateTime? ApprovedAt { get; set; }
    /// <summary>驳回原因</summary>
    public string RejectionReason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ─── 兼容旧字段 ───
    public string HospitalName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
