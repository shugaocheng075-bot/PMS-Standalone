<template>
  <div class="page-shell">
    <div class="page-head">
      <h2 class="page-title">系统设置</h2>
    </div>

    <el-row :gutter="20">
      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>系统信息</span></template>
          <el-descriptions :column="1" border v-loading="loading">
            <el-descriptions-item label="应用名称">{{ info.appName }}</el-descriptions-item>
            <el-descriptions-item label="版本">{{ info.version }}</el-descriptions-item>
            <el-descriptions-item label="运行环境">{{ info.environment }}</el-descriptions-item>
            <el-descriptions-item label=".NET 版本">{{ info.dotnetVersion }}</el-descriptions-item>
            <el-descriptions-item label="操作系统">{{ info.os }}</el-descriptions-item>
            <el-descriptions-item label="服务器时间">{{ info.serverTime }}</el-descriptions-item>
            <el-descriptions-item label="启动时间">{{ info.startTime }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>快捷入口</span></template>
          <div class="shortcuts">
            <el-button @click="$router.push('/permission/manage')">权限管理</el-button>
            <el-button @click="$router.push('/maintenance/data')">数据维护中心</el-button>
            <el-button @click="$router.push('/audit/log')">操作日志</el-button>
            <el-button @click="$router.push('/profile')">个人资料</el-button>
          </div>
        </el-card>

        <el-card shadow="never" style="margin-top: 20px">
          <template #header><span>数据备份与恢复</span></template>
          <div class="backup-section">
            <el-button type="primary" :loading="downloading" @click="onDownloadBackup">下载数据库备份</el-button>
            <el-upload
              :auto-upload="false"
              :show-file-list="false"
              accept=".db"
              :on-change="onRestoreFileSelected"
            >
              <el-button type="warning" :loading="restoring">上传恢复备份</el-button>
            </el-upload>
          </div>
        </el-card>

        <el-card shadow="never" style="margin-top: 20px">
          <template #header><span>前端信息</span></template>
          <el-descriptions :column="1" border>
            <el-descriptions-item label="前端框架">Vue 3 + TypeScript</el-descriptions-item>
            <el-descriptions-item label="UI 框架">Element Plus</el-descriptions-item>
            <el-descriptions-item label="构建工具">Vite</el-descriptions-item>
            <el-descriptions-item label="浏览器时间">{{ clientTime }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type UploadFile } from 'element-plus'
import { fetchSystemInfo, downloadBackup, uploadRestore, type SystemInfo } from '../../api/modules/system'

const loading = ref(false)
const downloading = ref(false)
const restoring = ref(false)
const clientTime = ref(new Date().toLocaleString('zh-CN'))
const info = reactive<SystemInfo>({
  appName: '-',
  version: '-',
  dotnetVersion: '-',
  os: '-',
  serverTime: '-',
  startTime: '-',
  environment: '-',
})

onMounted(async () => {
  loading.value = true
  try {
    const res = await fetchSystemInfo()
    Object.assign(info, res.data)
  } catch {
    // keep defaults
  } finally {
    loading.value = false
  }
})

const onDownloadBackup = async () => {
  downloading.value = true
  try {
    const blob = await downloadBackup()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `pms-backup-${new Date().toISOString().slice(0, 10)}.db`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('备份下载已开始')
  } catch {
    ElMessage.error('备份下载失败')
  } finally {
    downloading.value = false
  }
}

const onRestoreFileSelected = async (uploadFile: UploadFile) => {
  if (!uploadFile.raw) return
  try {
    await ElMessageBox.confirm(
      '恢复备份将覆盖当前数据，此操作不可撤销。确认继续？',
      '危险操作',
      { type: 'warning', confirmButtonText: '确认恢复', cancelButtonText: '取消' },
    )
  } catch {
    return
  }
  restoring.value = true
  try {
    const res = await uploadRestore(uploadFile.raw)
    ElMessage.success(res.data?.message ?? '恢复成功，请重启服务')
  } catch {
    ElMessage.error('恢复失败，请检查备份文件是否有效')
  } finally {
    restoring.value = false
  }
}
</script>

<style scoped>
.shortcuts {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
</style>
