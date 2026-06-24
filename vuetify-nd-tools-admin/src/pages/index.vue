<template>
  <div class="browse-page">
    <v-window v-model="tab">
      <v-window-item value="installbox">
        <div v-if="tab === 'installbox'" class="browse-tab">
          <header class="page-header">
            <div class="page-header__text">
              <h1 class="page-header__title text-h6 mb-1">
                <v-icon icon="mdi-package-variant" color="primary" size="small" class="page-header__icon" />
                天晴安装器资源包
              </h1>
              <p class="text-body-2 text-medium-emphasis page-header__lead">
                一个工具安装全网的 3ds Max 插件，让我们一起来丰富工具库，方便所有人！
              </p>
            </div>
            <div class="page-header__actions">
              <v-btn
                v-if="isAuthorized"
                prepend-icon="mdi-pencil"
                variant="tonal"
                to="/installbox-edit"
              >
                编辑数据
              </v-btn>
              <v-btn
                color="primary"
                prepend-icon="mdi-file-upload-outline"
                @click="requestUpload"
              >
                上传
              </v-btn>
            </div>
          </header>

          <section class="browse-content">
            <div v-if="loading" class="browse-state browse-state--loading">
              <v-progress-circular indeterminate color="primary" size="40" />
            </div>
            <v-alert v-else-if="error" type="error" variant="tonal" class="browse-alert">
              {{ error }}
            </v-alert>
            <div v-else class="browse-tree-panel">
            <v-treeview
              class="browse-tree"
              :items="rows"
              :open-all="false"
              :item-children="'child'"
              activatable
              hoverable
              open-on-click
              density="compact"
              color="primary"
            >
              <template #title="{ item }">
                <span :class="{ 'tree-row-title--folder': hasChildren(item) }">
                  {{ item.zipname }}
                </span>
              </template>
              <template #prepend="{ item }">
                <v-badge
                  v-if="hasChildren(item)"
                  color="info"
                  :content="getChildCount(item)"
                >
                  <v-icon color="warning">mdi-folder</v-icon>
                </v-badge>
                <v-icon v-else color="info" icon="mdi-file" />
              </template>
              <template #subtitle="{ item }">
                <span v-if="item.abouttext" class="tree-row-subtitle">
                  {{ item.abouttext }}
                </span>
              </template>
              <template #append="{ item }">
                <div class="tree-row-meta">
                  <v-chip
                    v-if="item._displayPath"
                    :color="item.quick ? 'quick' : 'noquick'"
                    variant="flat"
                    size="small"
                    class="tree-path-chip"
                    :class="item.quick ? 'tree-path-chip--quick' : 'tree-path-chip--standard'"
                  >
                    {{ item._displayPath }}
                  </v-chip>
                  <v-chip color="info" size="small" variant="tonal" class="tree-series-chip">
                    <v-icon start icon="mdi-numeric" size="x-small" />
                    {{ item._displaySeries }}
                  </v-chip>
                  <v-btn
                    v-if="item.helplink"
                    :href="item.helplink"
                    target="_blank"
                    icon
                    variant="text"
                    size="small"
                    color="info"
                    aria-label="查看帮助"
                  >
                    <v-icon>mdi-help-circle</v-icon>
                  </v-btn>
                  <v-icon v-else color="secondary" size="small" class="tree-row-meta__muted">
                    mdi-help-circle-outline
                  </v-icon>
                  <span class="tree-row-meta__date text-caption text-medium-emphasis">
                    {{ item._displayDate }}
                  </span>
                </div>
              </template>
            </v-treeview>
            </div>
          </section>

          <UploadDialog
            v-model="openUpload"
            title="上传插件提交到【天晴安装器】资源包"
            @submit="onUpload"
          />
        </div>
      </v-window-item>

      <v-window-item value="ndtools">
        <div v-if="tab === 'ndtools'" class="browse-tab">
          <header class="page-header">
            <div class="page-header__text">
              <h1 class="page-header__title text-h6 mb-1">
                <v-icon icon="mdi-toolbox-outline" color="info" size="small" class="page-header__icon" />
                C3S3 工具集
              </h1>
              <p class="text-caption text-medium-emphasis">NDToolsListC3S3.json</p>
            </div>
            <div class="page-header__actions">
              <v-btn
                v-if="isAuthorized"
                prepend-icon="mdi-pencil"
                variant="tonal"
                @click="openEditNDToolsC3S3 = true"
              >
                编辑数据
              </v-btn>
            </div>
          </header>

          <section class="browse-content">
            <NDToolsTree :items="ndtoolsTree" />
          </section>
        </div>
      </v-window-item>

      <v-window-item value="ndtoolsall">
        <div v-if="tab === 'ndtoolsall'" class="browse-tab">
          <header class="page-header">
            <div class="page-header__text">
              <h1 class="page-header__title text-h6 mb-1">
                <v-icon icon="mdi-view-list" color="warning" size="small" class="page-header__icon" />
                盒子全工具集
              </h1>
              <p class="text-caption text-medium-emphasis">NDToolsList.json</p>
            </div>
            <div class="page-header__actions">
              <v-btn
                v-if="isAuthorized"
                prepend-icon="mdi-pencil"
                variant="tonal"
                @click="openEditNDTools = true"
              >
                编辑数据
              </v-btn>
            </div>
          </header>

          <section class="browse-content">
            <NDToolsTree :items="ndtoolsAllTree" />
          </section>
        </div>
      </v-window-item>
    </v-window>

    <JsonEditDialog
      v-if="openEditNDTools"
      v-model="openEditNDTools"
      file-id="NDToolsList.json"
      title="编辑 NDToolsList.json"
      @saved="onJsonSaved"
    />
    <JsonEditDialog
      v-if="openEditNDToolsC3S3"
      v-model="openEditNDToolsC3S3"
      file-id="NDToolsListC3S3.json"
      title="编辑 NDToolsListC3S3.json"
      @saved="onJsonSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, inject, defineAsyncComponent, type Ref } from 'vue'
