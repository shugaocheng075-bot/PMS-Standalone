<template>
  <div class="login-page">
    <div class="login-card">
      <div class="title">项目管理平台</div>
      <div class="subtitle">请输入账号信息登录系统</div>

      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="login-form">
        <el-form-item label="账号" prop="account">
          <el-input
            v-model="form.account"
            clearable
            placeholder="请输入账号（姓名拼音）"
            @keyup.enter="onLogin"
          />
        </el-form-item>

        <el-form-item label="密码" prop="password">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            placeholder="请输入密码"
            @keyup.enter="onLogin"
          />
        </el-form-item>

        <el-alert
          title="默认账号为姓名拼音，初始密码为 123456"
          type="info"
          :closable="false"
          class="hint"
        />

        <el-button type="primary" :loading="submitLoading" class="login-btn" @click="onLogin">登录</el-button>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
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

const router = useRouter()
const access = useAccessControl()

const formRef = ref<FormInstance>()
const submitLoading = ref(false)

const form = reactive({
  account: '',
  password: '',
})

const rules: FormRules<typeof form> = {
  account: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

const submitLogin = async () => {
  const payload = {
    account: form.account.trim(),
    password: form.password,
  }

  try {
    return await login(payload)
  } catch (error) {
    const statusCode = axios.isAxiosError(error) ? error.response?.status : undefined
    const message = getErrorMessage(error, '')
    const shouldRetry = statusCode === 401 || message.includes('账号或密码错误')

    if (!shouldRetry) {
      throw error
    }

    await nextTick()
    return login(payload)
  }
}

const onLogin = async () => {
  if (submitLoading.value) {
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

    const response = await submitLogin()

    const result = response.data
    setAccessToken(result.accessToken)
    setAuthenticated(true)

    window.localStorage.setItem(CURRENT_PERSONNEL_STORAGE_KEY, String(result.personnelId))
    access.setCurrentPersonnelId(result.personnelId)
    await access.ensureAccessProfileLoaded(true)

    ElMessage.success('登录成功')
    await router.replace(access.getFirstAccessiblePath())
  } catch (error) {
    setAuthenticated(false)
    ElMessage.error(getErrorMessage(error, '登录失败，请稍后重试'))
  } finally {
    submitLoading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background:
    radial-gradient(circle at 10% 10%, rgba(10, 121, 190, 0.15) 0, transparent 35%),
    radial-gradient(circle at 90% 0, rgba(76, 165, 145, 0.16) 0, transparent 40%),
    #f4f8fd;
  padding: 24px;
}

.login-card {
  position: relative;
  width: min(420px, 90vw);
  background: #fff;
  border-radius: 8px;
  border: 1px solid #d7e6f3;
  box-shadow: 0 8px 24px rgba(27, 89, 143, 0.08);
  padding: 28px;
}

.login-card::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 4px;
  border-radius: 8px 8px 0 0;
  background: linear-gradient(90deg, #0066b2 0%, #27b294 100%);
}

.title {
  font-size: 22px;
  font-weight: 700;
  color: #1f4f7a;
}

.subtitle {
  margin-top: 6px;
  color: #5c7898;
  font-size: 13px;
}

.login-form {
  margin-top: 20px;
}

.hint {
  margin: 8px 0 16px;
}

.login-form :deep(.el-form-item__label) {
  color: #2c4d7f;
  font-size: 12px;
}

.login-form :deep(.el-input__wrapper) {
  min-height: 34px;
  border-radius: 4px;
}

.login-form :deep(.el-alert) {
  border-color: #d8eaf8;
  background: #eef7ff;
}

.login-btn {
  width: 100%;
  min-height: 34px;
  border-radius: 4px;
}
</style>
