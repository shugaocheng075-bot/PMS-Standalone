<template>
  <div class="login-page">
    <div class="login-shell">
      <div class="login-box">
        <div class="login-header">
          <div class="login-logo-circle">
            <span class="login-logo-text">PMS</span>
          </div>
          <h2 class="login-title">登录运维项目管理系统</h2>
          <p class="login-subtitle">
            {{ loginMode === 'password' ? '使用企业账号继续' : '使用企业移动端扫码' }}
          </p>
        </div>

        <div v-if="loginMode === 'password'" class="login-form-area">
          <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="apple-form">
            

            <el-form-item prop="account">
              <el-input v-model="form.account" clearable autocomplete="username" placeholder="账号" @keyup.enter="onLogin" class="apple-input" />
            </el-form-item>

            <el-form-item prop="password">
              <el-input v-model="form.password" type="password" show-password autocomplete="current-password" placeholder="密码" @keyup.enter="onLogin" class="apple-input" />
            </el-form-item>

            <div class="login-actions">
              <button type="button" :disabled="submitLoading" class="apple-btn-primary" @click="onLogin">
                <span v-if="submitLoading" class="loading-spinner"></span>
                继续
              </button>
            </div>
          </el-form>
        </div>

        <div v-else class="qr-area">
          <div class="qr-frame">
            <div class="qr-grid">
              <span v-for="(cell, index) in qrCells" :key="index" class="qr-cell" :class="{ 'is-filled': cell }" />
            </div>
            <div class="qr-center-badge">PMS</div>
          </div>
          <div class="qr-actions">
            <button type="button" class="apple-btn-secondary" @click="refreshQrCode">刷新二维码</button>
          </div>
        </div>

        <div class="login-footer">
          <button type="button" class="apple-link" @click="toggleMode">
            {{ loginMode === 'password' ? '改用扫码登录' : '改用密码登录' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { login } from '../../api/modules/auth'
import { useAccessControl } from '../../composables/useAccessControl'
import {
  clearAuthState,
  CURRENT_PERSONNEL_STORAGE_KEY,
  setAccessToken,
  setAuthenticated,
} from '../../constants/access'
import { getErrorMessage } from '../../utils/error'

type LoginMode = 'password' | 'qr'

const router = useRouter()
const access = useAccessControl()



const loginMode = ref<LoginMode>('password')



const qrSeed = ref(Date.now())
const qrCountdown = ref(60)
let qrTimer: ReturnType<typeof setInterval> | null = null

const generateMockQrMatrix = () => {
  const size = 13
  const matrix = Array(size * size).fill(false)
  for (let i = 0; i < size; i++) {
    for (let j = 0; j < size; j++) {
      if (
        (i < 3 && j < 3) ||
        (i < 3 && j >= size - 3) ||
        (i >= size - 3 && j < 3)
      ) {
        matrix[i * size + j] = true
        continue
      }
      if (Math.random() > 0.45) {
        matrix[i * size + j] = true
      }
    }
  }
  matrix[size * 6 + 6] = false
  return matrix
}

const qrCells = ref<boolean[]>(generateMockQrMatrix())

const refreshQrCode = () => {
  qrSeed.value = Date.now()
  qrCells.value = generateMockQrMatrix()
  qrCountdown.value = 60
  ElMessage.success('二维码已更新，请重新扫码')
}

watch(loginMode, (newMode) => {
  if (newMode === 'qr') {
    qrCountdown.value = 60
    qrTimer = setInterval(() => {
      qrCountdown.value--
      if (qrCountdown.value <= 0) {
        refreshQrCode()
      }
    }, 1000)
  } else {
    if (qrTimer) clearInterval(qrTimer)
  }
})

const toggleMode = () => {
  loginMode.value = loginMode.value === 'password' ? 'qr' : 'password'
}

const formRef = ref<FormInstance>()
const submitLoading = ref(false)

const form = reactive({
  
  account: '',
  password: ''
})

const rules = reactive<FormRules>({
  
  account: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
})

const onLogin = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    
    submitLoading.value = true
    try {
      clearAuthState()
      

      const res = await login({
        // organizationCode: form.organizationCode,
        account: form.account,
        password: form.password
      })

      if (res.code === 200 && res.data) {
        const payload = res.data as any
        const accessToken = payload.accessToken || payload.token || ''
        const personnelId = Number(payload.personnelId) || 1

        setAccessToken(accessToken)
        localStorage.setItem(CURRENT_PERSONNEL_STORAGE_KEY, String(personnelId))
        access.setCurrentPersonnelId(personnelId)
        await access.ensureAccessProfileLoaded(true)
        setAuthenticated(true)

        ElMessage.success(`欢迎回来，${payload.personnelName || payload.username || '用户'}`)

        nextTick(() => {
          router.push(access.getFirstAccessiblePath())
        })
      } else {
        ElMessage.error(res.message || '登录失败，请检查账号密码')
      }
    } catch (error: any) {
      ElMessage.error(getErrorMessage(error, '登录请求异常，请稍后重试'))
    } finally {
      submitLoading.value = false
    }
  })
}

