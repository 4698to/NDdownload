<template>
  <div class="ndtools-admin d-flex">
    <v-sheet class="admin-nav flex-shrink-0" width="240" border="e">
      <div class="pa-4 text-subtitle-1 font-weight-bold">NDTools 数据管理</div>

      <v-list nav density="compact" class="px-2">
        <v-list-item
          v-for="item in navItems"
          :key="item.value"
          :title="item.title"
          :subtitle="item.subtitle"
          :prepend-icon="item.icon"
          :active="activeFile === item.value"
          rounded="lg"
          @click="switchFile(item.value)"
        />
      </v-list>

      <v-divider class="my-2" />

      <div class="pa-3 d-flex flex-column ga-2">
        <v-btn
          v-if="!isAuthorized"
          color="primary"
          prepend-icon="mdi-key"
          block
          @click="requestDataKey(() => {})"
        >
          输入编辑密钥
        </v-btn>
        <template v-else>
          <v-chip v-if="dirty" color="warning" variant="tonal" size="small" class="align-self-start">
            未保存
          </v-chip>
          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" block @click="loadCurrentFile">
            重新加载
          </v-btn>
          <v-btn
            color="primary"
            prepend-icon="mdi-content-save"
            :loading="saving"
            :disabled="loading || !dirty"
            block
            @click="saveCurrentFile"
          >
            保存
          </v-btn>
        </template>
      </div>
    </v-sheet>

    <div class="admin-main flex-grow-1 pa-4 overflow-auto">
      <v-alert v-if="!isAuthorized" type="warning" variant="tonal">
        请先输入 NDTOOLDATAKEY 后再管理数据。
      </v-alert>

      <template v-else>
        <div class="d-flex align-center mb-4 ga-2">
          <div>
            <div class="text-h6">{{ currentNav?.title }}</div>
            <div class="text-caption text-medium-emphasis">{{ currentNav?.subtitle }}</div>
          </div>
        </div>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''">
          {{ errorMessage }}
        </v-alert>
        <v-alert v-if="successMessage" type="success" variant="tonal" class="mb-4" closable @click:close="successMessage = ''">
          {{ successMessage }}
        </v-alert>

        <div class="d-flex align-center flex-wrap ga-2 mb-4">
            <v-text-field
              v-model="searchText"
              prepend-inner-icon="mdi-magnify"
              label="搜索"
              variant="outlined"
              density="compact"
              hide-details
              clearable
              style="max-width: 320px"
            />
            <v-select
              v-model="createParentId"
              :items="parentPathOptions"
              label="新建到"
              variant="outlined"
              density="compact"
              hide-details
              style="max-width: 280px"
            />
            <v-btn prepend-icon="mdi-folder-plus" variant="tonal" size="small" @click="openCreate(true)">
              新建分组
            </v-btn>
            <v-btn prepend-icon="mdi-file-plus" variant="tonal" size="small" @click="openCreate(false)">
              新建工具
            </v-btn>
            <v-btn
              prepend-icon="mdi-pencil"
              variant="tonal"
              size="small"
              :disabled="!selectedRow"
              @click="openEditSelected"
            >
              编辑选中
            </v-btn>
            <v-btn
              prepend-icon="mdi-delete-outline"
              variant="tonal"
              size="small"
              color="error"
              :disabled="!selectedRow"
              @click="confirmDelete"
            >
              删除选中
            </v-btn>
            <v-chip color="info" variant="tonal">共 {{ filteredRows.length }} 条</v-chip>
          </div>

          <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-2" />

          <div class="admin-workspace d-flex ga-4">
            <div class="workspace-table flex-grow-1 min-width-0">
              <v-data-table
                :headers="headers"
                :items="filteredRows"
                item-value="rowId"
                density="compact"
                fixed-header
                height="calc(100vh - 280px)"
                :items-per-page="50"
                :items-per-page-options="[25, 50, 100, -1]"
                hover
                :row-props="getRowProps"
                @click:row="onRowClick"
              >
                <template #item.path="{ item }">
                  <div class="d-flex align-center ga-2">
                    <v-icon
                      :icon="item.node.IsGrouping ? 'mdi-folder' : 'mdi-file'"
                      size="small"
                      :color="item.node.IsGrouping ? 'warning' : 'info'"
                    />
                    <span class="text-caption">{{ item.path }}</span>
                  </div>
                </template>
                <template #item.Name="{ item }">{{ item.node.Name }}</template>
                <template #item.StartupFolder="{ item }">{{ item.node.StartupFolder }}</template>
                <template #item.SubPath="{ item }">{{ item.node.SubPath }}</template>
                <template #item.ExtensionType="{ item }">{{ item.node.ExtensionType }}</template>
                <template #item.message="{ item }">{{ item.node.message }}</template>
                <template #item.HelpUrl="{ item }">{{ item.node.HelpUrl }}</template>
                <template #item.hasHelp="{ item }">
                  <v-icon v-if="item.node.HelpUrl" icon="mdi-check" size="small" color="success" />
                </template>
              </v-data-table>
            </div>

            <v-sheet class="workspace-editor flex-shrink-0" width="360" border rounded="lg">
              <v-card v-if="selectedRow" variant="flat">
                <v-card-title class="text-subtitle-1 py-3 d-flex align-center flex-wrap ga-2">
                  <v-icon :icon="selectedRow.node.IsGrouping ? 'mdi-folder' : 'mdi-file'" />
                  <span>选中节点</span>
                </v-card-title>
                <v-card-subtitle class="text-wrap pb-2">{{ selectedRow.path }}</v-card-subtitle>
                <v-card-text class="pt-0">
                  <v-text-field
                    v-model="selectedRow.node.Name"
                    label="显示名称"
                    variant="outlined"
                    density="compact"
                    @update:model-value="markDirty"
                  />
                  <v-switch
                    v-model="selectedRow.node.IsGrouping"
                    label="分组"
                    color="primary"
                    density="compact"
                    hide-details
                    @update:model-value="onGroupingChange"
                  />
                  <v-text-field
                    v-model="selectedRow.node.RootType"
                    label="RootType"
                    variant="outlined"
                    density="compact"
                    class="mb-3"
                    @update:model-value="markDirty"
                  />
                  <v-text-field
                    v-model="selectedRow.node.ExtensionType"
                    label="ExtensionType"
                    variant="outlined"
                    density="compact"
                    class="mb-3"
                    @update:model-value="markDirty"
                  />
                  <v-text-field
                    v-model="selectedRow.node.StartupFolder"
                    label="StartupFolder"
                    variant="outlined"
                    density="compact"
                    class="mb-3"
                    @update:model-value="markDirty"
                  />
                  <v-text-field
                    v-model="selectedRow.node.SubPath"
                    label="SubPath"
                    variant="outlined"
                    density="compact"
                    class="mb-3"
                    @update:model-value="markDirty"
                  />
                  <v-text-field
                    v-model="selectedRow.node.HelpUrl"
                    label="HelpUrl"
                    variant="outlined"
                    density="compact"
                    hint="填写后自动标记为有帮助"
                    persistent-hint
                    class="mb-3"
                    @update:model-value="onHelpUrlChange"
                  />
                  <v-textarea
                    v-model="selectedRow.node.message"
                    label="说明 (message)"
                    variant="outlined"
                    density="compact"
                    rows="3"
                    auto-grow
                    @update:model-value="markDirty"
                  />
                </v-card-text>
              </v-card>
              <div v-else class="pa-4 text-center text-medium-emphasis">
                <v-icon icon="mdi-cursor-default-click" size="48" class="mb-2" />
                <div class="text-body-2">在左侧表格中选择一行</div>
                <div class="text-caption">可在此快速编辑节点信息</div>
              </div>
            </v-sheet>
          </div>
      </template>
    </div>

    <NDNodeEditDialog
      v-model="editDialogOpen"
      :mode="editMode"
      :node="editingNode"
      :is-grouping="createAsGroup"
      @submit="onNodeSubmit"
    />

    <v-dialog v-model="deleteDialog" max-width="480">
      <v-card>
        <v-card-title>确认删除</v-card-title>
        <v-card-text>
          确定删除「{{ selectedRow?.path }}」吗？其子节点也会一并删除。
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteDialog = false">取消</v-btn>
          <v-btn color="error" @click="doDelete">删除</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, ref, watch } from 'vue'
