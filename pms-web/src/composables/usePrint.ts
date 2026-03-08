/**
 * Composable for browser-based printing of specific page content.
 * Usage: const { printArea } = usePrint()
 * Call printArea(elementRef) to print the content of a specific element.
 */
export function usePrint() {
  const printArea = (el: HTMLElement | null, title?: string) => {
    if (!el) return

    const printWindow = window.open('', '_blank', 'width=900,height=700')
    if (!printWindow) return

    const styles = Array.from(document.querySelectorAll('link[rel="stylesheet"], style'))
      .map((s) => s.outerHTML)
      .join('\n')

    printWindow.document.write(`<!DOCTYPE html>
<html>
<head>
  <title>${title ?? document.title}</title>
  ${styles}
  <style>
    body { padding: 20px; background: white; color: #333; font-size: 12px; }
    .el-button, .el-pagination, .el-upload, .filter-card, .page-head .el-select,
    .no-print { display: none !important; }
    .el-table { font-size: 11px; }
    .el-card { box-shadow: none !important; border: 1px solid #eee; margin-bottom: 12px; }
    .page-title { font-size: 18px; }
    @media print {
      body { padding: 0; }
      @page { margin: 10mm; }
    }
  </style>
</head>
<body>
  ${el.innerHTML}
</body>
</html>`)
    printWindow.document.close()
    printWindow.focus()
    setTimeout(() => {
      printWindow.print()
      printWindow.close()
    }, 400)
  }

  return { printArea }
}
