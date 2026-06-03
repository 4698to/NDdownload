<template>
  <div class="installbox-admin d-flex">
    <v-sheet class="admin-nav flex-shrink-0" width="240" border="e">
      <div class="pa-4 text-subtitle-1 font-weight-bold">数据管理</div>

      <v-list nav density="compact" class="px-2">
        <v-list-item
          title="天晴安装器"
          subtitle="InstallBox_version_full.json"
          prepend-icon="mdi-package-variant"
          active
          rounded="lg"
          to="/installbox-edit"
        />
        <v-list-item
          title="NDTools 工具集"
          subtitle="NDToolsList / C3S3"
          prepend-icon="mdi-toolbox-outline"
          rounded="lg"
          to="/ndtools-edit"
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
          <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loading" block @click="loadData">
            重新加载
          </v-btn>
          <v-btn
            color="primary"
            prepend-icon="mdi-content-save"
            :loading="saving"
            :disabled="loading || !dirty"
            block
            @click="saveData"
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
            <div class="text-h6">天晴安装器资源包</div>
            <div class="text-caption text-medium-emphasis">InstallBox_version_full.json</div>
          </div>
        </div>

        <v-alert
          v-if="fileCheckSummary && !checkingFiles"
          :type="fileCheckSummary.missingCount > 0 ? 'warning' : 'success'"
          variant="tonal"
          class="mb-2"
          density="compact"
        >
          资源包检查（{{ fileCheckSummary.packpath }}）：
          {{ fileCheckSummary.existsCount }} 个存在，
          {{ fileCheckSummary.missingCount }} 个缺失
          <span v-if="fileCheckSummary.missingCount > 0" class="text-caption d-block mt-1">
            {{ fileCheckSummary.missingPreview.join('、') }}
            <span v-if="fileCheckSummary.missingCount > fileCheckSummary.missingPreview.length"> 等</span>
          </span>
        </v-alert>
        <v-alert v-if="!metaPackpath.trim() && !loading" type="info" variant="tonal" class="mb-2" density="compact">
          请在全局配置中填写 packpath（资源包所在目录），以便检查 zip 文件是否存在。
        </v-alert>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-2" closable @click:close="errorMessage = ''">
          {{ errorMessage }}
        </v-alert>
        <v-alert v-if="successMessage" type="success" variant="tonal" class="mb-2" closable @click:close="successMessage = ''">
          {{ successMessage }}
        </v-alert>

        <v-expansion-panels v-model="metaPanel" class="mb-2">
          <v-expansion-panel title="全局配置" value="meta">
            <v-expansion-panel-text>
              <v-row dense>
                <v-col cols="12" sm="2">
                  <v-text-field
                    v-model="metaVersion"
                    label="Version"
                    variant="outlined"
                    density="compact"
                    @update:model-value="markDirty"
                  />
                </v-col>
                
                <v-col cols="12" sm="4">
                  <v-text-field
                    v-model="metaPackpath"
                    label="packpath"
                    variant="outlined"
                    density="compact"
                    hint="资源包 zip 所在目录，用于检查文件是否存在"
                    persistent-hint
                    @update:model-value="markDirty"
                  />
                </v-col>
                <v-col cols="12" sm="4">
                  <v-text-field
                    v-model="metaRemoteUrl"
                    label="remoteUrl"
                    variant="outlined"
                    density="compact"
                    @update:model-value="markDirty"
                  />
                </v-col>
              </v-row>
            </v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>

        <div class="d-flex align-center flex-wrap ga-2">
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
            新建资源
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
          <v-btn
            prepend-icon="mdi-unfold-more-horizontal"
            variant="text"
            size="small"
            :disabled="!!searchQuery"
            @click="expandAll"
          >
            全部展开
          </v-btn>
          <v-btn
            prepend-icon="mdi-unfold-less-horizontal"
            variant="text"
            size="small"
            :disabled="!!searchQuery"
            @click="collapseAll"
          >
            全部折叠
          </v-btn>
          <v-btn
            prepend-icon="mdi-cloud-check-outline"
            variant="text"
            size="small"
            :loading="checkingFiles"
            :disabled="loading || flatRows.length === 0 || !metaPackpath.trim()"
            @click="checkPackFiles"
          >
            检查资源包
          </v-btn>
          <v-chip color="info" variant="tonal">
            显示 {{ displayRows.length }} / 共 {{ flatRows.length }} 条
          </v-chip>
        </div>

        <v-progress-linear v-if="loading || checkingFiles" indeterminate color="primary" class="mb-2" />

        <div class="admin-workspace d-flex ga-4">
          <div class="workspace-table flex-grow-1 min-width-0">
            <v-data-table
              :headers="headers"
              :items="displayRows"
              item-value="rowId"
              density="compact"
              fixed-header
              height="calc(100vh - 360px)"
              :items-per-page="50"
              :items-per-page-options="[25, 50, 100, -1]"
              hover
              :row-props="getRowProps"
              @click:row="onRowClick"
            >
              <template #item.zipname="{ item }">
                <div
                  class="d-flex align-center ga-1 node-path-cell"
                  :style="{ paddingLeft: `${getNodeDepth(item) * 20}px` }"
                  :title="item.path"
                >
                  <v-btn
                    v-if="rowHasChildren(item)"
                    variant="text"
                    size="x-small"
                    density="compact"
                    icon
                    class="expand-btn flex-shrink-0"
                    :disabled="!!searchQuery"
                    @click.stop="toggleExpand(item)"
                  >
                    <v-icon
                      :icon="isRowExpanded(item) ? 'mdi-chevron-down' : 'mdi-chevron-right'"
                      size="small"
                    />
                  </v-btn>
                  <span v-else class="expand-placeholder flex-shrink-0" />
                  <v-icon
                    :icon="item.node.isParent ? 'mdi-folder' : 'mdi-file'"
                    size="small"
                    :color="item.node.isParent ? 'warning' : 'info'"
                  />
                  <span class="text-caption">{{ item.node.zipname || '未命名' }}</span>
                </div>
              </template>
              <template #item.abouttext="{ item }">
                <span class="text-caption text-truncate d-inline-block" style="max-width: 240px">
                  {{ item.node.abouttext }}
                </span>
              </template>
              <template #item.dirpath="{ item }">{{ item.node.dirpath }}</template>
              <template #item.series="{ item }">{{ formatSeriesRange(item.node) }}</template>
              <template #item.IsEnabled="{ item }">
                <v-icon v-if="item.node.IsEnabled" icon="mdi-check" size="small" color="success" />
              </template>
              <template #item.quick="{ item }">
                <v-icon v-if="item.node.quick" icon="mdi-flash" size="small" color="warning" />
              </template>
              <template #item.version="{ item }">{{ item.node.version }}</template>
              <template #item.serverFile="{ item }">
                <v-tooltip v-if="getRowFileCheck(item)" location="top">
                  <template #activator="{ props: tipProps }">
                    <v-icon
                      v-bind="tipProps"
                      :icon="getRowFileCheckIcon(item)"
                      size="small"
                      :color="getRowFileCheckColor(item)"
                    />
                  </template>
                  <span>{{ getRowFileCheckTooltip(item) }}</span>
                </v-tooltip>
                <span v-else class="text-medium-emphasis">—</span>
              </template>
            </v-data-table>
          </div>

          <v-sheet class="workspace-editor flex-shrink-0" width="360" border rounded="lg">
            <v-card v-if="selectedRow" variant="flat">
              <v-card-title class="text-subtitle-1 py-3 d-flex align-center flex-wrap ga-2">
                <v-icon :icon="selectedRow.node.isParent ? 'mdi-folder' : 'mdi-file'" />
                <span>选中资源</span>
              </v-card-title>
              <v-card-subtitle class="text-wrap pb-2">{{ selectedRow.path }}</v-card-subtitle>
              <v-card-text class="pt-0">
                <v-text-field
                  v-model="selectedRow.node.zipname"
                  label="zipname"
                  variant="outlined"
                  density="compact"
                  @update:model-value="markDirty"
                />
                <v-textarea
                  v-model="selectedRow.node.abouttext"
                  label="abouttext"
                  variant="outlined"
                  density="compact"
                  rows="2"
                  auto-grow
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-switch
                  v-model="selectedRow.node.isParent"
                  label="分组 (isParent)"
                  color="primary"
                  density="compact"
                  hide-details
                  @update:model-value="onParentChange"
                />
                <v-text-field
                  v-model="selectedRow.node.targetpath"
                  label="targetpath"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-text-field
                  v-model="selectedRow.node.savepath"
                  label="savepath"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-text-field
                  v-model="selectedRow.node.dirpath"
                  label="dirpath"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-text-field
                  v-model.number="selectedRow.node.SeriesMin"
                  label="SeriesMin"
                  type="number"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-text-field
                  v-model.number="selectedRow.node.SeriesMax"
                  label="SeriesMax"
                  type="number"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-text-field
                  v-model="selectedRow.node.helplink"
                  label="helplink"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-alert
                  v-if="getRowPackFileName(selectedRow)"
                  :type="getRowFileCheck(selectedRow)?.exists === false ? 'error' : getRowFileCheck(selectedRow)?.exists ? 'success' : 'info'"
                  variant="tonal"
                  density="compact"
                  class="mb-3"
                >
                  {{ getRowFileCheckTooltip(selectedRow) }}
                </v-alert>
                <v-text-field
                  v-model.number="selectedRow.node.version"
                  label="version"
                  type="number"
                  step="0.01"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-switch
                  v-model="selectedRow.node.selected"
                  label="selected"
                  color="primary"
                  density="compact"
                  hide-details
                  @update:model-value="markDirty"
                />
                <v-switch
                  v-model="selectedRow.node.IsEnabled"
                  label="IsEnabled"
                  color="primary"
                  density="compact"
                  hide-details
                  @update:model-value="markDirty"
                />
                <v-switch
                  v-model="selectedRow.node.quick"
                  label="quick"
                  color="primary"
                  density="compact"
                  hide-details
                  @update:model-value="markDirty"
                />
              </v-card-text>
            </v-card>
            <div v-else class="pa-4 text-center text-medium-emphasis">
              <v-icon icon="mdi-cursor-default-click" size="48" class="mb-2" />
              <div class="text-body-2">在左侧表格中选择一行</div>
              <div class="text-caption">可在此快速编辑资源信息</div>
            </div>
          </v-sheet>
        </div>
      </template>
    </div>

    <InstallBoxNodeEditDialog
      v-model="editDialogOpen"
      :mode="editMode"
      :node="editingNode"
      :is-parent="createAsParent"
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
import { computed, inject, onMounted, ref, shallowRef, watch } from 'vue'
import axios from '@/plugins/axios'
import InstallBoxNodeEditDialog from '@/components/InstallBoxNodeEditDialog.vue'
import { isAuthorizedKey, requestDataKeyKey } from '@/keys/dataKey'
import type { RequestDataKeyFn } from '@/keys/dataKey'
import {
  addChildItem,
  applyItemFields,
  createDefaultItem,
  collectPackFileNames,
  filterSearchVisibleRows,
  filterVisibleRows,
  findRowById,
  flattenTree,
  formatSeriesRange,
  getRowPackFileName,
  parseLoadedData,
  removeNodeByIndexPath,
  rowHasChildren,
  serializeData,
  type FlatRow,
  type InstallBoxItem,
  type InstallBoxMeta,
  type PackFileCheckResult,
} from '@/utils/installBoxTree'