import axios from '@/plugins/axios'
import NDNodeEditDialog from '@/components/NDNodeEditDialog.vue'
import { isAuthorizedKey, requestDataKeyKey } from '@/keys/dataKey'
import type { RequestDataKeyFn } from '@/keys/dataKey'
import {
  addChildNode,
  applyNodeFields,
  createDefaultNode,
  filterRows,
  findRowById,
  flattenTree,
  normalizeHasHelp,
  parseLoadedData,
  removeNodeByIndexPath,
  serializeRoots,
  syncHasHelpFromUrl,
  type FlatRow,
  type LoadedNDData,
  type NDNode,
} from '@/utils/ndToolsTree'

type FileId = 'NDToolsList.json' | 'NDToolsListC3S3.json'

const navItems: { title: string; subtitle: string; value: FileId; icon: string }[] = [
  { title: '全工具集', subtitle: 'NDToolsList.json', value: 'NDToolsList.json', icon: 'mdi-toolbox-outline' },
  { title: 'C3S3 工具集', subtitle: 'NDToolsListC3S3.json', value: 'NDToolsListC3S3.json', icon: 'mdi-cube-outline' },
]

const requestDataKey = inject<RequestDataKeyFn>(requestDataKeyKey)!
const isAuthorized = inject(isAuthorizedKey)!

