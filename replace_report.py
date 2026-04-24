# -*- coding: utf-8 -*-
import io

path = r'c:\Users\Administrator\PMS-Standalone\pms-web\src\views\report\WorkHoursReportView.vue'
with io.open(path, 'r', encoding='utf-8') as f:
    text = f.read()

idx1 = text.find('<div class="report-hero">')
idx2 = text.find('<ProTable')

if idx1 > 0 and idx2 > 0:
    part1 = text[:idx1]
    part2 = text[idx2:]
    
    replacement = '''    <div class="apple-page-header">
      <div class="apple-page-header-content">
        <h2 class="apple-page-title">工时报表</h2>
        <div class="apple-page-subtitle">围绕当前月份集中处理工时明细、导入、重算和导出。</div>
      </div>
      <div class="apple-page-header-actions">
        <!-- minimalist header -->
      </div>
    </div>

'''
    text = part1 + replacement + part2

css_idx = text.find('<style scoped>')
if css_idx > 0:
    part1 = text[:css_idx]
    css_replacement = '''<style scoped>
.report-page {
  padding: 24px;
  background: #f5f5f7;
  min-height: 100%;
}
.apple-page-header {
  background: #fff;
  padding: 24px 32px;
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.03);
  margin-bottom: 24px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}
.apple-page-title {
  font-size: 28px;
  font-weight: 600;
  color: #1d1d1f;
  margin: 0 0 8px;
}
.apple-page-subtitle {
  font-size: 15px;
  color: #86868b;
  margin: 0;
  max-width: 600px;
  line-height: 1.5;
}
.table-action-group {
  display: flex;
  gap: 12px;
}
.table-footer {
  margin-top: 16px;
  color: #86868b;
  font-size: 14px;
}
</style>
'''
    text = part1 + css_replacement

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Done python script")
