import re

filepath = "d:/Projects/PMS-Standalone/pms-web/src/views/major-demand/MajorDemandView.vue"
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Imports
content = content.replace("import AppFilterCard from '../../components/AppFilterCard.vue'", "import ProTable from '../../components/ProTable.vue'")
content = content.replace("import AppTableCard from '../../components/AppTableCard.vue'", "import ProDrawer from '../../components/ProDrawer.vue'")
content = content.replace("import AppFormDialog from '../../components/AppFormDialog.vue'", "")

# Extract actions from div class="filter-actions" instead of el-form-item
actions_re = re.search(r'<div class="filter-actions">\s*<el-form-item>(.*?)</el-form-item>\s*</div>', content, re.DOTALL)
toolbar_inner = ''
if actions_re:
    actions = actions_re.group(1)
    search_btns = []
    toolbar_btns = []
    for btn in re.finditer(r'<el-button[^>]*>.*?</el-button>', actions, re.DOTALL):
        btn_str = btn.group(0)
        if '查询' in btn_str or '重置' in btn_str or 'onSearch' in btn_str or 'onReset' in btn_str:
            search_btns.append(btn_str)
        else:
            toolbar_btns.append(btn_str)
    
    new_actions = '<el-form-item class="filter-actions">\n          ' + '\n          '.join(search_btns) + '\n        </el-form-item>'
    content = content.replace(actions_re.group(0), new_actions)
    toolbar_inner = '\n        '.join(toolbar_btns)
    
# Replace AppFilterCard
search_form_re = re.search(r'<AppFilterCard>\s*(<el-form.*?</el-form>)\s*</AppFilterCard>', content, re.DOTALL)
search_block = ''
if search_form_re:
    search_block = f'<template #search>\n      {search_form_re.group(1)}\n    </template>\n\n    '
    content = content.replace(search_form_re.group(0), '')

# Add Create button to Toolbar since it's commonly outside
toolbar_inner = f'<el-button type="success" @click="onAddRow">新增</el-button>\n        <el-button :loading="loading.exporting" @click="onExport">导出 Excel</el-button>'

# Replace AppTableCard
table_card_re = re.search(r'<AppTableCard>(.*?)</AppTableCard>', content, re.DOTALL)
if table_card_re:
    inner_table = table_card_re.group(1)
    pro_table_start = f'''<ProTable
      title="需求列表"
      :data="pagedRows"
      :loading="loading.fetching"
      :total="displayRows.length"
      v-model:page="currentPage"
      v-model:size="pageSize"
      @refresh="loadData"
      stripe
      row-key="_rowId"
      empty-text="暂无重大需求数据"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDoubleClick"
    >'''
    
    stripped_inner = re.sub(r'<div class="pager[^"]*">.*?</div>', '', inner_table, flags=re.DOTALL)
    stripped_inner = re.sub(r'<el-table[^>]*>', '', stripped_inner, count=1, flags=re.DOTALL)
    stripped_inner = stripped_inner.replace('</el-table>', '')

    pro_toolbar = f'<template #toolbar>\n        {toolbar_inner}\n      </template>\n\n      '
    new_table_block = pro_table_start + '\n      ' + pro_toolbar + search_block + stripped_inner + '\n    </ProTable>'
    content = content.replace(table_card_re.group(0), new_table_block)

# Replace AppFormDialog
content = content.replace('<AppFormDialog', '<ProDrawer')
content = content.replace('</AppFormDialog>', '</ProDrawer>')

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
