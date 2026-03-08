<template>
  <div class="page-shell">
    <div class="page-head">
      <h2 class="page-title">个人资料</h2>
    </div>

    <el-row :gutter="20">
      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>基本信息</span></template>
          <el-descriptions :column="1" border>
            <el-descriptions-item label="姓名">{{ profile?.personnelName ?? '-' }}</el-descriptions-item>
            <el-descriptions-item label="角色类型">{{ profile?.roleType ?? '-' }}</el-descriptions-item>
            <el-descriptions-item label="系统角色">{{ profile?.systemRole ?? '-' }}</el-descriptions-item>
            <el-descriptions-item label="上级">{{ profile?.supervisorName ?? '无' }}</el-descriptions-item>
            <el-descriptions-item label="管理员">
              <el-tag :type="profile?.isAdmin ? 'success' : 'info'" size="small">
                {{ profile?.isAdmin ? '是' : '否' }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="权限数量">{{ profile?.permissions?.length ?? 0 }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>

      <el-col :span="12">
        <el-card shadow="never">
          <template #header><span>修改密码</span></template>
          <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" style="max-width: 400px">
            <el-form-item label="原密码" prop="oldPassword">
              <el-input v-model="form.oldPassword" type="password" show-password />
            </el-form-item>
            <el-form-item label="新密码" prop="newPassword">
              <el-input v-model="form.newPassword" type="password" show-password />
            </el-form-item>
            <el-form-item label="确认密码" prop="confirmPassword">
              <el-input v-model="form.confirmPassword" type="password" show-password />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="submitting" @click="onChangePassword">修改密码</el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" style="margin-top: 20px">
      <template #header><span>数据范围</span></template>
      <el-descriptions :column="1" border>
        <el-descriptions-item label="范围类型">
          {{ scopeLabel }}
        </el-descriptions-item>
        <el-descriptions-item label="可访问医院">
          <template v-if="hospitalNames.length">
            <el-tag v-for="h in hospitalNames" :key="h" size="small" style="margin: 2px 4px">{{ h }}</el-tag>
          </template>
          <span v-else>全部（不限制）</span>
        </el-descriptions-item>
      </el-descriptions>
    </el-card>

    <el-card shadow="never" style="margin-top: 20px">
      <template #header><span>已授权权限</span></template>
      <div class="permission-tags">
        <el-tag v-for="p in profile?.permissions ?? []" :key="p" size="small" style="margin: 2px 4px">{{ p }}</el-tag>
        <span v-if="!profile?.permissions?.length">暂无权限</span>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { ElMessage } from 'element-plus'
import { useAccessControl } from '../../composables/useAccessControl'
import { changePassword } from '../../api/modules/auth'
import { getErrorMessage } from '../../utils/error'

const { accessProfile } = useAccessControl()
const profile = computed(() => accessProfile.value)

const hospitalNames = computed(() => profile.value?.dataScope?.accessibleHospitalNames ?? [])
const scopeLabel = computed(() => {
  const t = profile.value?.dataScope?.scopeType
  if (t === 'own') return '仅本人'
  if (t === 'subordinates') return '含下属'
  return '全部'
})

const formRef = ref<FormInstance>()
const submitting = ref(false)
const form = reactive({ oldPassword: '', newPassword: '', confirmPassword: '' })

const rules: FormRules = {
  oldPassword: [{ required: true, message: '请输入原密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码不少于6位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    {
      validator: (_r: unknown, v: string, cb: (err?: Error) => void) => {
        v === form.newPassword ? cb() : cb(new Error('两次密码不一致'))
      },
      trigger: 'blur',
    },
  ],
}

const onChangePassword = async () => {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    await changePassword({ oldPassword: form.oldPassword, newPassword: form.newPassword })
    ElMessage.success('密码修改成功')
    form.oldPassword = ''
    form.newPassword = ''
    form.confirmPassword = ''
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '密码修改失败'))
  } finally {
    submitting.value = false
  }
}
</script>