onMounted(() => {
})

onBeforeUnmount(() => {
  if (qrTimer) clearInterval(qrTimer)
})
</script>

<style scoped>
.login-page {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: #f5f5f7;
  font-family: -apple-system, BlinkMacSystemFont, "SF Pro Text", "Helvetica Neue", Arial, sans-serif;
}

.login-shell {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 400px;
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.04), 0 1px 2px rgba(0, 0, 0, 0.02);
  padding: 40px 36px;
  display: flex;
  flex-direction: column;
}

.login-header {
  text-align: center;
  margin-bottom: 32px;
}

.login-logo-circle {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: #0071e3;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  font-weight: 700;
  margin: 0 auto 20px;
  letter-spacing: 1px;
}

.login-title {
  font-size: 24px;
  font-weight: 600;
  color: #1d1d1f;
  margin: 0 0 8px;
}

.login-subtitle {
  font-size: 15px;
  color: #86868b;
  margin: 0;
}

.apple-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.apple-form :deep(.el-form-item) {
  margin-bottom: 0;
}

.apple-form :deep(.el-input__wrapper) {
  box-shadow: none !important;
  background: #f0f0f5;
  border-radius: 12px;
  padding: 0 16px;
  height: 48px;
  transition: background 0.2s, box-shadow 0.2s;
}

.apple-form :deep(.el-input__wrapper.is-focus) {
  background: #ffffff;
  box-shadow: 0 0 0 2px #0071e3 !important;
}

.apple-form :deep(.el-input__inner) {
  color: #1d1d1f;
  font-size: 17px;
  height: 100%;
}

.apple-form :deep(.el-input__inner::placeholder) {
  color: #86868b;
}

.apple-form :deep(.el-select) {
  width: 100%;
}

.captcha-row {
  display: flex;
  gap: 12px;
  align-items: center;
}

.captcha-preview {
  height: 48px;
  padding: 0 20px;
  border-radius: 12px;
  border: none;
  background: #f0f0f5;
  color: #1d1d1f;
  font-size: 18px;
  font-weight: 600;
  letter-spacing: 4px;
  cursor: pointer;
  transition: background 0.2s;
}

.captcha-preview:hover {
  background: #e5e5ea;
}

.login-actions {
  margin-top: 16px;
}

.apple-btn-primary {
  width: 100%;
  height: 48px;
  border-radius: 12px;
  background: #0071e3;
  color: #ffffff;
  font-size: 17px;
  font-weight: 500;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, transform 0.1s;
}

.apple-btn-primary:hover:not(:disabled) {
  background: #0077ed;
}

.apple-btn-primary:active:not(:disabled) {
  transform: scale(0.98);
}

.apple-btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-right: 8px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.qr-area {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 24px;
}

.qr-frame {
  position: relative;
  padding: 20px;
  background: #ffffff;
  border-radius: 24px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.06);
}

.qr-grid {
  display: grid;
  grid-template-columns: repeat(13, 16px);
  grid-template-rows: repeat(13, 16px);
  gap: 1px;
}

.qr-cell {
  background: transparent;
  border-radius: 3px;
}

.qr-cell.is-filled {
  background: #1d1d1f;
}

.qr-center-badge {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background: #ffffff;
  padding: 8px;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 700;
  color: #0071e3;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.apple-btn-secondary {
  height: 48px;
  padding: 0 24px;
  border-radius: 12px;
  background: #f0f0f5;
  color: #1d1d1f;
  font-size: 15px;
  font-weight: 500;
  border: none;
  cursor: pointer;
  transition: background 0.2s;
}

.apple-btn-secondary:hover {
  background: #e5e5ea;
}

.login-footer {
  margin-top: 32px;
  text-align: center;
  border-top: 1px solid #e5e5ea;
  padding-top: 24px;
}

.apple-link {
  background: none;
  border: none;
  color: #0071e3;
  font-size: 14px;
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.apple-link:hover {
  text-decoration: none;
}
</style>
