<template>
  <v-dialog v-model="internalOpen" persistent fullscreen transition="dialog-bottom-transition">
    <v-card>
      <v-toolbar color="primary" density="compact">
        <v-toolbar-title>{{ titleText }}</v-toolbar-title>
        <v-spacer />
        <v-btn variant="text" prepend-icon="mdi-format-align-left" @click="formatJson">
          格式化
        </v-btn>
        <v-btn variant="text" prepend-icon="mdi-close" @click="onCancel">
          取消
        </v-btn>
        <v-btn
          variant="elevated"
          color="white"
          class="text-primary mr-2"
          prepend-icon="mdi-content-save"
          :loading="saving"
          :disabled="loading"
          @click="onSave"
        >
          保存
        </v-btn>
      </v-toolbar>

      <v-card-text class="pa-4 fill-height">
        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4" />
        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''">
          {{ errorMessage }}
        </v-alert>
        <v-alert v-if="successMessage" type="success" variant="tonal" class="mb-4" closable @click:close="successMessage = ''">
          {{ successMessage }}
        </v-alert>
        <v-textarea
          v-model="jsonText"
          :disabled="loading"
          variant="outlined"
          auto-grow
          rows="20"
          class="json-editor"
          hide-details
          spellcheck="false"
        />
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import axios from '@/plugins/axios'

const props = defineProps<{
  modelValue: boolean
  fileId: string
  title?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'saved', fileId: string): void
}>()

const internalOpen = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
})

const titleText = computed(() => props.title ?? `编辑 ${props.fileId}`)

const jsonText = ref('')
const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

async function loadFile() {
  if (!props.fileId) return
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const res = await axios.get(`/download?fileid=${encodeURIComponent(props.fileId)}`)
    let data = res.data
    if (typeof data === 'string') {
      data = JSON.parse(data.replace(/^\uFEFF/, '').trim())
    }
    jsonText.value = JSON.stringify(data, null, 2)
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.message || '加载失败'
    jsonText.value = ''
  } finally {
    loading.value = false
  }
}

function formatJson() {
  errorMessage.value = ''
  try {
    const parsed = JSON.parse(jsonText.value)
    jsonText.value = JSON.stringify(parsed, null, 2)
  } catch (e: any) {
    errorMessage.value = `JSON 格式错误: ${e.message}`
  }
}

function onCancel() {
  internalOpen.value = false
}

async function onSave() {
  errorMessage.value = ''
  successMessage.value = ''

  let parsed: unknown
  try {
    parsed = JSON.parse(jsonText.value)
  } catch (e: any) {
    errorMessage.value = `JSON 格式错误，无法保存: ${e.message}`
    return
  }

  saving.value = true
  try {
    await axios.put(`/data?fileid=${encodeURIComponent(props.fileId)}`, parsed)
    successMessage.value = '保存成功'
    emit('saved', props.fileId)
    setTimeout(() => {
      internalOpen.value = false
    }, 600)
  } catch (e: any) {
    const status = e?.response?.status
    if (status === 401) {
      errorMessage.value = '鉴权失败，密钥无效或已过期，请重新输入 NDTOOLDATAKEY'
    } else {
      errorMessage.value = e?.response?.data?.error || e?.response?.data?.message || e?.message || '保存失败'
    }
  } finally {
    saving.value = false
  }
}

watch(internalOpen, (open) => {
  if (open) {
    loadFile()
  }
})

watch(() => props.fileId, () => {
  if (internalOpen.value) {
    loadFile()
  }
})
</script>

<style scoped>
.json-editor :deep(textarea) {
  font-family: Consolas, 'Courier New', monospace;
  font-size: 13px;
  line-height: 1.5;
}
</style>