const activeFile = ref<FileId>('NDToolsList.json')
const searchText = ref('')
const loading = ref(false)
const saving = ref(false)
const dirty = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const selectedRowId = ref('')
const createParentId = ref('')
const editDialogOpen = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const createAsGroup = ref(false)
const editingNode = ref<NDNode | null>(null)
const editingRowId = ref<string | null>(null)
const deleteDialog = ref(false)

const fileState = ref<Record<FileId, LoadedNDData>>({
  'NDToolsList.json': { roots: [], format: 'array' },
  'NDToolsListC3S3.json': { roots: [], format: 'array' },
})

const headers = [
  { title: '路径', key: 'path', width: 300 },
  { title: '名称', key: 'Name' },
  { title: 'StartupFolder', key: 'StartupFolder' },
  { title: 'SubPath', key: 'SubPath' },
  { title: '扩展名', key: 'ExtensionType', width: 90 },
  { title: '说明', key: 'message' },
  { title: '帮助', key: 'hasHelp', width: 70, align: 'center' as const },
  { title: 'HelpUrl', key: 'HelpUrl' },
]

const flatRows = computed(() => flattenTree(fileState.value[activeFile.value].roots))
const filteredRows = computed(() => filterRows(flatRows.value, searchText.value))
const selectedRow = computed(() => {
  return selectedRowId.value ? findRowById(flatRows.value, selectedRowId.value) : undefined
})

const currentNav = computed(() => navItems.find(item => item.value === activeFile.value))

function switchFile(fileId: FileId) {
  if (activeFile.value !== fileId) {
    activeFile.value = fileId
  }
}

const parentPathOptions = computed(() => {
  const options = [{ title: '（根级）', value: '' }]
  for (const row of flatRows.value) {
    if (row.node.IsGrouping) {
      options.push({ title: row.path, value: row.rowId })
    }
  }
  return options
})

function markDirty() {
  dirty.value = true
}

function onHelpUrlChange(val: string | null) {
  if (!selectedRow.value) return
  selectedRow.value.node.HelpUrl = val?.trim() || null
  syncHasHelpFromUrl(selectedRow.value.node)
  markDirty()
}

function onGroupingChange(isGrouping: boolean | null) {
  if (!selectedRow.value) return
  const node = selectedRow.value.node
  node.IsGrouping = !!isGrouping
  if (node.IsGrouping) {
    if (!Array.isArray(node.Children)) node.Children = []
  } else {
    node.Children = []
  }
  markDirty()
}

