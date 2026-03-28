function e(){return{printArea:(e,t)=>{if(!e)return;let n=window.open(``,`_blank`,`width=900,height=700`);if(!n)return;let r=Array.from(document.querySelectorAll(`link[rel="stylesheet"], style`)).map(e=>e.outerHTML).join(`
`);n.document.write(`<!DOCTYPE html>
<html>
<head>
  <title>${t??document.title}</title>
  ${r}
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
  ${e.innerHTML}
</body>
</html>`),n.document.close(),n.focus(),setTimeout(()=>{n.print(),n.close()},400)}}}export{e as t};