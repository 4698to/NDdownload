<template>
  <v-dialog v-model="internalOpen" persistent max-width="640">
    <v-card>
      <v-card-title class="d-flex align-center">
        <span>{{ titleText }}</span>
        <v-spacer />
        <v-btn
          icon="mdi-help-circle-outline"
          variant="text"
          size="small"
          color="info"
          @click="showHelp = true"
          title="帮助"
        />
      </v-card-title>
      <v-card-text>
        <v-form ref="formRef" v-model="isValid" validate-on="blur">
          <v-row>
            <v-col cols="12" >
              <v-text-field
                v-model="form.name"
                label="名称"
                :rules="[rules.required]"
                density="comfortable"
                clearable
              />
            </v-col>
            <v-col cols="6">
              <v-select
                v-model="form.seriesMin"
                :items="yearOptions"
                label="兼容最小 3dsMax 版本"
                :rules="[rules.required, rules.minNotGreaterThanMax]"
                density="comfortable"
                clearable
              />
            </v-col>
            <v-col cols="6">
              <v-select
                v-model="form.seriesMax"
                :items="yearOptions"
                label="兼容最大 3dsMax 版本"
                :rules="[rules.required, rules.maxNotLessThanMin]"
                density="comfortable"
                clearable
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.helpUrl"
                label="帮助链接"
                placeholder="https://..."
                type="url"
                density="comfortable"
                clearable
              />
            </v-col>

            <v-col cols="12">
              <v-text-field
                v-model="form.contact"
                label="联系方式"
                placeholder="邮箱、QQ、微信等"
                density="comfortable"
                clearable
              />
            </v-col>

            <v-col cols="12">
              <v-textarea
                v-model="form.description"
                label="说明"
                rows="3"
                auto-grow
                clearable
              />
            </v-col>

            <v-col cols="12">
              <v-file-input
                v-model="form.file"
                label="选择文件"
                :rules="[rules.requiredFile]"
                show-size
                prepend-icon="mdi-file-upload"
                accept=".zip,.7z,.rar,.json,.txt,*/*"
                density="comfortable"
              />
            </v-col>
          </v-row>
          
          <!-- 上传进度和消息 -->
          <v-row v-if="isUploading || showUploadMessage">
            <v-col cols="12">
              <v-progress-linear
                v-if="isUploading"
                v-model="uploadProgress"
                color="primary"
                height="8"
                rounded
              />
              <v-alert
                v-if="showUploadMessage"
                :type="uploadMessage.includes('成功') ? 'success' : 'error'"
                variant="tonal"
                class="mt-3"
              >
                {{ uploadMessage }}
              </v-alert>
            </v-col>
          </v-row>
        </v-form>
        <v-label >
          需等待审核通过后，才能在工具树中显示，通常需要2-3天。
        </v-label>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="onCancel">取消</v-btn>
        <v-btn 
          :disabled="!isValid || isUploading" 
          color="primary" 
          @click="onSubmit"
          :loading="isUploading"
        >
          {{ isUploading ? '上传中...' : '提交' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <!-- 帮助对话框 -->
  <v-dialog v-model="showHelp" max-width="600">
    <v-card>
      <v-card-title class="d-flex align-center">
        <v-icon icon="mdi-help-circle" color="info" class="mr-2" />
        上传帮助
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" @click="showHelp = false" />
      </v-card-title>
      <v-card-text>
        <div class="help-content">
          <h3 class="text-h6 mb-3">如何上传工具？</h3>
          
          <div class="mb-4">
            <h4 class="text-subtitle-1 font-weight-bold mb-2">1. 基本信息填写</h4>
            <ul class="ml-4">
              <li><strong>名称：</strong>工具的名称，请使用中文描述</li>
              <li><strong>兼容版本：</strong>选择工具支持的3dsMax版本范围</li>
              <li><strong>说明：</strong>详细描述工具的功能和使用方法</li>
              <li><strong>帮助链接：</strong>可选的帮助文档链接</li>
              <li><strong>联系方式：</strong>您的邮箱、QQ、微信等联系方式，方便我们与您沟通</li>
            </ul>
          </div>

          <div class="mb-4">
            <h4 class="text-subtitle-1 font-weight-bold mb-2">2. 文件要求</h4>
            <ul class="ml-4">
              <li>支持格式：ZIP、7Z、RAR、JSON、TXT等</li>
              <li>文件大小：建议不超过60MB</li>
              <li>内容要求：确保文件包含完整的工具安装包</li>
            </ul>
          </div>

          <div class="mb-4">
            <h4 class="text-subtitle-1 font-weight-bold mb-2">3. 审核流程</h4>
            <ul class="ml-4">
              <li>提交后需要等待管理员审核</li>
              <li>审核时间通常为2-3个工作日</li>
              <li>审核通过后工具将在工具树中显示</li>
              <li>如有问题，我们会通过邮件联系您</li>
            </ul>
          </div>

          <div class="mb-4">
            <h4 class="text-subtitle-1 font-weight-bold mb-2">4. 注意事项</h4>
            <ul class="ml-4">
              <li>请确保工具的安全性，避免包含恶意代码</li>
              <li>工具描述要准确详细，便于用户理解</li>
              <li>版本兼容性信息要准确，避免用户安装后无法使用</li>
            </ul>
          </div>

          <v-alert
            type="info"
            variant="tonal"
            class="mt-4"
          >
            <strong>需要帮助？</strong> 如果您在上传过程中遇到问题，请联系 738746223@qq.com。
          </v-alert>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn color="primary" @click="showHelp = false">我知道了</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

type UploadForm = {
  name: string
  seriesMin: number | null
  seriesMax: number | null
  description: string
  helpUrl: string
  contact: string
  file: File | null
}

type UploadResult = {
  success: boolean
  message: string
}

const props = defineProps<{
  modelValue: boolean
  title?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'submit', payload: UploadForm, callback: (result: UploadResult) => void): void
  (e: 'cancel'): void
}>()

const internalOpen = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
})

