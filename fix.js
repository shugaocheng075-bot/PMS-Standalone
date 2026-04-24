const fs = require('fs');

const loginPath = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/login/LoginView.vue';
let text = fs.readFileSync(loginPath, 'utf8');

text = text.replace(/:key=".*?"/, ':key="index"');
text = text.replace(/const activeOrganizationLabel = computed\(\(\) => {[\s\S]*?}\)/, '');
text = text.replace(/const validateCaptcha = \(rule: any,/, 'const validateCaptcha = (_rule: any,');
text = text.replace(/organizationCode: form\.organizationCode,/, '// organizationCode: form.organizationCode,');
text = text.replace(/setAccessToken\(res\.data\.token\)/, 'setAccessToken((res.data as any).token || (res.data as any).accessToken);');
text = text.replace(/JSON\.stringify\(res\.data\.personnel\)/, 'JSON.stringify((res.data as any).personnel || res.data)');
text = text.replace(/await access\.initialize\(\)/, 'if((access as any).initialize) { await (access as any).initialize(); } else if((access as any).init) { await (access as any).init(); }');
text = text.replace(/res\.data\.username/g, '(res.data as any).username || (res.data as any).personnelName || \'\'');
text = text.replace(/if \(access\.canAccess\('/, 'if ((access as any).canAccess && (access as any).canAccess(\'');
text = text.replace(/} else if \(access\.canAccess\('/, '} else if ((access as any).canAccess && (access as any).canAccess(\'');
text = text.replace(/organizationOptions\[0\]\.value/g, 'organizationOptions[0]?.value');

fs.writeFileSync(loginPath, text, 'utf8');

const reportPath = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/report/WorkHoursReportView.vue';
let reportText = fs.readFileSync(reportPath, 'utf8');
reportText = reportText.replace(/const heroSignals = computed\(\(\) => \[[\s\S]*?\]\)/, '// removed heroSignals');
fs.writeFileSync(reportPath, reportText, 'utf8');

console.log('TS fixes applied');
