---
name: data-export
description: Excel 数据导出模式 — ClosedXML 生成 + 前端 blob 下载
globs: "**/*.cs,**/*.ts,**/*.vue"
---

# 数据导出（Excel）— 操作指南

> 本 Skill 指导你为现有模块添加 Excel 数据导出功能。

## 后端：Controller 导出端点

文件：`PMS.API/Controllers/{Module}/{Module}Controller.cs`

```csharp
[HttpGet("export")]
public async Task<IActionResult> Export(
    [FromQuery] string? keyword,
    // 其他筛选参数...
    CancellationToken cancellationToken)
{
    var personnelId = HttpContext.GetCurrentPersonnelId();
    var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

    // 复用 Service 查询（不分页，取全量）
    var query = new XxxQuery
    {
        Keyword = keyword,
        Page = 1,
        Size = int.MaxValue,
        AccessibleHospitalNames = dataScope.AccessibleHospitalNames,
    };
    var result = await xxxService.QueryAsync(query, cancellationToken);

    // ClosedXML 生成 Excel
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("数据导出");

    // 表头
    ws.Cell(1, 1).Value = "序号";
    ws.Cell(1, 2).Value = "名称";
    // ... 其他列

    // 数据行
    var row = 2;
    foreach (var item in result.Items)
    {
        ws.Cell(row, 1).Value = row - 1;
        ws.Cell(row, 2).Value = item.Name;
        row++;
    }

    // 自动列宽
    ws.Columns().AdjustToContents();

    // 返回文件
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var fileName = $"模块名称_{DateTime.Now:yyyyMMdd}.xlsx";
    return File(
        stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}
```

**要点**：
- 导出查询复用 Service 的 `QueryAsync`，`Size = int.MaxValue` 取全量
- 使用 `ClosedXML` 的 `XLWorkbook`，无需额外 NuGet（已引用）
- 文件名带日期：`模块名称_yyyyMMdd.xlsx`
- 数据范围过滤必须生效（不跳过权限检查）

## 前端：API 客户端

文件：`pms-web/src/api/modules/{module}.ts`

```typescript
export function exportXxx(params?: Partial<XxxQuery>) {
  return request.get<any, Blob>('/xxx/export', {
    params,
    responseType: 'blob',
  })
}
```

## 前端：下载触发

文件：`pms-web/src/views/{module}/{Module}View.vue`

```typescript
const exporting = ref(false)

async function onExport() {
  exporting.value = true
  try {
    const blob = await exportXxx({
      keyword: query.keyword,
      // 传入当前筛选条件
    })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `模块名称_${new Date().toISOString().slice(0, 10)}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch {
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}
```

模板中按钮：
```vue
<el-button :loading="exporting" @click="onExport">导出Excel</el-button>
```

## 验证清单

- [ ] 导出端点在 Swagger 中可见
- [ ] 下载的 .xlsx 文件能正常用 Excel 打开
- [ ] 数据内容与当前筛选条件一致
- [ ] 非 manager 角色导出时数据范围过滤已生效
- [ ] 导出按钮有 loading 状态
- [ ] `dotnet build PMS.slnx` 通过
- [ ] `cd pms-web && npm run build` 通过
