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
          title="默认账号为姓名拼音，初始密码为 123456；测试管理员账号：admin / 123456。"
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
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { login } from '../../api/modules/auth'
import { useAccessControl } from '../../composables/useAccessControl'
import {
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

const onLogin = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) {
    return
  }

  submitLoading.value = true
  try {
    const response = await login({
      account: form.account.trim(),
      password: form.password,
    })

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
  background: #f1f4fb;
  padding: 24px;
}

.login-card {
  width: 420px;
  max-width: 100%;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(47, 58, 150, 0.12);
  padding: 28px;
}

.title {
  font-size: 24px;
  font-weight: 700;
  color: #1f2a69;
}

.subtitle {
  margin-top: 6px;
  color: #5b6585;
  font-size: 13px;
}

.login-form {
  margin-top: 20px;
}

.hint {
  margin: 8px 0 16px;
}

.login-btn {
  width: 100%;
}
</style>
