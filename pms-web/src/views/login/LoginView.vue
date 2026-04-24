<template>
  <div class="login-page">
    <div class="login-shell">
      <section class="hero-panel">
        <div class="hero-topline">PMS Welcome Center</div>

        <div class="hero-brand">
          <div class="hero-logo">
            <span class="hero-logo-core">P</span>
          </div>
          <div>
            <div class="hero-brand-title">PMS 运维项目管理系统</div>
            <div class="hero-brand-subtitle">Project Management Service Platform</div>
          </div>
        </div>

        <div class="hero-title">统一入口，统一权限，统一运维协同工作台</div>
        <div class="hero-description">
          参照企业 welcome 门户的双栏登录体验，集中承载机构接入、密码登录、扫码切换和安全校验，作为项目、巡检、报修、工时与交接流程的统一入口。
        </div>

        <div class="hero-metrics">
          <div class="hero-metric">
            <strong>24h</strong>
            <span>运维事件与报修响应闭环</span>
          </div>
          <div class="hero-metric">
            <strong>4 级</strong>
            <span>角色权限与数据范围隔离</span>
          </div>
          <div class="hero-metric">
            <strong>12+</strong>
            <span>核心业务模块统一门户访问</span>
          </div>
        </div>

        <div class="hero-feature-grid">
          <div class="hero-feature-card">
            <span class="hero-feature-label">Project Coordination</span>
            <strong>项目台账、合同预警和服务交付统一追踪</strong>
          </div>
          <div class="hero-feature-card">
            <span class="hero-feature-label">Service Execution</span>
            <strong>巡检、报修、工时和重大需求在同一入口协同</strong>
          </div>
          <div class="hero-feature-card">
            <span class="hero-feature-label">Access Control</span>
            <strong>机构、角色、权限和路由访问统一编排</strong>
          </div>
          <div class="hero-feature-card">
            <span class="hero-feature-label">Management Ready</span>
            <strong>面向运营、主管和管理者的一体化登录体验</strong>
          </div>
        </div>

        <div class="hero-footer">
          <span class="hero-footer-badge">Blue-Green Enterprise Theme</span>
          <span>推荐使用 Chrome、Edge 等现代浏览器访问</span>
        </div>
      </section>

      <section class="access-panel">
        <button type="button" class="access-switch" @click="toggleMode">
          {{ loginMode === 'password' ? '切换到扫码登录' : '切换到密码登录' }}
        </button>

        <div class="access-panel-inner">
          <div class="access-badge">企业统一入口</div>
          <div class="access-title">{{ loginMode === 'password' ? '密码登录' : '扫码登录' }}</div>
          <div class="access-subtitle">
            {{
              loginMode === 'password'
                ? '请输入机构代码、账号、密码与验证码进入系统'
                : '请使用企业移动端扫描二维码，二维码将定时自动刷新'
            }}
          </div>

          <div class="mode-tabs">
            <button
              type="button"
              class="mode-tab"
              :class="{ 'is-active': loginMode === 'password' }"
              @click="loginMode = 'password'"
            >
              密码登录
            </button>
            <button
              type="button"
              class="mode-tab"
              :class="{ 'is-active': loginMode === 'qr' }"
              @click="loginMode = 'qr'"
            >
              扫码登录
            </button>
          </div>

          <div v-if="loginMode === 'password'" class="password-pane">
            <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="login-form">
              <el-form-item label="机构代码" prop="organizationCode">
                <el-select v-model="form.organizationCode" filterable placeholder="请选择机构代码">
                  <el-option
                    v-for="option in organizationOptions"
                    :key="option.value"
                    :label="option.label"
                    :value="option.value"
                  />
                </el-select>
              </el-form-item>

              <el-form-item label="账号" prop="account">
                <el-input
                  v-model="form.account"
                  clearable
                  autocomplete="username"
                  placeholder="请输入账号"
                  @keyup.enter="onLogin"
                />
              </el-form-item>

              <el-form-item label="密码" prop="password">
                <el-input
                  v-model="form.password"
                  type="password"
                  show-password
                  autocomplete="current-password"
                  placeholder="请输入密码"
                  @keyup.enter="onLogin"
                />
              </el-form-item>

              <el-form-item label="验证码" prop="captcha">
                <div class="captcha-row">
                  <el-input
                    v-model="form.captcha"
                    clearable
                    maxlength="4"
                    placeholder="请输入验证码"
                    @keyup.enter="onLogin"
                  />
                  <button type="button" class="captcha-preview" @click="refreshCaptcha" aria-label="刷新验证码">
                    <span>{{ captchaCode }}</span>
                  </button>
                </div>
              </el-form-item>

              <div class="access-hint-row">
                <span>当前机构：{{ activeOrganizationLabel }}</span>
                <button type="button" class="inline-link" @click="refreshCaptcha">看不清，换一张</button>
              </div>

              <el-alert
                title="登录将沿用现有账号密码认证接口，机构代码与验证码在前端完成交互校验"
                type="info"
                :closable="false"
                class="access-alert"
              />

              <el-button type="primary" :loading="submitLoading" class="login-btn" @click="onLogin">
                登录系统
              </el-button>
            </el-form>
          </div>

          <div v-else class="qr-pane">
            <div class="qr-frame">
              <div class="qr-grid">
                <span
                  v-for="(cell, index) in qrCells"
                  :key="`${qrSeed}-${index}`"
                  class="qr-cell"
                  :class="{ 'is-filled': cell }"
                />
              </div>
              <div class="qr-center-badge">PMS</div>
            </div>

            <div class="qr-title">请使用企业移动端扫码登录</div>
            <div class="qr-subtitle">二维码 {{ qrCountdown }} 秒后刷新，可点击下方按钮立即刷新</div>

            <div class="qr-actions">
              <el-button type="primary" plain @click="refreshQrCode">立即刷新二维码</el-button>
              <el-button @click="loginMode = 'password'">返回密码登录</el-button>
            </div>
          </div>

          <div class="access-footnote">如忘记密码，请联系管理员重置；登录后将按当前角色自动跳转到首个可访问模块。</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
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