import axios from '@/plugins/axios'
import { downloadJson } from '@/utils/downloadJson'
import {
  enrichInstallBoxDisplay,
  type InstallBoxItem,
} from '@/utils/installBoxTree'
import { requestDataKeyKey, isAuthorizedKey } from '@/keys/dataKey'
import type { RequestDataKeyFn } from '@/keys/dataKey'

const NDToolsTree = defineAsyncComponent(() => import('@/components/NDToolsTree.vue'))
const UploadDialog = defineAsyncComponent(() => import('@/components/UploadDialog.vue'))
const JsonEditDialog = defineAsyncComponent(() => import('@/components/JsonEditDialog.vue'))

const SERIES_MIN = 2015
const SERIES_MAX = 2025

const injectedTab = inject<Ref<'installbox' | 'ndtools' | 'ndtoolsall'>>('activeTab')
const requestDataKey = inject<RequestDataKeyFn>(requestDataKeyKey)!
const isAuthorized = inject(isAuthorizedKey)!
const tab = injectedTab ?? ref<'installbox' | 'ndtools' | 'ndtoolsall'>('installbox')
const loading = ref(true)
const error = ref('')
const rows = ref<InstallBoxItem[]>([])
const ndtoolsTree = ref<any[]>([])
const ndtoolsAllTree = ref<any[]>([])
const ndtoolsLoaded = ref(false)
const ndtoolsAllLoaded = ref(false)
const openUpload = ref(false)
const openEditNDTools = ref(false)
const openEditNDToolsC3S3 = ref(false)

function requestUpload() {
  requestDataKey(() => {
    openUpload.value = true
  })
}

function normalizeChildren(arr: InstallBoxItem[]): void {
  for (const item of arr) {
    if (Array.isArray(item.child)) {
      normalizeChildren(item.child)
      if (item.child.length === 0) {
        item.child = null
      }
    } else {
      item.child = null
    }
  }
}

function hasChildren(item: InstallBoxItem): boolean {
  return Array.isArray(item.child) && item.child.length > 0
}

function getChildCount(item: InstallBoxItem): number {
  return Array.isArray(item.child) ? item.child.length : 0
}

function normalizeNDChildren(arr: any[]): void {
  for (const item of arr) {
    if (Array.isArray(item.Children)) {
      normalizeNDChildren(item.Children)
      if (item.Children.length === 0) {
        item.Children = null
      }
    } else {
      item.Children = null
    }
  }
}

async function getdata() {
  try {
    loading.value = true
    error.value = ''
    const data = await downloadJson('InstallBox_version_full.json') as Record<string, unknown>

    const items: InstallBoxItem[] = Array.isArray(data?.item)
      ? data.item as InstallBoxItem[]
      : Array.isArray(data)
        ? data as InstallBoxItem[]
        : []

    normalizeChildren(items)
    enrichInstallBoxDisplay(items, SERIES_MIN, SERIES_MAX)
    rows.value = items
  } catch (e: unknown) {
    const err = e as { message?: string }
    if (e instanceof SyntaxError) {
      error.value = '返回的不是有效的 JSON 数据'
    } else {
      error.value = err?.message || '未知错误'
    }
    rows.value = []
  } finally {
    loading.value = false
  }
}

async function loadNDToolsTree() {
  if (ndtoolsLoaded.value) return
  try {
    const data = await downloadJson('NDToolsListC3S3.json')
    const items: any[] = Array.isArray(data) ? data : [data]
    normalizeNDChildren(items)
    ndtoolsTree.value = items
    ndtoolsLoaded.value = true
  } catch {
    ndtoolsTree.value = []
  }
}

async function loadNDToolsAllTree() {
  if (ndtoolsAllLoaded.value) return
  try {
    const data = await downloadJson('NDToolsList.json')
    const items: any[] = Array.isArray(data) ? data : [data]
    normalizeNDChildren(items)
    ndtoolsAllTree.value = items
    ndtoolsAllLoaded.value = true
  } catch {
    ndtoolsAllTree.value = []
  }
}

