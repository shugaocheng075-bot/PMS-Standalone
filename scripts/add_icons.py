import os
import re

def update_main():
    main_path = 'pms-web/src/main.ts'
    if not os.path.exists(main_path): return
    with open(main_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if '* as ElementPlusIconsVue' not in content:
        imports = "import * as ElementPlusIconsVue from '@element-plus/icons-vue'\n"
        content = content.replace("import router from './router'", "import router from './router'\n" + imports)
        registers = "for (const [key, component] of Object.entries(ElementPlusIconsVue)) {\n  app.component(key, component)\n}\n"
        content = content.replace("const app = createApp(App)", "const app = createApp(App)\n" + registers)
        with open(main_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print('Updated main.ts with icon registrations.')

icon_map = {
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
}

def update_buttons():
    for root, dirs, files in os.walk('pms-web/src'):
        for f in files:
            if f.endswith('.vue'):
                path = os.path.join(root, f)
                with open(path, 'r', encoding='utf-8') as f_obj:
                    content = f_obj.read()
                
                def replacer(match):
                    full_str = match.group(0)
                    start_tag = match.group(1)
                    text = match.group(2)
                    end_tag = match.group(3)
                    
                    if 'icon=' in start_tag or ':icon=' in start_tag:
                        return full_str
                    
                    if '<el-icon>' in full_str:
                        return full_str
                        
                    matched_icon = None
                    for key, icon in icon_map.items():
                        if key in text:
                            matched_icon = icon
                            break
                            
                    if matched_icon:
                        idx = start_tag.rfind('>')
                        if idx != -1:
                            new_start = start_tag[:idx] + f' icon="{matched_icon}"' + start_tag[idx:]
                            return new_start + text + end_tag
                    return full_str

                new_content = re.sub(r'(<el-button[^>]*>)([^<]+)(</el-button>)', replacer, content)
                
                if new_content != content:
                    with open(path, 'w', encoding='utf-8') as f_obj:
                        f_obj.write(new_content)
                    print(f'Updated {path}')

if __name__ == '__main__':
    update_main()
    update_buttons()
