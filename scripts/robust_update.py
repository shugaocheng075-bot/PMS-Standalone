import re
import sys

def update_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Imports
    content = content.replace("import AppFilterCard from '../../components/AppFilterCard.vue'", "import ProTable from '../../components/ProTable.vue'")
    content = content.replace("import AppTableCard from '../../components/AppTableCard.vue'", "import ProDrawer from '../../components/ProDrawer.vue'")
    content = content.replace("import AppFormDialog from '../../components/AppFormDialog.vue'", "")

    # Extract actions
    actions_re = re.search(r'<el-form-item class="filter-actions">(.*?)</el-form-item>', content, re.DOTALL)
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

    # Find the el-table to copy its events (row-dblclick etc) and AppTableCard content
    table_card_re = re.search(r'<AppTableCard>(.*?)</AppTableCard>', content, re.DOTALL)
    if table_card_re:
        inner_table = table_card_re.group(1)
        # Parse el-table attributes
        table_attr_re = re.search(r'<el-table([^>]*)>', inner_table)
        table_attrs = table_attr_re.group(1) if table_attr_re else ''
        
        # Build ProTable start tag mapping
        pro_attrs = f'''title="数据列表"
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
      @row-dblclick="onRowDoubleClick"'''
        
        pro_table_start = f'<ProTable\n      {pro_attrs}\n    >'
        
        # Remove el-pagination completely
        stripped_inner = re.sub(r'<el-pagination.*?(?:/>|</el-pagination>)', '', inner_table, flags=re.DOTALL)
        
        # Remove any <div> that is purely for pager
        stripped_inner = re.sub(r'<div class="pager[^"]*">\s*</div>', '', stripped_inner, flags=re.DOTALL)
        
        # Remove <el-table> start and end
        stripped_inner = re.sub(r'<el-table[^>]*>', '', stripped_inner, count=1, flags=re.DOTALL)
        stripped_inner = stripped_inner.replace('</el-table>', '')

        # Assemble new block
        pro_toolbar = f'<template #toolbar>\n        {toolbar_inner}\n      </template>\n\n      '
        
        new_table_block = pro_table_start + '\n      ' + pro_toolbar + search_block + stripped_inner + '\n    </ProTable>'
        
        content = content.replace(table_card_re.group(0), new_table_block)

    # Replace AppFormDialog
    content = content.replace('<AppFormDialog', '<ProDrawer')
    content = content.replace('</AppFormDialog>', '</ProDrawer>')

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

update_file("d:/Projects/PMS-Standalone/pms-web/src/views/personnel/PersonnelListView.vue")
