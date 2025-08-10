<template>
  <v-sheet class="pa-2" color="success" density="compact">
    <div class="d-flex align-center mb-2">
      <v-label theme="light" color="white">提交任何插件的使用意见，如插件路径、使用说明、作者等信息，让我们一起来丰富工具库,方便所有人！</v-label>
      <v-spacer />
      <v-btn @click="openUpload = true" prepend-icon="mdi-file-upload-outline">上传</v-btn>
    </div>
    <v-text-field 
      v-model="searchText"
      clear-icon="mdi-close-circle-outline"
      prepend-inner-icon="mdi-magnify"
      label="搜索"
      variant="solo"
      clearable
      flat
      hide-details
      density="compact"
      class="mb-2"
    />
    
    
  </v-sheet>
  <v-treeview
    :items="filteredItems"
    :item-children="'Children'"
    :item-value="'PathId'"
    v-model:opened="openedModel"
    dense
    open-on-click
  >
    <template v-slot:title="{ item }">
      <span class="pa-3">{{item.Name }}</span>
    </template>
    <template v-slot:prepend="{ item }">
        <v-badge v-if="item.IsGrouping" color="info" :content="Array.isArray(item.Children) ? item.Children.length : 0">
            <v-icon color="warning">mdi-folder</v-icon>
        </v-badge>
        <v-icon v-else color="info" icon="mdi-file" >
        </v-icon>
    </template>
    <template v-slot:subtitle="{ item }">
        <span v-if="!item.IsGrouping" class="pa-3">
            {{ item.message }}
            <!-- {{`${item.StartupFolder}\\${item.SubPath}` }} -->
        </span>
    </template>
    <template v-slot:append="{ item }">
        <span v-if="!item.IsGrouping" class="pa-3">{{ `${item.StartupFolder}\\${item.SubPath}` }}</span>
        <span v-else class="pa-3">{{ item.Version }}</span>
      <v-chip v-if="item.ExtensionType" size="small" class="ml-2">{{ item.ExtensionType }}</v-chip>
        <v-chip v-if="item.hasHelp" size="small" class="ml-2" >
        <a :href="item.HelpUrl" target="_blank">
            <v-icon color="info">mdi-help-circle</v-icon>
        </a>
        </v-chip>
        <v-chip v-else  size="x-small" class="ml-2">
            <a target="_blank"><v-icon color="secondary">mdi-help-circle</v-icon></a>
        </v-chip>
        
    </template>

  </v-treeview>

  <UploadDialog v-model="openUpload" @submit="onUpload" title="提交插件路径或是上传插件" />
</template>

<script setup lang="ts">
import { defineProps, computed, ref, watch } from 'vue'
import axios from '@/plugins/axios'
import UploadDialog from '@/components/UploadDialog.vue'

type NDNode = {
  Name?: string
  IsGrouping?: boolean
  Children?: NDNode[] | null
  // 其他字段按需扩展
  [key: string]: any
}

const props = defineProps<{ items: NDNode[] }>()

const searchText = ref('')
const openUpload = ref(false)

function textMatches(node: NDNode, query: string): boolean {
  if (!query) return true
  const q = query.toLowerCase()
  const fields = [
    node.Name,
    node.message,
    node.SubPath,
    node.StartupFolder,
    node.ExtensionType,
  ]
  return fields.some(v => typeof v === 'string' && v.toLowerCase().includes(q))
}

function filterTree(nodes: NDNode[], query: string): NDNode[] {
  if (!Array.isArray(nodes)) return []
  if (!query) return nodes

  const result: NDNode[] = []

  for (const node of nodes) {
    const children = Array.isArray(node.Children) ? node.Children : []
    const filteredChildren = filterTree(children, query)
    const isMatch = textMatches(node, query)

    if (isMatch || filteredChildren.length > 0) {
      result.push({
        ...node,
        Children: filteredChildren.length > 0 ? filteredChildren : (isMatch ? node.Children : null),
      })
    }
  }
  return result
}

// 为每个节点生成稳定的 PathId，便于控制展开
function attachPathId(nodes: NDNode[], parentId = ''): NDNode[] {
  const list: NDNode[] = []
  const sep = ' / '
  for (const node of nodes) {
    const selfId = parentId ? `${parentId}${sep}${node.Name || node.SubPath || 'Item'}` : `${node.Name || node.SubPath || 'Item'}`
    const children = Array.isArray(node.Children) ? attachPathId(node.Children, selfId) : null
    list.push({ ...node, PathId: selfId, Children: children })
  }
  return list
}

const filteredItems = computed(() => {
  const base = filterTree(props.items || [], searchText.value.trim())
  return attachPathId(base)
})

// 展开控制：有搜索词时展开所有含有 Children 的分支，否则使用用户手动展开值
const userOpened = ref<string[]>([])
const openedModel = ref<string[]>([])

watch(filteredItems, (items) => {
  const q = searchText.value.trim()
  if (!q) {
    openedModel.value = [...userOpened.value]
    return
  }
  const allBranchIds: string[] = []
  function collectBranchIds(nodes: NDNode[]) {
    for (const n of nodes) {
      if (Array.isArray(n.Children) && n.Children.length > 0) {
        allBranchIds.push(n.PathId as string)
        collectBranchIds(n.Children)
      }
    }
  }
  collectBranchIds(items)
  openedModel.value = allBranchIds
}, { immediate: true })

// 同步用户手动展开状态（仅在非搜索时记录）
watch(openedModel, (val) => {
  if (!searchText.value.trim()) {
    userOpened.value = [...val]
  }
})

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
    // 这里可以触发父组件刷新或直接刷新当前数据
    
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