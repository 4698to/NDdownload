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

    <div class="admin-main flex-grow-1 overflow-auto">
      <v-alert v-if="!isAuthorized" type="warning" variant="tonal" class="ma-4">
        请先输入 NDTOOLDATAKEY 后再管理数据。
      </v-alert>

      <div v-else class="admin-content">
        <header class="page-header">
          <div class="page-header__text">
            <h1 class="text-h6 mb-1">天晴安装器资源包</h1>
            <p class="text-caption text-medium-emphasis">InstallBox_version_full.json · 维护清单、打包与发布</p>
          </div>
        </header>

        <section class="meta-panel">
          <v-expansion-panels v-model="metaPanel" flat>
            <v-expansion-panel value="meta">
              <v-expansion-panel-title density="compact" class="meta-panel-title">
                <div class="meta-panel-title__content">
                  <span class="meta-panel-title__label">
                    <v-icon size="small" icon="mdi-cog" class="mr-2" />
                    <span class="text-subtitle-2">全局配置</span>
                  </span>
                  <span class="meta-panel-title__status" aria-label="运行状态" @click.stop>
                    <v-chip color="info" variant="tonal" size="small" class="flex-shrink-0">
                      发布预览：{{ metaVersion || '—' }}
                      <template v-if="metaVersionRaise"> → {{ previewVersion }}</template>
                      · {{ packLeafCount }} 个 zip
                    </v-chip>
                    <v-chip
                      v-if="fileCheckSummary && !checkingFiles"
                      :color="fileCheckSummary.missingCount > 0 ? 'warning' : 'success'"
                      variant="tonal"
                      size="small"
                      class="flex-shrink-0"
                    >
                      资源包：{{ fileCheckSummary.existsCount }} 存在 / {{ fileCheckSummary.missingCount }} 缺失
                    </v-chip>
                    <v-chip
                      v-if="!metaPackpath.trim() && !loading"
                      color="info"
                      variant="tonal"
                      size="small"
                      class="flex-shrink-0"
                    >
                      请填写 packpath
                    </v-chip>
                    <v-chip
                      v-if="errorMessage"
                      color="error"
                      variant="tonal"
                      size="small"
                      closable
                      class="flex-shrink-0"
                      @click:close.stop="errorMessage = ''"
                    >
                      {{ errorMessage }}
                    </v-chip>
                    <v-chip
                      v-if="successMessage"
                      color="success"
                      variant="tonal"
                      size="small"
                      closable
                      class="flex-shrink-0"
                      @click:close.stop="successMessage = ''"
                    >
                      {{ successMessage }}
                    </v-chip>
                  </span>
                </div>
              </v-expansion-panel-title>
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
                  <v-col cols="12" sm="2">
                    <v-switch
                      v-model="metaVersionRaise"
                      label="发布时递增版本"
                      color="primary"
                      density="compact"
                      hide-details
                    />
                  </v-col>
                </v-row>
              </v-expansion-panel-text>
            </v-expansion-panel>
          </v-expansion-panels>
        </section>

        <section class="toolbar" aria-label="筛选与操作">
          <div class="toolbar__row toolbar__row--primary">
            <div class="toolbar__filters">
              <v-text-field
                v-model="searchText"
                prepend-inner-icon="mdi-magnify"
                label="搜索"
                variant="outlined"
                density="compact"
                hide-details
                clearable
                class="toolbar__search"
              />
              <v-chip color="info" variant="tonal" size="small" class="flex-shrink-0">
                显示 {{ displayRows.length }} / 共 {{ flatRows.length }} 条
              </v-chip>
            </div>
            <div class="toolbar__primary-actions">
              <v-btn
                prepend-icon="mdi-publish"
                color="primary"
                :loading="publishing"
                :disabled="loading || flatRows.length === 0 || !metaPackpath.trim()"
                @click="openPublishDialog"
              >
                发布
              </v-btn>
            </div>
          </div>

          <div class="toolbar__row toolbar__row--secondary">
            <div class="toolbar__group">
              <v-btn prepend-icon="mdi-folder-plus" variant="tonal" size="small" @click="openCreate(true)">
                新建分组
              </v-btn>
              <v-btn prepend-icon="mdi-file-plus" variant="tonal" size="small" @click="openCreate(false)">
                新建资源
              </v-btn>
            </div>
            <div class="toolbar__group">
              <v-btn
                prepend-icon="mdi-pencil"
                variant="tonal"
                size="small"
                :disabled="!selectedRow"
                @click="openEditSelected"
              >
                编辑
              </v-btn>
              <v-btn
                prepend-icon="mdi-delete-outline"
                variant="tonal"
                size="small"
                color="error"
                :disabled="!selectedRow"
                @click="confirmDelete"
              >
                删除
              </v-btn>
            </div>
            <div class="toolbar__group">
              <v-btn
                prepend-icon="mdi-refresh-circle"
                variant="tonal"
                size="small"
                color="warning"
                :loading="refreshingChanges"
                :disabled="loading || flatRows.length === 0"
                @click="refreshChanges"
              >
                刷新变更
              </v-btn>
              <v-btn
                prepend-icon="mdi-folder-multiple-plus"
                variant="tonal"
                size="small"
                @click="openImportParent"
              >
                导入目录
              </v-btn>
              <v-btn
                prepend-icon="mdi-zip-box"
                variant="tonal"
                size="small"
                :loading="packing"
                :disabled="loading || !selectedRow || !metaPackpath.trim()"
                @click="packSelected"
              >
                打包选中
              </v-btn>
            </div>
            <div class="toolbar__group toolbar__group--utility">
              <v-btn
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
            </div>
          </div>
        </section>

        <v-progress-linear
          v-if="loading || checkingFiles || packing || publishing || refreshingChanges"
          indeterminate
          color="primary"
          class="toolbar-progress"
        />

        <div class="admin-workspace">
          <div class="workspace-table">
            <v-data-table
              :headers="headers"
              :items="displayRows"
              item-value="rowId"
              density="compact"
              fixed-header
              class="data-table"
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
                <span class="text-caption text-truncate cell-abouttext">
                  {{ item.node.abouttext }}
                </span>
              </template>
              <template #item.dirpath="{ item }">{{ item.node.dirpath }}</template>
              <template #item.type="{ item }">{{ formatDirType(item.node.type) }}</template>
              <template #item.ischange="{ item }">
                <v-icon
                  v-if="!item.node.isParent || rowHasChildren(item)"
                  :icon="item.node.ischange ? 'mdi-alert-circle' : 'mdi-check-circle-outline'"
                  size="small"
                  :color="item.node.ischange ? 'warning' : 'success'"
                />
              </template>
              <template #item.sha="{ item }">
                <v-tooltip v-if="item.node.sha" location="top">
                  <template #activator="{ props: tipProps }">
                    <span v-bind="tipProps" class="text-caption">{{ truncateSha(item.node.sha) }}</span>
                  </template>
                  <span>{{ item.node.sha }}</span>
                </v-tooltip>
                <span v-else class="text-medium-emphasis">—</span>
              </template>
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

          <v-sheet class="workspace-editor" border rounded="lg">
            <template v-if="selectedRow">
              <div class="editor-header">
                <div class="editor-header__title text-subtitle-1 font-weight-medium d-flex align-center ga-2">
                  <v-icon :icon="selectedRow.node.isParent ? 'mdi-folder' : 'mdi-file'" size="small" />
                  选中资源
                </div>
                <p class="editor-header__path text-caption text-medium-emphasis">{{ selectedRow.path }}</p>
              </div>
              <div class="editor-body">
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
                <!-- savepath 仅开发环境使用，发布时 strip -->
                <v-text-field
                  v-model="selectedRow.node.dirpath"
                  label="dirpath"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-select
                  v-model="selectedRow.node.type"
                  :items="DIR_TYPE_OPTIONS"
                  item-title="title"
                  item-value="value"
                  label="type"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  @update:model-value="markDirty"
                />
                <v-row dense class="mb-3">
                  <v-col cols="6">
                    <v-text-field
                      v-model.number="selectedRow.node.SeriesMin"
                      label="SeriesMin"
                      type="number"
                      variant="outlined"
                      density="compact"
                      hide-details
                      @update:model-value="markDirty"
                    />
                  </v-col>
                  <v-col cols="6">
                    <v-text-field
                      v-model.number="selectedRow.node.SeriesMax"
                      label="SeriesMax"
                      type="number"
                      variant="outlined"
                      density="compact"
                      hide-details
                      @update:model-value="markDirty"
                    />
                  </v-col>
                </v-row>
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
                <v-text-field
                  :model-value="selectedRow.node.sha || ''"
                  label="sha"
                  variant="outlined"
                  density="compact"
                  readonly
                  class="mb-3"
                />
                <v-row dense class="mb-3">
                  <v-col cols="6">
                    <v-text-field
                      :model-value="formatMsDate(selectedRow.node.LastWriteTime)"
                      label="LastWriteTime"
                      variant="outlined"
                      density="compact"
                      readonly
                      hide-details
                    />
                  </v-col>
                  <v-col cols="6">
                    <v-text-field
                      :model-value="formatMsDate(selectedRow.node.LastPackTime, '未打包')"
                      label="LastPackTime"
                      variant="outlined"
                      density="compact"
                      readonly
                      hide-details
                    />
                  </v-col>
                </v-row>
                <v-btn
                  prepend-icon="mdi-zip-box"
                  variant="tonal"
                  size="small"
                  block
                  class="mb-3"
                  :loading="packing"
                  :disabled="!metaPackpath.trim() || selectedRow.node.isParent"
                  @click="packSelectedRow"
                >
                  打包此项
                </v-btn>
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
              </div>
            </template>
            <div v-else class="editor-empty">
              <v-icon icon="mdi-cursor-default-click" size="48" class="mb-2" />
              <div class="text-body-2">在左侧表格中选择一行</div>
              <div class="text-caption text-medium-emphasis">可在此快速编辑资源信息</div>
            </div>
          </v-sheet>
        </div>
      </div>
    </div>

    <InstallBoxNodeEditDialog
      v-model="editDialogOpen"
      v-model:parent-id="createParentId"
      :mode="editMode"
      :node="editingNode"
      :is-parent="createAsParent"
      :parent-options="parentPathOptions"
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

    <v-dialog v-model="importParentDialog" max-width="560" persistent>
      <v-card>
        <v-card-title>从父目录导入</v-card-title>
        <v-card-text>
          <v-select
            v-model="createParentId"
            :items="parentPathOptions"
            label="导入到"
            variant="outlined"
            density="comfortable"
            class="mb-3"
          />
          <v-text-field
            v-model="importParentPath"
            label="父目录路径（服务器可访问）"
            variant="outlined"
            density="comfortable"
            hint="将扫描一级子文件夹并创建分组 + 子资源"
            persistent-hint
          />
          <v-alert v-if="importPreview" type="info" variant="tonal" density="compact" class="mt-3">
            将导入分组「{{ importPreview.parent?.zipname }}」，包含 {{ importPreview.children?.length ?? 0 }} 个子资源
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeImportParent">取消</v-btn>
          <v-btn variant="tonal" :loading="scanningParent" @click="scanImportParent">扫描预览</v-btn>
          <v-btn color="primary" :disabled="!importPreview" @click="confirmImportParent">导入</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="publishDialog" max-width="560" persistent>
      <v-card>
        <v-card-title>确认发布</v-card-title>
        <v-card-text>
          <v-list density="compact">
            <v-list-item title="全局版本" :subtitle="publishSummary.versionLine" />
            <v-list-item title="打包数量" :subtitle="`${packLeafCount} 个 zip`" />
            <v-list-item title="输出目录 (packpath)" :subtitle="metaPackpath || '—'" />
            <v-list-item title="同步 DATA_DIR" subtitle="InstallBox_version_full.json / .xml / updateBox.txt" />
          </v-list>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="publishDialog = false">取消</v-btn>
          <v-btn color="primary" :loading="publishing" @click="doPublish">确认发布</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="publishLogsDialog" max-width="720">
      <v-card>
        <v-card-title>发布日志</v-card-title>
        <v-card-text>
          <pre class="publish-logs">{{ publishLogsText }}</pre>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn color="primary" @click="publishLogsDialog = false">关闭</v-btn>
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
  buildParentFromScan,
  countPackLeaves,
  createDefaultItem,
  collectPackFileNames,
  DIR_TYPE_OPTIONS,
  filterSearchVisibleRows,
  filterVisibleRows,
  findRowById,
  flattenTree,
  formatDirType,
  formatMsDate,
  formatSeriesRange,
  getRowPackFileName,
  getVersionRaise,
  parseLoadedData,
  previewNextVersion,
  removeNodeByIndexPath,
  rowHasChildren,
  serializeData,
  setVersionRaise,
  syncVersionMeta,
  truncateSha,
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
const packing = ref(false)
const publishing = ref(false)
const refreshingChanges = ref(false)
const scanningParent = ref(false)
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
const importParentDialog = ref(false)
const importParentPath = ref('')
const importPreview = ref<{ parent: InstallBoxItem; children: InstallBoxItem[] } | null>(null)
const publishDialog = ref(false)
const publishLogsDialog = ref(false)
const publishLogsText = ref('')
const collapsedRowIds = shallowRef<Set<string>>(new Set())
const metaPanel = ref(['meta'])