const ORGANIZATION_STORAGE_KEY = 'pms-login-organization'
const CAPTCHA_CHARSET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'

const organizationOptions = [
  { label: 'BJGOODWILL / 北极星运维事业部', value: 'bjgoodwill' },
  { label: 'PMS-STANDALONE / 本地演示环境', value: 'pms-standalone' },
]

const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const loginMode = ref<LoginMode>('password')
const captchaCode = ref('')
const qrCountdown = ref(56)
const qrSeed = ref(0)

let qrTimer: number | undefined

const form = reactive({
  organizationCode: 'bjgoodwill',
  account: '',
  password: '',
  captcha: '',
})

const activeOrganizationLabel = computed(() => {
  return organizationOptions.find((option) => option.value === form.organizationCode)?.label ?? '未选择机构'
})

const isFinderCell = (row: number, col: number, top: number, left: number) => {
  const withinRow = row >= top && row <= top + 4
  const withinCol = col >= left && col <= left + 4
  if (!withinRow || !withinCol) {
    return false
  }

  const offsetRow = row - top
  const offsetCol = col - left
  const isBorder = offsetRow === 0 || offsetRow === 4 || offsetCol === 0 || offsetCol === 4
  const isCenter = offsetRow >= 1 && offsetRow <= 3 && offsetCol >= 1 && offsetCol <= 3

  return isBorder || isCenter
}

const isQrFilled = (index: number) => {
  const size = 13
  const row = Math.floor(index / size)
  const col = index % size

  if (
    isFinderCell(row, col, 0, 0) ||
    isFinderCell(row, col, 0, 8) ||
    isFinderCell(row, col, 8, 0)
  ) {
    return true
  }

  const seed = qrSeed.value
  return ((row * 7 + col * 11 + seed * 3) % 5 === 0) || ((row + col + seed) % 7 === 0 && row > 3 && col > 3)
}

const qrCells = computed(() => Array.from({ length: 169 }, (_, index) => isQrFilled(index)))

