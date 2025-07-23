<template>
  <v-treeview
    :items="items"
    :open-all="false"
    :item-children="'Children'"
    activatable
    hoverable
    open-on-click
  >
    <template v-slot:title="{ item }">
      <span class="pa-3">{{item.Name }}</span>
    </template>
    <template v-slot:prepend="{ item }">
        <v-badge v-if="item.IsGrouping" color="info" :content="item.Children.length">
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
</template>

<script setup lang="ts">
import { defineProps } from 'vue'
const props = defineProps<{ items: any[] }>()
</script> 