const meta = ref<InstallBoxMeta>({})
const items = ref<InstallBoxItem[]>([])
const fileCheckMap = ref<Record<string, PackFileCheckResult>>({})
const checkedPackpath = ref('')

const headers = [
  { title: 'zipname', key: 'zipname', width: 260 },
  { title: '说明', key: 'abouttext', width: 200 },
  { title: 'dirpath', key: 'dirpath', width: 100 },
  { title: '安装根', key: 'type', width: 100 },
  { title: '变更', key: 'ischange', width: 70, align: 'center' as const },
  { title: 'sha', key: 'sha', width: 100 },
  { title: '版本范围', key: 'series', width: 100 },
  { title: '服务器', key: 'serverFile', width: 70, align: 'center' as const },
  { title: '启用', key: 'IsEnabled', width: 60, align: 'center' as const },
  { title: 'quick', key: 'quick', width: 60, align: 'center' as const },
  { title: 'version', key: 'version', width: 70 },
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
const metaLastPack = computed(() => String(meta.value.LastPack ?? meta.value._lastPack ?? ''))
const metaVersionRaise = computed({
  get: () => getVersionRaise(meta.value),
  set: (v: boolean) => {
    setVersionRaise(meta.value, v)
    markDirty()
  },
})
const previewVersion = computed(() => previewNextVersion(meta.value, metaVersionRaise.value))
const packLeafCount = computed(() => countPackLeaves(items.value))
const publishSummary = computed(() => ({
  versionLine: metaVersionRaise.value
    ? `${metaVersion.value || '—'} → ${previewVersion.value}`
    : `${metaVersion.value || '—'}（不递增）`,
}))

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
  if (loading.value) return
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
      void checkPackFiles()
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
  if (selectedRow.value?.node.isParent) {
    createParentId.value = selectedRow.value.rowId
  }
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

async function refreshChanges() {
  refreshingChanges.value = true
  errorMessage.value = ''
  try {
    const res = await axios.post('/installbox/refresh-changes', { items: items.value })
    items.value = res.data?.items ?? items.value
    markDirty()
    successMessage.value = `变更检测完成：${res.data?.changedCount ?? 0} / ${res.data?.total ?? 0} 条有变更`
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.message || '刷新变更失败'
  } finally {
    refreshingChanges.value = false
  }
}

function openImportParent() {
  importParentPath.value = ''
  importPreview.value = null
  importParentDialog.value = true
}

function closeImportParent() {
  importParentDialog.value = false
  importPreview.value = null
}

async function scanImportParent() {
  const parentPath = importParentPath.value.trim()
  if (!parentPath) {
    errorMessage.value = '请输入父目录路径'
    return
  }
  scanningParent.value = true
  errorMessage.value = ''
  try {
    const res = await axios.post('/installbox/scan-parent', { parentPath })
    importPreview.value = {
      parent: res.data.parent,
      children: res.data.children ?? [],
    }
  } catch (e: any) {
    importPreview.value = null
    errorMessage.value = e?.response?.data?.error || e?.message || '扫描父目录失败'
  } finally {
    scanningParent.value = false
  }
}

function confirmImportParent() {
  if (!importPreview.value) return
  const node = buildParentFromScan(importPreview.value.parent, importPreview.value.children)
  addChildItem(items.value, getParentIndexPath(), node)
  if (createParentId.value) {
    const next = new Set(collapsedRowIds.value)
    next.delete(createParentId.value)
    collapsedRowIds.value = next
  }
  markDirty()
  closeImportParent()
  successMessage.value = `已导入分组「${node.zipname}」`
}

async function packByIndexPaths(indexPaths: number[][]) {
  const packpath = metaPackpath.value.trim()
  if (!packpath) {
    errorMessage.value = '请先在全局配置中填写 packpath'
    return
  }
  packing.value = true
  errorMessage.value = ''
  try {
    const res = await axios.post('/installbox/pack', {
      items: items.value,
      indexPaths,
      packpath,
      version_raise: metaVersionRaise.value,
    }, { timeout: 300000 })
    items.value = res.data?.items ?? items.value
    markDirty()
    successMessage.value = '打包完成'
    await checkPackFiles()
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.message || '打包失败'
  } finally {
    packing.value = false
  }
}

function packSelected() {
  if (!selectedRow.value) return
  packByIndexPaths([selectedRow.value.indexPath])
}

function packSelectedRow() {
  packSelected()
}

function openPublishDialog() {
  publishDialog.value = true
}

async function doPublish() {
  const packpath = metaPackpath.value.trim()
  if (!packpath) {
    errorMessage.value = '请先在全局配置中填写 packpath'
    return
  }
  publishing.value = true
  errorMessage.value = ''
  publishDialog.value = false
  try {
    const res = await axios.post('/installbox/publish', {
      meta: meta.value,
      items: items.value,
      packpath,
      version_raise: metaVersionRaise.value,
    }, { timeout: 300000 })
    if (res.data?.updatedMeta) {
      meta.value = syncVersionMeta(
        { ...meta.value, ...res.data.updatedMeta },
        res.data.version ?? metaVersion.value,
        res.data.updatedMeta.LastPack ?? res.data.updatedMeta._lastPack,
      )
    }
    if (res.data?.updatedItems) {
      items.value = res.data.updatedItems
    }
    markDirty()
    publishLogsText.value = (res.data?.logs ?? []).join('\n')
    publishLogsDialog.value = true
    successMessage.value = `发布成功，版本 ${res.data?.version ?? metaVersion.value}`
    await saveData()
  } catch (e: any) {
    errorMessage.value = e?.response?.data?.error || e?.message || '发布失败'
  } finally {
    publishing.value = false
  }
}

watch(isAuthorized, (authorized, wasAuthorized) => {
  if (authorized && !wasAuthorized) loadData()
})

onMounted(() => {
  if (isAuthorized.value) loadData()
})
</script>

<style scoped>
.installbox-admin {
  --space-xs: 4px;
  --space-sm: 8px;
  --space-md: 16px;
  --space-lg: 24px;
  --space-xl: 32px;
  --admin-chrome-offset: 300px;
  --editor-width: 360px;
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

.admin-content {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
  padding: var(--space-md);
  min-width: 0;
}

@media (min-width: 960px) {
  .admin-content {
    padding: var(--space-lg);
    gap: var(--space-lg);
  }
}

.page-header {
  padding-bottom: var(--space-md);
  border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.page-header__text {
  min-width: 0;
}

.meta-panel :deep(.v-expansion-panel) {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
}

.meta-panel-title :deep(.v-expansion-panel-title__overlay) {
  opacity: 0;
}

.meta-panel-title__content {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-sm);
  width: 100%;
  min-width: 0;
  padding-right: var(--space-sm);
}

.meta-panel-title__label {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
}

.meta-panel-title__status {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: var(--space-sm);
  flex: 1 1 200px;
  min-width: 0;
}

.toolbar {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
}

.toolbar__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-md);
}