const FILE_ID = 'InstallBox_version_full.json'

const requestDataKey = inject<RequestDataKeyFn>(requestDataKeyKey)!
const isAuthorized = inject(isAuthorizedKey)!

const searchText = ref('')
const loading = ref(false)
const checkingFiles = ref(false)
const saving = ref(false)
const dirty = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const selectedRowId = ref('')
const createParentId = ref('')
const editDialogOpen = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const createAsParent = ref(false)
const editingNode = ref<InstallBoxItem | null>(null)
const editingRowId = ref<string | null>(null)
const deleteDialog = ref(false)
const collapsedRowIds = shallowRef<Set<string>>(new Set())
const metaPanel = ref(['meta'])

const meta = ref<InstallBoxMeta>({})
const items = ref<InstallBoxItem[]>([])
const fileCheckMap = ref<Record<string, PackFileCheckResult>>({})
const checkedPackpath = ref('')

const headers = [
  { title: 'zipname', key: 'zipname', width: 280 },
  { title: '说明', key: 'abouttext', width: 240 },
  { title: 'dirpath', key: 'dirpath', width: 120 },
  { title: '版本范围', key: 'series', width: 110 },
  { title: '服务器', key: 'serverFile', width: 80, align: 'center' as const },
  { title: '启用', key: 'IsEnabled', width: 70, align: 'center' as const },
  { title: 'quick', key: 'quick', width: 70, align: 'center' as const },
  { title: 'version', key: 'version', width: 80 },
]

