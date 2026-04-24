import re

path = r'c:\Users\Administrator\PMS-Standalone\pms-web\src\views\report\WorkHoursReportView.vue'

with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# First undo any partial mistake we made
text = re.sub(
    r'<div class="apple-page-header">.*?</template>',
    '<template>',
    text,
    flags=re.DOTALL
)

# Wait actually we already checked out the file before, let's just do git checkout and do it right
