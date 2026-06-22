<template>
  <v-container class="server-page" fluid>
    <v-row>
      <v-col cols="12">
        <v-card>
          <v-card-title>服务器节点列表</v-card-title>
          <v-card-text>
            <div class="table-wrap">
              <v-data-table
                class="server-table"
                :headers="headers"
                hide-default-footer
                :items="servers"
                :loading="loading"
              >
                <template #item.heartbeatTimestamp="{ item }">
                  {{ formatTimestamp(item.heartbeatTimestamp) }}
                </template>
                <template #item.status="{ item }">
                  <v-chip
                    :color="getServerStatus(item.heartbeatTimestamp).color"
                    size="small"
                  >
                    {{ getServerStatus(item.heartbeatTimestamp).text }}
                  </v-chip>
                </template>
              </v-data-table>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { onMounted, onUnmounted, ref } from 'vue'
  import { serverApi } from '@/apis'
  import type ServerNodeResponse from '@/apis/response/ServerNodeResponse'
  import type { DataTableHeader } from 'vuetify'

  const loading = ref(false)
  const servers = ref<ServerNodeResponse[]>([])

  const headers = ref<DataTableHeader[]>([
    { title: '服务器名称', key: 'serverName', align: 'start' },
    { title: '最后心跳时间', key: 'heartbeatTimestamp', align: 'center' },
    { title: '状态', key: 'status', align: 'center' },
  ])

  const formatTimestamp = (timestamp: number) => {
    return new Date(timestamp).toLocaleString()
  }

  const getServerStatus = (heartbeatTimestamp: number) => {
    const now = Date.now()
    const diff = now - heartbeatTimestamp

    if (diff > 30000) { // 30秒没有心跳认为离线
      return { color: 'error', text: '离线' }
    }
    return { color: 'success', text: '在线' }
  }

  const loadServers = async () => {
    loading.value = true
    try {
      const response = await serverApi.getList()
      servers.value = response.data
    } catch (error) {
      console.error('Failed to load servers:', error)
    } finally {
      loading.value = false
    }
  }

  // 定时刷新服务器状态
  let refreshInterval: number

  onMounted(() => {
    loadServers()
    refreshInterval = window.setInterval(() => {
      loadServers()
    }, 5000) // 每5秒刷新一次
  })

  onUnmounted(() => {
    if (refreshInterval) {
      clearInterval(refreshInterval)
    }
  })
</script>

<style scoped>
.server-page {
  max-width: 100%;
  padding: 0;
}

.table-wrap {
  width: 100%;
  overflow-x: hidden;
}

.table-wrap :deep(.v-table__wrapper) {
  overflow-x: auto;
}

.server-table {
  width: 100%;
  min-width: 0;
}

.server-table :deep(.v-table__wrapper > table) {
  min-width: 560px;
}

@media (max-width: 600px) {
  .server-page :deep(.v-card-text) {
    padding-inline: 8px;
  }
}
</style>
