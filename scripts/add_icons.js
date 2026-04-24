const fs = require('fs');
const path = require('path');

const iconMap = {
    '新增': 'Plus',
    '添加': 'Plus',
    '创建': 'Plus',
    '编辑': 'Edit',
    '修改': 'Edit',
    '删除': 'Delete',
    '清除': 'Delete',
    '详情': 'Document',
    '查看': 'View',
    '预览': 'View',
    '上传': 'Upload',
    '导入': 'Upload',
    '下载': 'Download',
    '导出': 'Download',
    '提交': 'Check',
    '保存': 'Check',
    '确认': 'Check',
    '通过': 'Check',
    '完成': 'Check',
    '驳回': 'Close',
    '取消': 'Close',
    '刷新': 'Refresh',
    '重置': 'Refresh',
    '搜索': 'Search',
    '查询': 'Search',
    '分配': 'User',
    '更多': 'MoreFilled',
    '返回': 'Back',
    '撤销': 'RefreshLeft',
    '生成': 'VideoPlay',
    '处理': 'Service'
};

function walk(dir, callback) {
    const list = fs.readdirSync(dir);
    list.forEach(file => {
        const filePath = path.join(dir, file);
        const stat = fs.statSync(filePath);
        if (stat && stat.isDirectory()) {
            walk(filePath, callback);
        } else {
            callback(filePath);
        }
    });
}

function updateMain() {
    const mainPath = path.join(__dirname, '..', 'pms-web', 'src', 'main.ts');
    if (!fs.existsSync(mainPath)) return;
    let content = fs.readFileSync(mainPath, 'utf8');
    
    if (!content.includes('* as ElementPlusIconsVue')) {
        const imports = "import * as ElementPlusIconsVue from '@element-plus/icons-vue'\n";
        content = content.replace("import router from './router'", "import router from './router'\n" + imports);
        const registers = "for (const [key, component] of Object.entries(ElementPlusIconsVue)) {\n  app.component(key, component)\n}\n";
        content = content.replace("const app = createApp(App)", "const app = createApp(App)\n" + registers);
        fs.writeFileSync(mainPath, content, 'utf8');
        console.log('Updated main.ts with icon registrations.');
    }
}

function updateButtons() {
    const srcPath = path.join(__dirname, '..', 'pms-web', 'src');
    walk(srcPath, filePath => {
        if (!filePath.endsWith('.vue')) return;
        
        const content = fs.readFileSync(filePath, 'utf8');
        
        let newContent = content.replace(/(<el-button[^>]*>)([^<]+)(<\/el-button>)/g, (match, startTag, text, endTag) => {
            if (startTag.includes('icon=') || startTag.includes(':icon=')) {
                return match;
            }
            if (match.includes('<el-icon>')) {
                return match;
            }
            
            let matchedIcon = null;
            for (const [key, icon] of Object.entries(iconMap)) {
                if (text.includes(key)) {
                    matchedIcon = icon;
                    break;
                }
            }
            
            if (matchedIcon) {
                const closeIdx = startTag.lastIndexOf('>');
                if (closeIdx !== -1) {
                    const newStart = startTag.slice(0, closeIdx) + ` icon="${matchedIcon}"` + startTag.slice(closeIdx);
                    return newStart + text + endTag;
                }
            }
            return match;
        });
        
        if (newContent !== content) {
            fs.writeFileSync(filePath, newContent, 'utf8');
            console.log(`Updated ${filePath}`);
        }
    });
}

try {
    updateMain();
    updateButtons();
} catch (e) {
    console.error(e);
}
