<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">综合设计方案（全文）</h2>
        <div class="page-subtitle">项目管理平台_综合设计方案_v2 原文展示</div>
      </div>
    </div>

    <el-card shadow="never" class="table-card doc-card">
      <pre class="doc-pre">{{ designDoc }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import designDoc from '../../assets/docs/pms-design-v2.md?raw'
</script>

<style scoped>
.doc-card {
  max-height: calc(100vh - 170px);
  overflow: auto;
}

.doc-pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  line-height: 1.6;
  color: #2e3b63;
  font-size: 13px;
}
</style>
