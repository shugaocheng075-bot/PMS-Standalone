import re
import os

def update_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Replace AppFilterCard with ProTable
    # Finding <AppFilterCard>\n<el-form> ... </el-form>\n</AppFilterCard>

    content = re.sub(
        r'<AppFilterCard>\s*(<el-form.*?</el-form>)\s*</AppFilterCard>',
        r'<template #search>\n      \1\n    </template>',
        content,
        flags=re.DOTALL
    )

    # 2. Extract toolbar actions from #search form
    #  <el-form-item class="filter-actions"> ... </el-form-item>
    
    match = re.search(r'<el-form-item class="filter-actions">(.*?)</el-form-item>', content, re.DOTALL)
    if match:
        actions = match.group(1)
        # find buttons inside actions
        # We need to move the '导出CSV' and '批量编辑' to toolbar, leave '查询' and '重置' in search
        search_actions = []
        toolbar_actions = []
        for btn in re.finditer(r'<el-button.*?>.*?</el-button>', actions, re.DOTALL):
            btn_str = btn.group(0)
            if '查询' in btn_str or '重置' in btn_str or 'onSearch' in btn_str or 'onReset' in btn_str:
                search_actions.append(btn_str)
            else:
                toolbar_actions.append(btn_str)
        
        # reconstruct search actions
        new_search_item = '<el-form-item class="filter-actions">\n          ' + '\n          '.join(search_actions) + '\n        </el-form-item>'
        content = content.replace(match.group(0), new_search_item)
        
        toolbar_template = '<template #toolbar>\n        ' + '\n        '.join(toolbar_actions) + '\n      </template>'

    else:
        toolbar_template = '<template #toolbar></template>'

    # 3. Replace <AppTableCard> with ProTable wrapper
    # First, find <el-table ... >
    table_match = re.search(r'<el-table(.*?)stripe(.*?)max-height="520"(.*?)scrollbar-always-on(.*?)empty-text="暂无符合条件的数据"(.*?)>', content, re.DOTALL)
    
    # Replace AppTableCard with ProTable component opening
    pro_table_start = f'''<ProTable
      title="项目列表明细"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="loadData"
      @pagination-change="loadData"
      stripe
      empty-text="暂无符合条件的数据"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDoubleClick"
    >'''
    
    content = re.sub(r'<AppTableCard>\s*<el-table[^>]*>', pro_table_start, content, count=1, flags=re.DOTALL)
    
    # Remove pager
    content = re.sub(r'<div class="pager">.*?</div>', '', content, flags=re.DOTALL)
    content = content.replace('</el-table>', '')
    content = content.replace('</AppTableCard>', '</ProTable>')
    
    # insert toolbar right after title search
    content = content.replace('</template>', '</template>\n\n    ' + toolbar_template, 1)

    # 4. Replace AppFormDialog with ProDrawer
    content = content.replace('<AppFormDialog ', '<ProDrawer ')
    content = content.replace('</AppFormDialog>', '</ProDrawer>')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

update_file("d:/Projects/PMS-Standalone/pms-web/src/views/project/ProjectListView.vue")
