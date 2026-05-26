<template>
  <v-app>
    <v-app-bar app flat density="compact" class="elevation-4">
      <v-tabs v-if="!isAdminPage" v-model="activeTab" align-tabs="start" density="compact">
        <v-tab value="installbox">天晴安装器资源包</v-tab>
        <v-tab value="ndtools">盒子 - C3S3工具集</v-tab>
        <v-tab value="ndtoolsall">盒子-全工具集</v-tab>
      </v-tabs>
      <v-btn
        v-else
        variant="text"
        prepend-icon="mdi-arrow-left"
        to="/"
      >
        返回首页
      </v-btn>
      <v-spacer />
      <v-btn
        v-if="isAuthorized && !isAdminPage"
        variant="tonal"
        size="small"
        prepend-icon="mdi-table-edit"
        class="mr-2"
        to="/ndtools-edit"
      >
        数据管理
      </v-btn>
      <v-chip
        v-if="isAuthorized"
        color="success"
        variant="tonal"
        size="small"
        prepend-icon="mdi-shield-check"
        class="mr-2"
      >
        已授权编辑
      </v-chip>
      <v-btn
        v-if="isAuthorized"
        variant="text"
        size="small"
        prepend-icon="mdi-logout"
        class="mr-2"
        @click="logout"
      >
        退出编辑
      </v-btn>
      <v-btn
        v-else
        variant="tonal"
        size="small"
        prepend-icon="mdi-key"
        class="mr-2"
        @click="showKeyDialog = true"
      >
        输入编辑密钥
      </v-btn>
      <v-btn icon @click="toggleTheme" :title="isDark ? '切换为亮色' : '切换为暗色'">
        <v-icon>{{ isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night' }}</v-icon>
      </v-btn>
    </v-app-bar>
    <v-main>
      <router-view />
    </v-main>

    <DataKeyDialog v-model="showKeyDialog" @verified="onKeyVerified" />
  </v-app>
</template>

<script lang="ts" setup>
import { useTheme } from 'vuetify'
import { computed, onMounted, onUnmounted, provide, ref } from 'vue'
import { useRoute } from 'vue-router'
import DataKeyDialog from '@/components/DataKeyDialog.vue'
import { requestDataKeyKey, isAuthorizedKey } from '@/keys/dataKey'
import { clearDataKey, hasDataKey, onDataKeyChange } from '@/utils/dataKey'

const theme = useTheme()
const route = useRoute()
const isAdminPage = computed(() => route.path === '/ndtools-edit')
const isDark = computed(() => theme.global.current.value.dark)
const activeTab = ref<'installbox' | 'ndtools' | 'ndtoolsall'>('installbox')
provide('activeTab', activeTab)

const showKeyDialog = ref(false)
const isAuthorized = ref(hasDataKey())
let pendingCallback: (() => void) | null = null

function requestDataKey(onVerified: () => void) {
  if (hasDataKey()) {
    onVerified()
    return
  }
  pendingCallback = onVerified
  showKeyDialog.value = true
}

provide(requestDataKeyKey, requestDataKey)
provide(isAuthorizedKey, isAuthorized)

function onKeyVerified() {
  isAuthorized.value = true
  pendingCallback?.()
  pendingCallback = null
}

function logout() {
  clearDataKey()
  isAuthorized.value = false
}

function toggleTheme() {
  theme.global.name.value = isDark.value ? 'light' : 'dark'
}

let unsubscribe: (() => void) | undefined
onMounted(() => {
  unsubscribe = onDataKeyChange(() => {
    isAuthorized.value = hasDataKey()
  })
})
onUnmounted(() => {
  unsubscribe?.()
})
</script>
