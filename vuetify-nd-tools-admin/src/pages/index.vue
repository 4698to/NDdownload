<template>
  <v-container fluid class="ma-0 pa-0">
    <!-- <v-tabs v-model="tab" bg-color="primary" dark>
      <v-tab value="installbox">天晴安装器资源包</v-tab>
      <v-tab value="ndtools">盒子 - C3S3工具集</v-tab>
      <v-tab value="ndtoolsall">盒子-全工具集</v-tab>
    </v-tabs> -->
    <v-window v-model="tab" class="mt-0">
      <v-window-item value="installbox">
        <v-card>

          <v-card-title class="d-flex align-center">
            <v-label >一个工具安装全网的3dsMax插件,让我们一起来丰富工具库,方便所有人！</v-label>
            <v-spacer />
            <v-btn v-if="isAuthorized" prepend-icon="mdi-pencil" variant="tonal" class="mr-2" to="/installbox-edit">编辑数据</v-btn>
            <v-btn prepend-icon="mdi-file-upload-outline" @click="requestUpload">上传</v-btn>
          </v-card-title>
          <v-card-text>
            <v-progress-circular v-if="loading" indeterminate color="primary" />
            <v-alert v-else-if="error" type="error">{{ error }}</v-alert>
            <v-treeview
              v-else
              :items="rows"
              :open-all="false"
              :item-children="'child'"
              activatable
              hoverable
              open-on-click
            >
            <template v-slot:title="{ item }">
                <span class="pa-3">
                    {{ item.zipname }}
                </span>
            </template>
            <template v-slot:prepend="{ item }">
                <v-badge v-if="hasChildren(item)" color="info" :content="getChildCount(item)">
                    <v-icon>mdi-folder</v-icon>
                </v-badge>
                <v-icon v-else color="info" icon="mdi-file" >
                    <!-- <v-icon>mdi-file</v-icon> -->
                </v-icon>
            </template>
            <template v-slot:subtitle="{ item }">
                <span class="pa-3">
                    {{ item.abouttext }}
                </span>
            </template>
            <template v-slot:append="{ item }">
                <v-chip v-if="item.dirpath" class="ml-2" :color="item.quick ? 'quick' : 'noquick'" variant="flat" style="max-width: 200px; min-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                    {{ getitempath(item) }}
                </v-chip>
                <v-chip v-if="item.SeriesMin !== 0 && item.SeriesMax !== 0" class="ml-2">
                   {{ item.SeriesMin }} - {{ item.SeriesMax }}
                </v-chip>
                <v-chip v-else class="ml-2">
                    {{ SeriesMin }} - {{ SeriesMax }}
                </v-chip>
                <v-chip v-if="item.helplink" size="x-small" class="ml-2" >
                    <a :href="item.helplink" target="_blank">
                        <v-icon color="info">mdi-help-circle</v-icon>
                    </a>
                </v-chip>
                <v-chip v-else  size="x-small" class="ml-2">
                    <a target="_blank"><v-icon color="secondary">mdi-help-circle</v-icon></a>
                </v-chip>
                <v-chip size="x-small" class="ml-2">
                    {{ formatDate(item.LastPackTime) }}
                </v-chip>
            </template>

            </v-treeview>
          </v-card-text>
          <UploadDialog v-model="openUpload" @submit="onUpload" :title="`上传插件提交到【天晴安装器】资源包`"/>
        </v-card>
      </v-window-item>
      <v-window-item value="ndtools">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-label>C3S3 工具集数据</v-label>
            <v-spacer />
            <v-btn v-if="isAuthorized" prepend-icon="mdi-pencil" variant="tonal" @click="openEditNDToolsC3S3 = true">编辑数据</v-btn>
          </v-card-title>
          <v-card-text>
            <NDToolsTree :items="ndtoolsTree" />
          </v-card-text>
        </v-card>
      </v-window-item>
      <v-window-item value="ndtoolsall">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-label>盒子全工具集数据</v-label>
            <v-spacer />
            <v-btn v-if="isAuthorized" prepend-icon="mdi-pencil" variant="tonal" @click="openEditNDTools = true">编辑数据</v-btn>
          </v-card-title>
          <v-card-text>
            <NDToolsTree :items="ndtoolsAllTree" />
          </v-card-text>
        </v-card>
      </v-window-item>
    </v-window>

    <JsonEditDialog
      v-model="openEditNDTools"
      file-id="NDToolsList.json"
      title="编辑 NDToolsList.json"
      @saved="onJsonSaved"
    />
    <JsonEditDialog
      v-model="openEditNDToolsC3S3"
      file-id="NDToolsListC3S3.json"
      title="编辑 NDToolsListC3S3.json"
      @saved="onJsonSaved"
    />
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, inject, type Ref } from 'vue'
import axios from '@/plugins/axios'
import NDToolsTree from '@/components/NDToolsTree.vue'
import UploadDialog from '@/components/UploadDialog.vue'
import JsonEditDialog from '@/components/JsonEditDialog.vue'
import { requestDataKeyKey, isAuthorizedKey } from '@/keys/dataKey'
import type { RequestDataKeyFn } from '@/keys/dataKey'

const SeriesMin = 2015;
const SeriesMax = 2025;

