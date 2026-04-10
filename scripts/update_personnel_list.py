import re

filepath = "d:/Projects/PMS-Standalone/pms-web/src/views/personnel/PersonnelListView.vue"
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace("import AppFilterCard from '../../components/AppFilterCard.vue'", "import ProTable from '../../components/ProTable.vue'")
content = content.replace("import AppTableCard from '../../components/AppTableCard.vue'", "import ProDrawer from '../../components/ProDrawer.vue'")
content = content.replace("import AppFormDialog from '../../components/AppFormDialog.vue'", "")

match = re.search(r'<el-form-item class="filter-actions">(.*?)</el-form-item>', content, re.DOTALL)
if match:
    actions = match.group(1)
    search_actions = []
    toolbar_actions = []
    for btn in re.finditer(r'<el-button[^>]*>.*?</el-button>', actions, re.DOTALL):
        btn_str = btn.group(0)
        if '查询' in btn_str or '重置' in btn_str or 'onSearch' in btn_str or 'onReset' in btn_str:
            search_actions.append(btn_str)
        else:
            toolbar_actions.append(btn_str)
    new_search_item = '<el-form-item class="filter-actions">\n          ' + '\n          '.join(search_actions) + '\n        </el-form-item>'
    content = content.replace(match.group(0), new_search_item)
    toolbar_template = '<template #toolbar>\n        ' + '\n        '.join(toolbar_actions) + '\n      </template>\n\n      '
else:
    toolbar_template = ''

content = re.sub(
    r'<AppFilterCard>\s*(<el-form.*?</el-form>)\s*</AppFilterCard>',
    r'<template #search>\n      \1\n    </template>',
    content,
    flags=re.DOTALL
)

pro_table_start = f'''<ProTable
      title="人员列表"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="loadData"
      @pagination-change="loadData"
      stripe
      empty-text="暂无符合条件的数据"
    >
      ''' + toolbar_template

content = re.sub(r'<AppTableCard>\s*<el-table[^>]*>', pro_table_start, content, count=1, flags=re.DOTALL)
content = re.sub(r'<div class="pager(?:-container)?">.*?</div>', '', content, flags=re.DOTALL)
content = content.replace('</el-table>', '')
content = content.replace('</AppTableCard>', '</ProTable>')

search_block_match = re.search(r'<template #search>.*?</template>', content, re.DOTALL)
if search_block_match:
    search_block = search_block_match.group(0)
    content = content.replace(search_block, '')
    content = content.replace(pro_table_start, pro_table_start + '\n      ' + search_block)

content = content.replace('<AppFormDialog ', '<ProDrawer ')
content = content.replace('</AppFormDialog>', '</ProDrawer>')

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