const flatRows = computed(() => flattenTree(items.value))
const searchQuery = computed(() => (searchText.value ?? '').trim())
const displayRows = computed(() => {
  const rows = flatRows.value
  const query = searchQuery.value
  if (query) return filterSearchVisibleRows(rows, query)
  return filterVisibleRows(rows, collapsedRowIds.value)
})
const selectedRow = computed(() =>
  selectedRowId.value ? findRowById(flatRows.value, selectedRowId.value) : undefined,
)

const metaVersion = computed({
  get: () => String(meta.value.Version ?? meta.value._version ?? ''),
  set: (v: string) => {
    meta.value.Version = v
    meta.value._version = v
    markDirty()
  },
})
const metaRemoteUrl = computed({
  get: () => String(meta.value.remoteUrl ?? ''),
  set: (v: string) => {
    meta.value.remoteUrl = v
    markDirty()
  },
})
const metaPackpath = computed({
  get: () => String(meta.value.packpath ?? ''),
  set: (v: string) => {
    meta.value.packpath = v || null
    markDirty()
  },
})

const parentPathOptions = computed(() => {
  const options = [{ title: '（根级）', value: '' }]
  for (const row of flatRows.value) {
    if (row.node.isParent) {
      options.push({ title: row.path, value: row.rowId })
    }
  }
  return options
})