function getRowProps({ item }: { item: FlatRow }) {
  return {
    class: item.rowId === selectedRowId.value ? 'row-selected' : '',
  }
}

function onRowClick(_: Event, { item }: { item: FlatRow }) {
  selectedRowId.value = item.rowId
}

function getParentIndexPath(): number[] {
  if (!createParentId.value) return []
  return findRowById(flatRows.value, createParentId.value)?.indexPath ?? []
}

async function loadFile(fileId: FileId) {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await axios.get(`/download?fileid=${encodeURIComponent(fileId)}`)
    let data = res.data
    if (typeof data === 'string') {
      data = JSON.parse(data.replace(/^\uFEFF/, '').trim())
    }
    fileState.value[fileId] = parseLoadedData(data)
    normalizeHasHelp(fileState.value[fileId].roots)
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.message || `加载 ${fileId} 失败`
  } finally {
    loading.value = false
  }
}

async function loadCurrentFile() {
  dirty.value = false
  selectedRowId.value = ''
  await loadFile(activeFile.value)
}

async function saveCurrentFile() {
  saving.value = true
  errorMessage.value = ''
  successMessage.value = ''
  const fileId = activeFile.value
  const { roots, format } = fileState.value[fileId]
  try {
    await axios.put(`/data?fileid=${encodeURIComponent(fileId)}`, serializeRoots(roots, format))
    dirty.value = false
    successMessage.value = `${fileId} 保存成功`
    await loadFile(fileId)
  } catch (e: any) {
    if (e?.response?.status === 401) {
      errorMessage.value = '鉴权失败，请重新输入 NDTOOLDATAKEY'
    } else {
      errorMessage.value = e?.response?.data?.error || e?.message || '保存失败'
    }
  } finally {
    saving.value = false
  }
}

function openCreate(asGroup: boolean) {
  editMode.value = 'create'
  createAsGroup.value = asGroup
  editingNode.value = createDefaultNode(asGroup)
  editingRowId.value = null
  editDialogOpen.value = true
}

function openEditSelected() {
  if (!selectedRow.value) return
  editMode.value = 'edit'
  editingNode.value = selectedRow.value.node
  editingRowId.value = selectedRow.value.rowId
  editDialogOpen.value = true
}

function onNodeSubmit(node: NDNode) {
  const roots = fileState.value[activeFile.value].roots
  if (editMode.value === 'create') {
    addChildNode(roots, getParentIndexPath(), node)
    markDirty()
    return
  }
  const row = editingRowId.value ? findRowById(flatRows.value, editingRowId.value) : selectedRow.value
  if (!row) return
  applyNodeFields(row.node, node)
  markDirty()
}

function confirmDelete() {
  if (!selectedRow.value) return
  deleteDialog.value = true
}

function doDelete() {
  if (!selectedRow.value) return
  removeNodeByIndexPath(fileState.value[activeFile.value].roots, selectedRow.value.indexPath)
  selectedRowId.value = ''
  deleteDialog.value = false
  markDirty()
}

watch(activeFile, () => {
  searchText.value = ''
  selectedRowId.value = ''
  createParentId.value = ''
  if (isAuthorized.value) {
    loadCurrentFile()
  }
})

watch(isAuthorized, (authorized) => {
  if (authorized) loadCurrentFile()
})

onMounted(() => {
  if (isAuthorized.value) loadCurrentFile()
})
</script>

<style scoped>
.ndtools-admin {
  min-height: calc(100vh - 48px);
}

.admin-nav {
  min-height: calc(100vh - 48px);
  display: flex;
  flex-direction: column;
}

.admin-nav .pa-3:last-child {
  margin-top: auto;
}

.admin-workspace {
  align-items: flex-start;
}

.workspace-editor {
  position: sticky;
  top: 16px;
  max-height: calc(100vh - 280px);
  overflow-y: auto;
}

.min-width-0 {
  min-width: 0;
}

:deep(.row-selected) {
  background-color: rgba(var(--v-theme-primary), 0.14) !important;
}

:deep(.row-selected:hover) {
  background-color: rgba(var(--v-theme-primary), 0.2) !important;
}
</style>