const rules: FormRules<typeof form> = {
  organizationCode: [{ required: true, message: '请选择机构代码', trigger: 'change' }],
  account: [
    { required: true, message: '请输入账号', trigger: 'blur' },
    { min: 2, max: 64, message: '账号长度需在2到64个字符之间', trigger: 'blur' },
    { pattern: /^[a-z0-9_-]+$/i, message: '账号仅支持字母、数字、下划线或中划线', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 128, message: '密码长度需在6到128个字符之间', trigger: 'blur' },
  ],
  captcha: [
    { required: true, message: '请输入验证码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (!value) {
          callback(new Error('请输入验证码'))
          return
        }

        if (String(value).trim().toUpperCase() !== captchaCode.value) {
          callback(new Error('验证码不正确'))
          return
        }

        callback()
      },
      trigger: 'blur',
    },
  ],
}

const refreshCaptcha = () => {
  captchaCode.value = Array.from({ length: 4 }, () => {
    const index = Math.floor(Math.random() * CAPTCHA_CHARSET.length)
    return CAPTCHA_CHARSET[index]
  }).join('')
  form.captcha = ''
}

const refreshQrCode = () => {
  qrSeed.value += 1
  qrCountdown.value = 56
}

const toggleMode = () => {
  loginMode.value = loginMode.value === 'password' ? 'qr' : 'password'
}

const startQrTimer = () => {
  if (typeof window === 'undefined') {
    return
  }

  qrTimer = window.setInterval(() => {
    qrCountdown.value -= 1
    if (qrCountdown.value <= 0) {
      refreshQrCode()
    }
  }, 1000)
}

const stopQrTimer = () => {
  if (typeof window === 'undefined' || qrTimer == null) {
    return
  }

  window.clearInterval(qrTimer)
  qrTimer = undefined
}

const onLogin = async () => {
  if (submitLoading.value || loginMode.value !== 'password') {
    return
  }

  if (typeof document !== 'undefined' && document.activeElement instanceof HTMLElement) {
    document.activeElement.blur()
  }

  await nextTick()

  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

  submitLoading.value = true
  try {
    clearAuthState()

    const response = await login({
      account: form.account.trim(),
      password: form.password,
    })

    const result = response.data
    setAccessToken(result.accessToken)
    setAuthenticated(true)

    if (typeof window !== 'undefined') {
      window.localStorage.setItem(CURRENT_PERSONNEL_STORAGE_KEY, String(result.personnelId))
      window.localStorage.setItem(ORGANIZATION_STORAGE_KEY, form.organizationCode)
    }

    access.setCurrentPersonnelId(result.personnelId)
    await access.ensureAccessProfileLoaded(true)

    ElMessage.success('登录成功')
    await router.replace(access.getFirstAccessiblePath())
  } catch (error) {
    setAuthenticated(false)
    refreshCaptcha()
    ElMessage.error(getErrorMessage(error, '登录失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}

watch(loginMode, (mode) => {
  if (mode === 'qr') {
    refreshQrCode()
    return
  }

  formRef.value?.clearValidate(['captcha'])
})

onMounted(() => {
  if (typeof window !== 'undefined') {
    const savedOrganization = window.localStorage.getItem(ORGANIZATION_STORAGE_KEY)
    if (savedOrganization && organizationOptions.some((option) => option.value === savedOrganization)) {
      form.organizationCode = savedOrganization
    }
  }

  refreshCaptcha()
  startQrTimer()
})

onBeforeUnmount(() => {
  stopQrTimer()
})
</script>

<style scoped>
.login-page {
  --login-brand-blue: #0066b2;
  --login-brand-green: #4ca591;
  --login-brand-navy: #0e2e4f;
  --login-text-main: #18324d;
  --login-text-secondary: #68839d;
  --login-border: rgba(25, 81, 129, 0.12);

  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 28px;
  overflow: hidden;
  background:
    radial-gradient(circle at 0 0, rgba(0, 102, 178, 0.12), transparent 30%),
    radial-gradient(circle at 100% 100%, rgba(76, 165, 145, 0.12), transparent 32%),
    linear-gradient(180deg, #eef4f8 0%, #e8f0f6 100%);
}

.login-page::before {
  content: '';
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.3) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.3) 1px, transparent 1px);
  background-size: 56px 56px;
  opacity: 0.45;
  pointer-events: none;
}

.login-shell {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(448px, 0.78fr);
  width: min(1148px, 92vw);
  overflow: hidden;
  border-radius: 28px;
  background: rgba(255, 255, 255, 0.84);
  border: 1px solid rgba(255, 255, 255, 0.7);
  box-shadow: 0 36px 80px rgba(18, 46, 73, 0.18);
  backdrop-filter: blur(14px);
}

.hero-panel {
  position: relative;
  padding: 40px 42px 34px;
  color: #ffffff;
  background:
    radial-gradient(circle at 18% 18%, rgba(112, 194, 255, 0.22), transparent 22%),
    radial-gradient(circle at 88% 82%, rgba(87, 230, 206, 0.18), transparent 26%),
    linear-gradient(145deg, #0f365d 0%, #0a5b99 48%, #0b7f81 100%);
}

.hero-panel::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.08), transparent 32%);
  pointer-events: none;
}

