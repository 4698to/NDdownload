<template>
  <v-dialog v-model="internalOpen" persistent max-width="720">
    <v-card>
      <v-card-title>{{ titleText }}</v-card-title>
      <v-card-text>
        <v-row dense>
          <v-col v-if="mode === 'create'" cols="12">
            <v-select
              v-model="parentIdModel"
              :items="parentOptions"
              label="新建到"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field v-model="form.zipname" label="zipname" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12" sm="6">
            <v-switch v-model="form.isParent" label="分组节点 (isParent)" color="primary" hide-details />
          </v-col>
          <v-col cols="12">
            <v-textarea v-model="form.abouttext" label="abouttext" rows="2" auto-grow variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.targetpath" label="targetpath" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field v-model="form.savepath" label="savepath" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field v-model="form.dirpath" label="dirpath" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="4">
            <v-select
              v-model="form.type"
              :items="DIR_TYPE_OPTIONS"
              item-title="title"
              item-value="value"
              label="type"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="4">
            <v-text-field v-model.number="form.SeriesMin" label="SeriesMin" type="number" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="4">
            <v-text-field v-model.number="form.SeriesMax" label="SeriesMax" type="number" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="form.helplink" label="helplink" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="4">
            <v-text-field v-model.number="form.version" label="version" type="number" step="0.01" variant="outlined" density="comfortable" />
          </v-col>
          <v-col cols="4">
            <v-switch v-model="form.selected" label="selected" color="primary" hide-details />
          </v-col>
          <v-col cols="4">
            <v-switch v-model="form.IsEnabled" label="IsEnabled" color="primary" hide-details />
          </v-col>
          <v-col cols="4">
            <v-switch v-model="form.quick" label="quick" color="primary" hide-details />
          </v-col>
          <v-col cols="4">
            <v-switch v-model="form.ischange" label="ischange" color="primary" hide-details />
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
import { cloneItem, createDefaultItem, DIR_TYPE_OPTIONS, type InstallBoxItem } from '@/utils/installBoxTree'

const props = defineProps<{
  modelValue: boolean
  mode: 'create' | 'edit'
  node?: InstallBoxItem | null
  isParent?: boolean
  parentId?: string
  parentOptions?: { title: string; value: string }[]
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'update:parentId', v: string): void
  (e: 'submit', node: InstallBoxItem): void
}>()

const parentIdModel = computed({
  get: () => props.parentId ?? '',
  set: (v: string) => emit('update:parentId', v),
})

const parentOptions = computed(() => props.parentOptions ?? [{ title: '（根级）', value: '' }])

const internalOpen = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
})

const titleText = computed(() =>
  props.mode === 'create'
    ? (props.isParent ? '新建分组' : '新建资源')
    : '编辑资源',
)

const form = reactive<InstallBoxItem>(createDefaultItem(false))

function resetForm() {
  const base = props.mode === 'edit' && props.node
    ? cloneItem(props.node)
    : createDefaultItem(!!props.isParent)
  Object.assign(form, base)
}

function onSubmit() {
  emit('submit', cloneItem(form))
  internalOpen.value = false
}

watch(internalOpen, (open) => {
  if (open) resetForm()
})
</script>