const fileCheckSummary = computed(() => {
  const values = Object.values(fileCheckMap.value)
  if (values.length === 0) return null
  const missing = values.filter(item => !item.exists)
  const missingNames = Object.entries(fileCheckMap.value)
    .filter(([, item]) => !item.exists)
    .map(([name]) => name)
  return {
    existsCount: values.length - missing.length,
    missingCount: missing.length,
    missingPreview: missingNames.slice(0, 8),
    packpath: checkedPackpath.value || metaPackpath.value,
  }
})

function getRowFileCheck(row: FlatRow): PackFileCheckResult | null {
  const name = getRowPackFileName(row)
  if (!name) return null
  return fileCheckMap.value[name] ?? null
}

function getRowFileCheckIcon(row: FlatRow): string {
  const check = getRowFileCheck(row)
  if (checkingFiles.value && !check) return 'mdi-loading'
  if (!check) return 'mdi-help-circle-outline'
  return check.exists ? 'mdi-check-circle' : 'mdi-alert-circle'
}

function getRowFileCheckColor(row: FlatRow): string {
  const check = getRowFileCheck(row)
  if (checkingFiles.value && !check) return 'info'
  if (!check) return 'secondary'
  return check.exists ? 'success' : 'error'
}

function getRowFileCheckTooltip(row: FlatRow): string {
  const name = getRowPackFileName(row)
  if (!name) return ''
  const check = getRowFileCheck(row)
  if (checkingFiles.value && !check) return `${name}：检查中…`
  if (!check) return `${name}：未检查`
  if (check.exists) {
    const location = check.path || checkedPackpath.value || metaPackpath.value
    return `${name}：存在（packpath · ${location}）`
  }
  const base = checkedPackpath.value || metaPackpath.value
  return base ? `${name}：在 ${base} 中不存在` : `${name}：不存在`
}

async function checkPackFiles() {
  const packpath = metaPackpath.value.trim()
  if (!packpath) {
    errorMessage.value = '请先在全局配置中填写 packpath（资源包目录）'
    fileCheckMap.value = {}
    checkedPackpath.value = ''
    return
  }
  const names = collectPackFileNames(items.value)
  if (names.length === 0) {
    fileCheckMap.value = {}
    checkedPackpath.value = ''
    return
  }
  checkingFiles.value = true
  errorMessage.value = ''
  try {
    const res = await axios.post('/installbox/check-files', {
      files: names,
      packpath,
    }, { timeout: 120000 })
    fileCheckMap.value = res.data?.results ?? {}
    checkedPackpath.value = res.data?.packpath || packpath
  } catch (e: any) {
    fileCheckMap.value = {}
    checkedPackpath.value = ''
    errorMessage.value = e?.response?.data?.error || e?.message || '资源包检查失败'
  } finally {
    checkingFiles.value = false
  }
}

