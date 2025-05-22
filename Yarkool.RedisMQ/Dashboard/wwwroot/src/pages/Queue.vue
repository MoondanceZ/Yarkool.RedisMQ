<template>
  <v-container>
    <v-row>
      <v-col cols="12">
        <v-card>
          <v-card-title>
            队列
          </v-card-title>

          <v-card-text>
            <v-data-table
              fixed-header
              :headers="headers"
              hide-default-footer
              :items="queues"
              :loading="loading"
            >
              <template #item.status="{ item }">
                <v-chip
                  :color="getStatusColor(item.status)"
                  size="small"
                  :text="getStatusText(item.status)"
                />
              </template>
              <template #item.isDelayQueue="{ item }">
                <v-chip
                  :color="item.isDelayQueue ? 'purple' : 'teal'"
                  size="small"
                >
                  {{ item.isDelayQueue ? '延迟队列' : '普通队列' }}
                </v-chip>
              </template>
              <template #item.consumerList="{ item }">
                <div class="mb-2">
                  <div
                    v-for="consumer in item.consumerList"
                    :key="consumer"
                    class="mt-2"
                  >
                    <v-chip
                      color="info"
                      size="small"
                    >
                      {{ consumer }}
                    </v-chip>
                  </div>
                </div>
              </template>
            </v-data-table>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4">
      <v-col cols="12">
        <v-card>
          <v-card-title>
            消费者
          </v-card-title>

          <v-card-text>
            <v-data-table
              fixed-header
              :headers="consumerHeaders"
              hide-default-footer
              :items="consumers"
              :loading="loadingConsumers"
            >
              <template #item.queueName="{ item }">
                <v-chip
                  color="info"
                  size="small"
                >
                  {{ item.queueName }}
                </v-chip>
              </template>
            </v-data-table>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import { consumerApi, queueApi } from '@/apis'
  import type QueueResponse from '@/apis/response/QueueResponse'
  import { QueueStatus } from '@/types/QueueStatus'
  import type { DataTableHeader } from 'vuetify'
  import type ConsumerResponse from '@/apis/response/ConsumerResponse'

  const loading = ref(false)
  const headers = ref<DataTableHeader[]>([
    { title: '名称', key: 'queueName', align: 'start' },
    { title: '类型', key: 'isDelayQueue', align: 'center' },
    { title: '状态', key: 'status', align: 'center' },
    { title: '消费者', key: 'consumerList', align: 'center' },
  ])
  const queues = ref<QueueResponse[]>([])

  const getStatusColor = (status: QueueStatus) => {
    const colors = {
      [QueueStatus.Processing]: 'success',
      [QueueStatus.Stopping]: 'warning',
      [QueueStatus.Stopped]: 'error',
    }
    return colors[status]
  }

  const getStatusText = (status: QueueStatus) => {
    const texts = {
      [QueueStatus.Processing]: '运行中',
      [QueueStatus.Stopping]: '暂停中',
      [QueueStatus.Stopped]: '已停止',
    }
    return texts[status]
  }

  const loadQueues = async () => {
    loading.value = true
    try {
      const response = await queueApi.getList()
      queues.value = response.data
    } catch (error) {
      console.error('Failed to load queues:', error)
    } finally {
      loading.value = false
    }
  }

  // 消费者相关
  const loadingConsumers = ref(false)
  const consumers = ref<ConsumerResponse[]>([])
  const consumerHeaders = ref<DataTableHeader[]>([
    { title: '名称', key: 'consumerName', align: 'start' },
    { title: '服务器', key: 'serverName', align: 'center' },
    { title: '队列', key: 'queueName', align: 'center' },
  ])


  const loadConsumers = async () => {
    loadingConsumers.value = true
    try {
      const response = await consumerApi.getList()
      consumers.value = response.data
    } catch (error) {
      console.error('Failed to load consumers:', error)
    } finally {
      loadingConsumers.value = false
    }
  }

  onMounted(() => {
    loadQueues()
    loadConsumers()
  })
</script>

<style scoped>
</style>
