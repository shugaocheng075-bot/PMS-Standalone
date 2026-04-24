const fs = require('fs');

const loginPath = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/login/LoginView.vue';
let text = fs.readFileSync(loginPath, 'utf8');

text = text.replace(/<el-option v-for="option in organizationOptions" :key=".*?"/, '<el-option v-for="option in organizationOptions" :key="option.value"');
text = text.replace(/v-for="\(cell, index\) in qrCells" :key=".*?"/, 'v-for="(cell, index) in qrCells" :key="index"');
text = text.replace(/import { computed, nextTick/, 'import { nextTick');
text = text.replace(/form\.organizationCode = organizationOptions\[0\]\?\.value/, 'form.organizationCode = organizationOptions[0]?.value || \'\'');

fs.writeFileSync(loginPath, text, 'utf8');

const reportPath = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/report/WorkHoursReportView.vue';
let reportText = fs.readFileSync(reportPath, 'utf8');
reportText = reportText.replace(/const uniqueHospitalCount = computed\(\(\) => new Set\(rows\.value\.map\(\(row\) => row\.hospitalName\)\.filter\(Boolean\)\)\.size\)/, '// removed uniqueHospitalCount');
reportText = reportText.replace(/const uniqueProductCount = computed\(\(\) => new Set\(rows\.value\.map\(\(row\) => row\.productName\)\.filter\(Boolean\)\)\.size\)/, '// removed uniqueProductCount');
reportText = reportText.replace(/const uniquePersonnelCount = computed\(\(\) => {[\s\S]*?}\)/, '// removed uniquePersonnelCount');

fs.writeFileSync(reportPath, reportText, 'utf8');
console.log('Fixes applied');