const titleText = computed(() => props.title ?? '上传文件并填写信息')

const formRef = ref()
const isValid = ref(false)
const isUploading = ref(false)
const uploadProgress = ref(0)
const uploadMessage = ref('')
const showUploadMessage = ref(false)
const showHelp = ref(false)

const form = reactive<UploadForm>({
  name: '',
  seriesMin: 2015,
  seriesMax: 2025,
  description: '',
  helpUrl: '',
  contact: '',
  file: null,
})

const yearOptions = computed(() => {
  const start = 2015
  const end = 2025
  const list: number[] = []
  for (let y = end; y >= start; y -= 1) list.push(y)
  return list
})

const rules = {
  required: (v: any) => (v !== undefined && v !== null && String(v).trim().length > 0) || '必填项',
  requiredFile: (v: File | null) => !!v || '请选择文件',
  minNotGreaterThanMax: () => {
    const min = form.seriesMin ?? 0
    const max = form.seriesMax ?? 0
    return min <= max || '最小版本不能大于最大版本'
  },
  maxNotLessThanMin: () => {
    const min = form.seriesMin ?? 0
    const max = form.seriesMax ?? 0
    return max >= min || '最大版本不能小于最小版本'
  },
}

watch(internalOpen, (open) => {
  if (!open) return
  // 打开时重置校验状态
  isValid.value = false
  // 重置上传状态
  isUploading.value = false
  uploadProgress.value = 0
  showUploadMessage.value = false
  uploadMessage.value = ''
})

function onCancel() {
  internalOpen.value = false
  emit('cancel')
}

async function onSubmit() {
  // 触发校验
  // @ts-ignore Vuetify form has validate method at runtime
  await formRef.value?.validate()
  if (!isValid.value) return
  
  try {
    isUploading.value = true
    uploadProgress.value = 0
    showUploadMessage.value = false
    
    // 模拟上传进度
    const progressInterval = setInterval(() => {
      if (uploadProgress.value < 90) {
        uploadProgress.value += 10
      }
    }, 200)
    
    // 发送数据到父组件并等待结果
    emit('submit', { ...form }, (result: UploadResult) => {
      clearInterval(progressInterval)
      uploadProgress.value = 100
      
      // 显示父组件返回的消息
      uploadMessage.value = result.message
      showUploadMessage.value = true
      
      // 延迟关闭对话框
      setTimeout(() => {
        internalOpen.value = false
        // 重置状态
        setTimeout(() => {
          isUploading.value = false
          uploadProgress.value = 0
          showUploadMessage.value = false
          uploadMessage.value = ''
        }, 300)
      }, 1500)
    })
    
  } catch (error: any) {
    // 显示错误消息
    uploadMessage.value = error.message || '上传失败，请重试'
    showUploadMessage.value = true
    isUploading.value = false
    uploadProgress.value = 0
  }
}
</script>