.hero-topline {
  position: relative;
  z-index: 1;
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.16);
  background: rgba(255, 255, 255, 0.08);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.hero-brand {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 22px;
}

.hero-logo {
  position: relative;
  width: 62px;
  height: 62px;
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.3), rgba(255, 255, 255, 0.12));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.4), 0 16px 32px rgba(0, 35, 70, 0.18);
}

.hero-logo::before,
.hero-logo::after {
  content: '';
  position: absolute;
  border-radius: 50%;
}

.hero-logo::before {
  inset: 9px;
  background: linear-gradient(135deg, #43b9ff 0%, #0f78d9 100%);
}

.hero-logo::after {
  width: 18px;
  height: 18px;
  right: 8px;
  bottom: 8px;
  background: linear-gradient(135deg, #56d8bb 0%, #2ea184 100%);
  box-shadow: 0 4px 10px rgba(54, 174, 145, 0.28);
}

.hero-logo-core {
  position: absolute;
  inset: 0;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  font-weight: 800;
  letter-spacing: 0.04em;
}

.hero-brand-title {
  font-size: 28px;
  font-weight: 700;
}

.hero-brand-subtitle {
  margin-top: 4px;
  color: rgba(231, 242, 252, 0.78);
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.hero-title {
  position: relative;
  z-index: 1;
  margin-top: 36px;
  max-width: 520px;
  font-size: 38px;
  line-height: 1.18;
  font-weight: 700;
}

.hero-description {
  position: relative;
  z-index: 1;
  margin-top: 14px;
  max-width: 540px;
  color: rgba(232, 244, 255, 0.82);
  font-size: 15px;
  line-height: 1.85;
}

.hero-metrics {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 26px;
}

.hero-metric {
  padding: 16px 14px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.08);
}

.hero-metric strong {
  display: block;
  font-size: 28px;
  font-weight: 700;
}

.hero-metric span {
  display: block;
  margin-top: 8px;
  font-size: 12px;
  line-height: 1.7;
  color: rgba(235, 244, 255, 0.78);
}

.hero-feature-grid {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 26px;
}

.hero-feature-card {
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.hero-feature-label {
  display: block;
  margin-bottom: 8px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgba(220, 237, 255, 0.72);
}

.hero-feature-card strong {
  display: block;
  font-size: 14px;
  line-height: 1.7;
}

.hero-footer {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-top: 24px;
  color: rgba(233, 243, 255, 0.76);
  font-size: 12px;
}

.hero-footer-badge {
  display: inline-flex;
  align-items: center;
  padding: 7px 11px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  font-weight: 700;
}

.access-panel {
  position: relative;
  display: flex;
  justify-content: center;
  padding: 28px 28px 28px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98), rgba(246, 250, 252, 0.96));
}

.access-panel::before {
  content: '';
  position: absolute;
  top: -34px;
  right: -24px;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(76, 165, 145, 0.16), transparent 64%);
}