watch(tab, (value) => {
  if (value === 'ndtools') void loadNDToolsTree()
  if (value === 'ndtoolsall') void loadNDToolsAllTree()
})

onMounted(() => {
  void getdata()
})

async function onJsonSaved(fileId: string) {
  if (fileId === 'NDToolsList.json') {
    ndtoolsAllLoaded.value = false
    await loadNDToolsAllTree()
  } else if (fileId === 'NDToolsListC3S3.json') {
    ndtoolsLoaded.value = false
    await loadNDToolsTree()
  }
}

async function onUpload(payload: any, callback: (result: { success: boolean, message: string }) => void) {
  try {
    // 创建 FormData 对象来上传文件
    const formData = new FormData()
    formData.append('name', payload.name)
    formData.append('seriesMin', payload.seriesMin?.toString() || '')
    formData.append('seriesMax', payload.seriesMax?.toString() || '')
    formData.append('description', payload.description)
    formData.append('helpUrl', payload.helpUrl)
    formData.append('contact', payload.contact || '')
    if (payload.file) {
      formData.append('file', payload.file)
    }

    // 调用上传 API
    const response = await axios.post('/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })

    console.log('上传成功:', response.data)

    // 上传成功后可以刷新数据
    // await getdata()

    callback({ success: true, message: '文件上传成功！' })

  } catch (error: any) {
    console.error('上传失败:', error)
    let errorMessage = '上传失败'

    if (error.response?.data?.message) {
      errorMessage = error.response.data.message
    } else if (error.message) {
      errorMessage = error.message
    }

    callback({ success: false, message: errorMessage })
  }
}
</script>

<style scoped>
.browse-page {
  --space-xs: 4px;
  --space-sm: 8px;
  --space-md: 16px;
  --space-lg: 24px;
  --space-xl: 32px;
  --browse-primary-wash: rgba(var(--v-theme-primary), 0.06);
  --browse-surface-border: rgba(var(--v-border-color), var(--v-border-opacity));
  padding: var(--space-md);
}

@media (min-width: 960px) {
  .browse-page {
    padding: var(--space-lg);
  }
}

.browse-tab {
  display: flex;
  flex-direction: column;
  gap: var(--space-lg);
}

.page-header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-md);
  padding-bottom: var(--space-md);
  border-bottom: 1px solid var(--browse-surface-border);
}

.page-header__title {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
}

.page-header__icon {
  flex-shrink: 0;
  opacity: 0.92;
}

.page-header__lead {
  max-width: 65ch;
  text-wrap: pretty;
}

.page-header__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-sm);
  flex-shrink: 0;
}

.browse-content {
  min-height: 200px;
}

.browse-state {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 240px;
  padding: var(--space-xl);
  border-radius: 4px;
}

.browse-state--loading {
  background: var(--browse-primary-wash);
  border: 1px solid var(--browse-surface-border);
}

.browse-alert {
  border: 1px solid rgba(var(--v-theme-error), 0.24);
}

.browse-tree-panel {
  border: 1px solid var(--browse-surface-border);
  border-radius: 4px;
  background: rgb(var(--v-theme-surface));
  overflow: hidden;
}

.browse-tree :deep(.v-treeview-item) {
  border-bottom: 1px solid rgba(var(--v-border-color), calc(var(--v-border-opacity) * 0.6));
  content-visibility: auto;
  contain-intrinsic-size: auto 48px;
  transition: background-color 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}

.browse-tree :deep(.v-treeview-item--active) {
  background: rgba(var(--v-theme-primary), 0.12);
}

.browse-tree :deep(.v-treeview-item:hover:not(.v-treeview-item--active)) {
  background: rgba(var(--v-theme-primary), 0.04);
}

.tree-row-title--folder {
  font-weight: 500;
}

.tree-row-subtitle {
  display: block;
  max-width: 48ch;
  text-wrap: pretty;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
}

.tree-row-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: var(--space-sm);
  max-width: min(100%, 520px);
}

.tree-path-chip {
  max-width: 200px;
}

.tree-path-chip :deep(.v-chip__content) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tree-path-chip--quick :deep(.v-chip__content) {
  color: rgb(13, 71, 161);
}

.tree-path-chip--standard :deep(.v-chip__content) {
  color: rgb(62, 56, 0);
}

.v-theme--dark .tree-path-chip--quick :deep(.v-chip__content) {
  color: rgb(187, 222, 251);
}

.v-theme--dark .tree-path-chip--standard :deep(.v-chip__content) {
  color: rgb(255, 249, 196);
}

.tree-series-chip :deep(.v-icon) {
  opacity: 0.85;
}

.tree-row-meta__date {
  white-space: nowrap;
  flex-shrink: 0;
}

.tree-row-meta__muted {
  opacity: 0.45;
}

@media (prefers-reduced-motion: reduce) {
  .browse-tree :deep(.v-treeview-item) {
    transition: none;
  }
}
</style>
