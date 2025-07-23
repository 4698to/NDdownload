<template>
  <v-app>
    <v-app-bar app flat density="compact" class="elevation-4">
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
import { computed } from 'vue'

const theme = useTheme()
const isDark = computed(() => theme.global.current.value.dark)
function toggleTheme() {
  theme.global.name.value = isDark.value ? 'light' : 'dark'
}
</script>
