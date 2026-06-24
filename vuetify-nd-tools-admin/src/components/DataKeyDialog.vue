<template>
  <v-dialog
    v-model="internalOpen"
    persistent
    max-width="480"
    scroll-strategy="none"
    :retain-focus="false"
    @after-enter="focusInput"
  >
    <v-card>
      <v-card-title class="d-flex align-center">
        <v-icon icon="mdi-key" class="mr-2" />
        输入编辑密钥
      </v-card-title>
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis mb-4">
          编辑或上传数据需要 NDTOOLDATAKEY，请输入后验证。
        </p>
        <v-text-field
          ref="keyInputRef"
          v-model="keyInput"
          label="NDTOOLDATAKEY"
          type="password"
          variant="outlined"
          density="comfortable"
          :disabled="verifying"
          @keyup.enter="onVerify"
        />
        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-2">
          {{ errorMessage }}
        </v-alert>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="verifying" @click="onCancel">取消</v-btn>
        <v-btn color="primary" :loading="verifying" @click="onVerify">验证并继续</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import axios from '@/plugins/axios'
import { setDataKey } from '@/utils/dataKey'

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'verified'): void
}>()

const internalOpen = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
})

const keyInput = ref('')
const keyInputRef = ref<{ focus: () => void } | null>(null)
const verifying = ref(false)
const errorMessage = ref('')

async function focusInput() {
  await nextTick()
  keyInputRef.value?.focus()
}

async function onVerify() {
  errorMessage.value = ''
  const key = keyInput.value.trim()
  if (!key) {
    errorMessage.value = '请输入 NDTOOLDATAKEY'
    return
  }

  verifying.value = true
  errorMessage.value = ''
  try {
    await axios.post('/auth/verify', {}, {
      headers: { 'X-NDTools-Data-Key': key },
    })
    setDataKey(key)
    internalOpen.value = false
    emit('verified')
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.response?.data?.message || '密钥验证失败'
  } finally {
    verifying.value = false
  }
}

function onCancel() {
  internalOpen.value = false
}

watch(internalOpen, (open) => {
  if (open) {
    keyInput.value = ''
    errorMessage.value = ''
  }
})
</script>