.access-switch {
  position: absolute;
  top: 20px;
  right: 18px;
  z-index: 1;
  padding: 8px 12px;
  border: 1px solid rgba(0, 102, 178, 0.12);
  border-radius: 999px;
  background: #ffffff;
  color: var(--login-brand-blue);
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.access-panel-inner {
  position: relative;
  z-index: 1;
  width: min(100%, 426px);
  margin-top: 14px;
}

.access-switch:hover {
  border-color: rgba(0, 102, 178, 0.24);
  background: #f5fbff;
}

.access-badge {
  position: relative;
  z-index: 1;
  display: inline-flex;
  align-items: center;
  padding: 7px 11px;
  border-radius: 999px;
  background: linear-gradient(180deg, rgba(239, 248, 255, 0.98), rgba(245, 251, 255, 0.94));
  border: 1px solid rgba(0, 102, 178, 0.12);
  color: var(--login-brand-blue);
  font-size: 12px;
  font-weight: 700;
}

.access-title {
  position: relative;
  z-index: 1;
  margin-top: 20px;
  font-size: 28px;
  line-height: 1.12;
  font-weight: 700;
  color: var(--login-text-main);
}

.access-subtitle {
  position: relative;
  z-index: 1;
  margin-top: 12px;
  color: var(--login-text-secondary);
  font-size: 14px;
  line-height: 1.65;
}

.mode-tabs {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  margin-top: 24px;
  padding: 5px;
  border-radius: 17px;
  background: linear-gradient(180deg, #edf4fb 0%, #e4eef8 100%);
}

.mode-tab {
  min-height: 44px;
  border: none;
  border-radius: 14px;
  background: transparent;
  color: #4d6984;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.mode-tab.is-active {
  background: linear-gradient(135deg, var(--login-brand-blue) 0%, var(--login-brand-green) 100%);
  color: #ffffff;
  box-shadow: 0 14px 26px rgba(0, 102, 178, 0.18);
}

.password-pane,
.qr-pane {
  position: relative;
  z-index: 1;
  margin-top: 22px;
}

.login-form :deep(.el-form-item) {
  margin-bottom: 18px;
}

.login-form :deep(.el-form-item__label) {
  color: #1d3753;
  font-size: 12px;
  font-weight: 700;
  line-height: 1.2;
  padding-bottom: 8px;
}

.login-form :deep(.el-input__wrapper),
.login-form :deep(.el-select__wrapper) {
  min-height: 48px;
  border-radius: 14px;
  background: linear-gradient(180deg, rgba(247, 251, 254, 0.98), rgba(241, 247, 251, 0.94));
  box-shadow:
    inset 0 0 0 1px rgba(25, 81, 129, 0.1),
    0 1px 0 rgba(255, 255, 255, 0.7);
}

.login-form :deep(.el-input__wrapper.is-focus),
.login-form :deep(.el-select__wrapper.is-focused) {
  background: #ffffff;
  box-shadow:
    inset 0 0 0 1px rgba(0, 102, 178, 0.28),
    0 0 0 4px rgba(0, 102, 178, 0.08);
}

.captcha-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 120px;
  gap: 12px;
  width: 100%;
}

.captcha-preview {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  min-height: 48px;
  border: 1px solid rgba(0, 102, 178, 0.14);
  border-radius: 14px;
  background:
    linear-gradient(135deg, rgba(233, 246, 255, 0.98), rgba(245, 251, 255, 0.96)),
    repeating-linear-gradient(135deg, rgba(0, 102, 178, 0.05) 0 6px, transparent 6px 12px);
  color: var(--login-brand-blue);
  font-size: 20px;
  font-weight: 800;
  letter-spacing: 0.18em;
  cursor: pointer;
}

.captcha-preview span {
  position: relative;
  z-index: 1;
}

.captcha-preview::before,
.captcha-preview::after {
  content: '';
  position: absolute;
  inset: auto;
  width: 140%;
  height: 2px;
  left: -20%;
  background: rgba(76, 165, 145, 0.26);
  transform: rotate(-10deg);
}

.captcha-preview::before {
  top: 18px;
}

.captcha-preview::after {
  bottom: 18px;
  transform: rotate(8deg);
}

.access-hint-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin: 2px 0 16px;
  color: var(--login-text-secondary);
  font-size: 12px;
}

