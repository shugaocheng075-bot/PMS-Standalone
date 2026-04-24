
const fs = require('fs');
const path = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/report/WorkHoursReportView.vue';
let text = fs.readFileSync(path, 'utf-8');
const idx1 = text.indexOf('<div class=\"report-hero\">');
const idx2 = text.indexOf('<ProTable');
if (idx1 > -1 && idx2 > -1) {
  text = text.slice(0, idx1) + '<div class=\"apple-page-header\">\n  <div class=\"apple-page-header-content\">\n    <h2 class=\"apple-page-title\">工时报表</h2>\n    <div class=\"apple-page-subtitle\">围绕当前月份集中处理工时明细、导入、重算和导出。</div>\n  </div>\n</div>\n\n    ' + text.slice(idx2);
}
const cssIdx = text.indexOf('<style scoped>');
if (cssIdx > -1) {
  text = text.slice(0, cssIdx) + '<style scoped>\n.report-page { padding: 24px; background: #f5f5f7; min-height: 100vh; }\n.apple-page-header { background: #fff; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 4px 16px rgba(0,0,0,0.03); }\n.apple-page-title { font-size: 28px; font-weight: 600; color: #1d1d1f; margin: 0 0 8px; letter-spacing: -0.02em; }\n.apple-page-subtitle { font-size: 15px; color: #86868b; }\n</style>\n';
}
fs.writeFileSync(path, text, 'utf-8');
console.log('Node replace done');

