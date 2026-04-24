const fs = require('fs');
const path = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/report/WorkHoursReportView.vue';
let text = fs.readFileSync(path, 'utf8');

const idx1 = text.indexOf('<div class="report-hero">');
const idx2 = text.indexOf('<ProTable');

if (idx1 !== -1 && idx2 !== -1) {
    const replacement = `<div class="apple-page-header">
  <div class="apple-page-header-content">
    <h2 class="apple-page-title">工时报表</h2>
    <p class="apple-page-subtitle">围绕当前月份集中处理工时明细、导入、重算和导出。</p>
  </div>
</div>

    `;
    text = text.substring(0, idx1) + replacement + text.substring(idx2);
}

const cssStart = text.indexOf('<style scoped>');
if (cssStart !== -1) {
    const cssReplacement = `<style scoped>
.report-page {
  padding: 24px;
  background: #f5f5f7;
  min-height: 100vh;
}
.apple-page-header {
  background: #fff;
  padding: 24px 32px;
  border-radius: 16px;
  margin-bottom: 24px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.03);
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}
.apple-page-title {
  font-size: 28px;
  font-weight: 600;
  color: #1d1d1f;
  margin: 0 0 8px;
  letter-spacing: -0.02em;
}
.apple-page-subtitle {
  font-size: 15px;
  color: #86868b;
  margin: 0;
  max-width: 600px;
  line-height: 1.5;
}
`;
    text = text.substring(0, cssStart) + cssReplacement + text.substring(cssStart + 14);
}

fs.writeFileSync(path, text, 'utf8');
console.log('Update successful!');