function markDirty() {
  dirty.value = true
}

function getNodeDepth(row: FlatRow) {
  return row.indexPath.length - 1
}

function isRowExpanded(row: FlatRow) {
  if (searchQuery.value) return true
  return !collapsedRowIds.value.has(row.rowId)
}

function toggleExpand(row: FlatRow) {
  if (!rowHasChildren(row) || searchQuery.value) return
  const next = new Set(collapsedRowIds.value)
  if (next.has(row.rowId)) next.delete(row.rowId)
  else next.add(row.rowId)
  collapsedRowIds.value = next
}

function expandAll() {
  collapsedRowIds.value = new Set()
}

function collapseAll() {
  const ids = new Set<string>()
  for (const row of flatRows.value) {
    if (rowHasChildren(row)) ids.add(row.rowId)
  }
  collapsedRowIds.value = ids
}

function resetTreeExpandState() {
  collapsedRowIds.value = new Set()
}

function getRowProps({ item }: { item: FlatRow }) {
  return {
    class: item.rowId === selectedRowId.value ? 'row-selected' : '',
  }
}

function onRowClick(_: Event, { item }: { item: FlatRow }) {
  selectedRowId.value = item.rowId
}

function onParentChange(isParent: boolean | null) {
  if (!selectedRow.value) return
  const node = selectedRow.value.node
  node.isParent = !!isParent
  if (node.isParent) {
    if (!Array.isArray(node.child)) node.child = []
  } else {
    node.child = null
  }
  markDirty()
}

function getParentIndexPath(): number[] {
  if (!createParentId.value) return []
  return findRowById(flatRows.value, createParentId.value)?.indexPath ?? []
}

async function loadData() {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await axios.get(`/download?fileid=${encodeURIComponent(FILE_ID)}`)
    let data = res.data
    if (typeof data === 'string') {
      data = JSON.parse(data.replace(/^\uFEFF/, '').trim())
    }
    const parsed = parseLoadedData(data)
    meta.value = parsed.meta
    items.value = parsed.items
    dirty.value = false
    selectedRowId.value = ''
    resetTreeExpandState()
    if (metaPackpath.value.trim()) {
      await checkPackFiles()
    } else {
      fileCheckMap.value = {}
      checkedPackpath.value = ''
    }
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.message || `加载 ${FILE_ID} 失败`
  } finally {
    loading.value = false
  }
}

async function saveData() {
  saving.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await axios.put(`/data?fileid=${encodeURIComponent(FILE_ID)}`, serializeData(meta.value, items.value))
    dirty.value = false
    successMessage.value = `${FILE_ID} 保存成功`
    await loadData()
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

function openCreate(asParent: boolean) {
  editMode.value = 'create'
  createAsParent.value = asParent
  editingNode.value = createDefaultItem(asParent)
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

function onNodeSubmit(node: InstallBoxItem) {
  if (editMode.value === 'create') {
    addChildItem(items.value, getParentIndexPath(), node)
    if (createParentId.value) {
      const next = new Set(collapsedRowIds.value)
      next.delete(createParentId.value)
      collapsedRowIds.value = next
    }
    markDirty()
    return
  }
  const row = editingRowId.value ? findRowById(flatRows.value, editingRowId.value) : selectedRow.value
  if (!row) return
  applyItemFields(row.node, node)
  markDirty()
}

function confirmDelete() {
  if (!selectedRow.value) return
  deleteDialog.value = true
}

function doDelete() {
  if (!selectedRow.value) return
  removeNodeByIndexPath(items.value, selectedRow.value.indexPath)
  selectedRowId.value = ''
  deleteDialog.value = false
  markDirty()
}

watch(isAuthorized, (authorized) => {
  if (authorized) loadData()
})

onMounted(() => {
  if (isAuthorized.value) loadData()
})
</script>

<style scoped>
.installbox-admin {
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
  max-height: calc(100vh - 360px);
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

.expand-btn {
  width: 24px;
  height: 24px;
}

.expand-placeholder {
  width: 24px;
}
</style>
