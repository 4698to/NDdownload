<template>
  <v-container fluid>
    <v-tabs v-model="tab" bg-color="primary" dark>
      <v-tab value="installbox">天晴安装器资源包</v-tab>
      <v-tab value="ndtools">盒子 - C3S3工具集</v-tab>
      <v-tab value="ndtoolsall">盒子-全工具集</v-tab>
    </v-tabs>
    <v-window v-model="tab" class="mt-4">
      <v-window-item value="installbox">
        <v-card>
          <!-- <v-card-title>InstallBox 数据</v-card-title> -->
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
                <v-badge v-if="item.child.length > 0" color="info" :content="item.child.length">
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
        </v-card>
      </v-window-item>
      <v-window-item value="ndtools">
        <v-card>
          <!-- <v-card-title>C3S3工具集</v-card-title> -->
          <v-card-text>
            <NDToolsTree :items="ndtoolsTree" />
          </v-card-text>
        </v-card>
      </v-window-item>
      <v-window-item value="ndtoolsall">
        <v-card>
          <v-card-text>
            <NDToolsTree :items="ndtoolsAllTree" />
          </v-card-text>
        </v-card>
      </v-window-item>
    </v-window>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import axios from '@/plugins/axios'
import NDToolsTree from '@/components/NDToolsTree.vue'

const SeriesMin = 2015;
const SeriesMax = 2025;

const tab = ref('installbox')
const loading = ref(true)
const error = ref('')
const headers = ref<string[]>([])
const rows = ref<any[]>([])
const ndtoolsTree = ref<any[]>([])
const ndtoolsAllTree = ref<any[]>([])

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
    if (!Array.isArray(item.child)) {
      item.child = []
    } else {
      normalizeChildren(item.child)
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
    // 线上API数据
    const res = await axios.get('/download?fileid=InstallBox_version_full.json')
    const data = res.data
    const items = Array.isArray(data.item) ? data.item : []
    if (items.length > 0 && typeof items[0] === 'object') {
      normalizeChildren(items)
      headers.value = collectAllKeys(items)
      rows.value = items
    } else {
      error.value = '数据格式不正确'
    }
  } catch (e: any) {
    error.value = e.message || '未知错误'
  } finally {
    loading.value = false
  }
}

async function loadNDToolsTree() {
  try {
    const res = await axios.get('/download?fileid=NDToolsListC3S3.json')
    // 兼容根节点为对象或数组
    if (Array.isArray(res.data)) {
      ndtoolsTree.value = res.data
    } else {
      ndtoolsTree.value = [res.data]
    }
  } catch (e) {
    ndtoolsTree.value = []
  }
}

async function loadNDToolsAllTree() {
  try {
    const res = await axios.get('/download?fileid=NDToolsList.json')
    if (Array.isArray(res.data)) {
      ndtoolsAllTree.value = res.data
    } else {
      ndtoolsAllTree.value = [res.data]
    }
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
</script>
