<template>
  <v-dialog v-model="internalOpen" persistent max-width="640">
    <v-card>
      <v-card-title>{{ titleText }}</v-card-title>
      <v-card-text>
        <v-row dense>
          <v-col cols="12">
            <v-text-field v-model="form.Name" label="名称" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-switch v-model="form.IsGrouping" label="分组节点" color="primary" hide-details />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.StartupFolder" label="StartupFolder" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="6">
            <v-text-field v-model="form.SubPath" label="SubPath" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="6">
            <v-text-field v-model="form.ExtensionType" label="ExtensionType" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.RootType" label="RootType" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-textarea v-model="form.message" label="说明 (message)" rows="2" auto-grow variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.HelpUrl" label="HelpUrl" variant="outlined" density="comfortable" />
          </v-col>
        </v-row>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="internalOpen = false">取消</v-btn>
        <v-btn color="primary" @click="onSubmit">确定</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import { cloneNode, createDefaultNode, syncHasHelpFromUrl, type NDNode } from '@/utils/ndToolsTree'

const props = defineProps<{
  modelValue: boolean
  mode: 'create' | 'edit'
  node?: NDNode | null
  isGrouping?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'submit', node: NDNode): void
}>()

const internalOpen = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
})

const titleText = computed(() =>
  props.mode === 'create'
    ? (props.isGrouping ? '新建分组' : '新建工具')
    : '编辑节点',
)

const form = reactive<NDNode>(createDefaultNode(false))

function resetForm() {
  const base = props.mode === 'edit' && props.node
    ? cloneNode(props.node)
    : createDefaultNode(!!props.isGrouping)
  Object.assign(form, base)
}

function onSubmit() {
  syncHasHelpFromUrl(form)
  emit('submit', cloneNode(form))
  internalOpen.value = false
}

watch(internalOpen, (open) => {
  if (open) resetForm()
})
</script>