.toolbar__row--primary {
  justify-content: space-between;
}

.toolbar__filters {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-sm);
  flex: 1 1 320px;
  min-width: 0;
}

.toolbar__search {
  flex: 1 1 200px;
  max-width: 320px;
}

.toolbar__primary-actions {
  display: flex;
  flex-shrink: 0;
  gap: var(--space-sm);
}

.toolbar__row--secondary {
  gap: var(--space-lg);
  padding-top: var(--space-xs);
  border-top: 1px solid rgba(var(--v-border-color), calc(var(--v-border-opacity) * 0.6));
}

.toolbar__group {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-sm);
}

.toolbar__group--utility {
  margin-left: auto;
}

.toolbar-progress {
  margin-top: calc(var(--space-xs) * -1);
}

.admin-workspace {
  display: grid;
  grid-template-columns: minmax(0, 1fr) var(--editor-width);
  gap: var(--space-md);
  align-items: start;
  min-height: 0;
}

@media (max-width: 1279px) {
  .admin-workspace {
    grid-template-columns: 1fr;
  }

  .workspace-editor {
    position: static;
    max-height: none;
  }
}

.workspace-table {
  min-width: 0;
}

.data-table :deep(.v-table__wrapper) {
  max-height: calc(100vh - var(--admin-chrome-offset));
}

.cell-abouttext {
  display: inline-block;
  max-width: 240px;
}

.workspace-editor {
  position: sticky;
  top: var(--space-md);
  max-height: calc(100vh - var(--admin-chrome-offset));
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

.editor-header {
  padding: var(--space-md);
  border-bottom: 1px solid rgba(var(--v-border-color), calc(var(--v-border-opacity) * 0.6));
  flex-shrink: 0;
}

.editor-header__path {
  margin: var(--space-xs) 0 0;
  word-break: break-all;
}

.editor-body {
  padding: var(--space-md);
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
}

.editor-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--space-xl) var(--space-md);
  min-height: 240px;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
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

.publish-logs {
  max-height: 420px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
  font-size: 12px;
  margin: 0;
}
</style>
