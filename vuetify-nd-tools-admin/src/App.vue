<template>
  <v-app>
    <v-app-bar app flat density="compact" class="elevation-4">
      <v-tabs v-model="activeTab" align-tabs="start" density="compact">
        <v-tab value="installbox">天晴安装器资源包</v-tab>
        <v-tab value="ndtools">盒子 - C3S3工具集</v-tab>
        <v-tab value="ndtoolsall">盒子-全工具集</v-tab>
      </v-tabs>
      <v-spacer />
      <v-btn icon @click="toggleTheme" :title="isDark ? '切换为亮色' : '切换为暗色'">
        <v-icon>{{ isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night' }}</v-icon>
      </v-btn>
    </v-app-bar>
    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>

<script lang="ts" setup>
import { useTheme } from 'vuetify'
import { computed, ref, provide } from 'vue'

const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)
const activeTab = ref<'installbox' | 'ndtools' | 'ndtoolsall'>('installbox')
provide('activeTab', activeTab)
function toggleTheme() {
  theme.global.name.value = isDark.value ? 'light' : 'dark'
}
</script>