.inline-link {
  border: none;
  background: transparent;
  color: var(--login-brand-blue);
  cursor: pointer;
  font-weight: 700;
}

.access-alert {
  margin-bottom: 16px;
}

.login-form :deep(.el-alert) {
  border-color: #d7e9f5;
  background: linear-gradient(180deg, #eff7fc 0%, #f6fbfe 100%);
  border-radius: 14px;
}

.login-form :deep(.el-alert__description) {
  line-height: 1.65;
}

.login-btn {
  width: 100%;
  min-height: 50px;
  border-radius: 14px;
  font-size: 15px;
  font-weight: 700;
  background: linear-gradient(135deg, var(--login-brand-blue) 0%, var(--login-brand-green) 100%) !important;
  border: none !important;
  box-shadow: 0 20px 34px rgba(0, 102, 178, 0.2);
}

.qr-pane {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding-top: 8px;
}

.qr-frame {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 264px;
  height: 264px;
  padding: 22px;
  border-radius: 28px;
  background: linear-gradient(180deg, #ffffff 0%, #f6fbfe 100%);
  border: 1px solid rgba(0, 102, 178, 0.12);
  box-shadow: 0 22px 42px rgba(17, 65, 105, 0.12);
}

.qr-grid {
  display: grid;
  grid-template-columns: repeat(13, 1fr);
  gap: 3px;
  width: 100%;
  height: 100%;
}

.qr-cell {
  border-radius: 2px;
  background: rgba(22, 50, 77, 0.05);
}

.qr-cell.is-filled {
  background: linear-gradient(135deg, #123456 0%, #0071b8 100%);
}

.qr-center-badge {
  position: absolute;
  width: 62px;
  height: 62px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--login-brand-blue) 0%, var(--login-brand-green) 100%);
  color: #ffffff;
  font-size: 18px;
  font-weight: 800;
  box-shadow: 0 12px 24px rgba(0, 102, 178, 0.18);
}

.qr-title {
  margin-top: 20px;
  font-size: 18px;
  font-weight: 700;
  color: var(--login-text-main);
}

.qr-subtitle {
  margin-top: 8px;
  color: var(--login-text-secondary);
  font-size: 13px;
  line-height: 1.7;
}

.qr-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-top: 20px;
}

.access-footnote {
  position: relative;
  z-index: 1;
  margin-top: 18px;
  text-align: left;
  color: #6f8398;
  font-size: 12px;
  line-height: 1.7;
}

@media (max-width: 1040px) {
  .login-shell {
    grid-template-columns: 1fr;
    width: min(620px, 94vw);
  }

  .hero-metrics,
  .hero-feature-grid {
    grid-template-columns: 1fr;
  }

  .hero-footer {
    flex-direction: column;
    align-items: flex-start;
  }

  .access-panel {
    padding: 28px 24px 26px;
  }

  .access-panel-inner {
    width: min(100%, 460px);
    margin-top: 12px;
  }
}

@media (max-width: 576px) {
  .login-page {
    padding: 14px;
  }

  .hero-panel,
  .access-panel {
    padding: 26px 20px 24px;
  }

  .access-panel-inner {
    width: 100%;
    margin-top: 28px;
  }

  .hero-brand-title {
    font-size: 24px;
  }

  .hero-title {
    font-size: 30px;
  }

  .access-title {
    font-size: 26px;
  }

  .captcha-row {
    grid-template-columns: 1fr;
  }

  .qr-frame {
    width: min(100%, 250px);
    height: auto;
    aspect-ratio: 1;
  }

  .qr-actions,
  .access-hint-row {
    flex-direction: column;
    align-items: stretch;
  }

  .access-footnote {
    text-align: center;
  }
}
</style>