const injectedTab = inject<Ref<'installbox' | 'ndtools' | 'ndtoolsall'>>('activeTab')
const requestDataKey = inject<RequestDataKeyFn>(requestDataKeyKey)!
const isAuthorized = inject(isAuthorizedKey)!
const tab = injectedTab ?? ref<'installbox' | 'ndtools' | 'ndtoolsall'>('installbox')
const loading = ref(true)
const error = ref('')
const headers = ref<string[]>([])
const rows = ref<any[]>([])
const ndtoolsTree = ref<any[]>([])
const ndtoolsAllTree = ref<any[]>([])
const openUpload = ref(false)
const openEditNDTools = ref(false)
const openEditNDToolsC3S3 = ref(false)

function requestUpload() {
  requestDataKey(() => {
    openUpload.value = true
  })
}

function collectAllKeys(arr: any[]): string[] {
  const keys = new Set<string>()
  function walk(items: any[]) {
    for (const item of items) {
      Object.keys(item).forEach(k => {
        if (k !== 'child') keys.add(k)
      })
      if (Array.isArray(item.child)) walk(item.child)
    }
  }
  walk(arr)
  return Array.from(keys)
}

function normalizeChildren(arr: any[]): void {
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

function hasChildren(item: any): boolean {
  return Array.isArray(item?.child) && item.child.length > 0
}

function getChildCount(item: any): number {
  return Array.isArray(item?.child) ? item.child.length : 0
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
function getitempath(item: any) {
    if (item.type === 0) {
        return item.dirpath
    } else {
        if (item.dirpath) {
            return  `ApplicationPlugins/${item.dirpath}`
        } else {
            return item.targetpath
        }
    }
}
// 提取节点主标题
function itemTitle(item: any) {
  // 优先显示 zipname，其次 abouttext，否则显示 targetpath
  return item.zipname || item.abouttext || item.targetpath || '未命名'
}

function formatDate(val: string | number) {
  function toYMDHM(date: Date) {
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    const h = String(date.getHours()).padStart(2, '0')
    const min = String(date.getMinutes()).padStart(2, '0')
    return `${y}-${m}-${d} ${h}:${min}`
  }
  if (!val) return toYMDHM(new Date())
  // 支持 /Date(1749813715000+0800)/ 格式
  if (typeof val === 'string' && val.startsWith('/Date(')) {
    const match = val.match(/\/Date\((\-?\d+)([+-]\d+)?\)\//)
    if (match) {
      const ms = parseInt(match[1], 10)
      if (ms <= -62135596800000) return toYMDHM(new Date())
      const date = new Date(ms)
      if (date.getFullYear() < 1970) return toYMDHM(new Date())
      return toYMDHM(date)
    }
    return toYMDHM(new Date())
  }
  // 普通时间戳
  const num = typeof val === 'string' ? parseInt(val, 10) : val
  if (!isNaN(num) && num > 1000000000000) {
    const date = new Date(num)
    if (date.getFullYear() < 1970) return toYMDHM(new Date())
    return toYMDHM(date)
  }
  return toYMDHM(new Date())
}

async function getdata() {
  try {
    loading.value = true
    error.value = ''
    //const res = await axios.get('/download?fileid=InstallBox_version_full.json')
    const res = await axios.get('/download?fileid=InstallBox_version_full.json')

    let data: any = res.data
    if (typeof data === 'string') {
      try {
        const trimmed = data.replace(/^\uFEFF/, '').trim()
        data = JSON.parse(trimmed)
      } catch (e) {
        error.value = '返回的不是有效的 JSON 数据'
        rows.value = []
        return
      }
    }

    const items: any[] = Array.isArray(data?.item)
      ? data.item
      : Array.isArray(data)
        ? data
        : []

    if (Array.isArray(items) && items.length >= 0) {
      normalizeChildren(items)
      headers.value = collectAllKeys(items)
      rows.value = items
    } else {
      error.value = '数据格式不正确'
      rows.value = []
    }
  } catch (e: any) {
    error.value = e?.message || '未知错误'
    rows.value = []
  } finally {
    loading.value = false
  }
}

async function loadNDToolsTree() {
  try {
    const res = await axios.get('/download?fileid=NDToolsListC3S3.json')
    let data: any = res.data
    if (typeof data === 'string') {
      try {
        const trimmed = data.replace(/^\uFEFF/, '').trim()
        data = JSON.parse(trimmed)
      } catch (e) {
        ndtoolsTree.value = []
        return
      }
    }
    const items: any[] = Array.isArray(data) ? data : [data]
    normalizeNDChildren(items)
    ndtoolsTree.value = items
  } catch (e) {
    ndtoolsTree.value = []
  }
}

async function loadNDToolsAllTree() {
  try {
    const res = await axios.get('/download?fileid=NDToolsList.json')
    let data: any = res.data
    if (typeof data === 'string') {
      try {
        const trimmed = data.replace(/^\uFEFF/, '').trim()
        data = JSON.parse(trimmed)
      } catch (e) {
        ndtoolsAllTree.value = []
        return
      }
    }
    const items: any[] = Array.isArray(data) ? data : [data]
    normalizeNDChildren(items)
    ndtoolsAllTree.value = items
  } catch (e) {
    ndtoolsAllTree.value = []
  }
}

onMounted(async () => {
    await getdata();
    await loadNDToolsTree()
    await loadNDToolsAllTree()
})

const headersForDataTable = computed(() =>
  headers.value.map(h => ({ text: h, value: h }))
)

async function onJsonSaved(fileId: string) {
  if (fileId === 'NDToolsList.json') {
    await loadNDToolsAllTree()
  } else if (fileId === 'NDToolsListC3S3.json') {
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
