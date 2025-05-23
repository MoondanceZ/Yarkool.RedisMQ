<template>
  <v-container>
    <v-row>
      <v-col cols="2">
        <v-card>
          <v-card-title>消息状态</v-card-title>
          <v-list>
            <v-list-item
              v-for="status in statusList"
              :key="status.value || 'All'"
              :active="selectedStatus === status.value"
              @click="handleStatusChange(status.value)"
            >
              <v-list-item-title>
                {{ status.label }}
                <v-chip class="ml-2" color="primary" size="small">
                  {{ getStatusCount(status.value) }}
                </v-chip>
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>
      <v-col cols="10">
        <v-card>
          <v-card-title class="d-flex align-center">
            消息列表
            <v-spacer />
            <v-btn color="error" :disabled="!selected.length" @click="handleDelete">
              <v-icon>mdi-delete</v-icon>
              删除
            </v-btn>
          </v-card-title>
          <v-data-table-server
            v-model="selected"
            :headers="headers"
            item-value="message"
            :items="messages"
            :items-length="totalCount"
            :items-per-page="pageSize"
            :page="pageIndex"
            show-select
            @update:options="handlePageChange"
          >
            <template #item.messageId="{ item }">
              {{ item.message.messageId }}
            </template>
            <template #item.queueName="{ item }">
              {{ item.message.queueName }}
            </template>
            <template #item.status="{ item }">
              <v-chip class="text-white" :color="getStatusColor(item.status)" size="small">
                {{ MessageStatus[item.status] }}
              </v-chip>
            </template>
            <template #item.createTime="{ item }">
              {{ new Date(item.message.createTimestamp).toLocaleString() }}
            </template>
            <template #item.actions="{ item }">
              <div class="d-flex">
                <v-btn
                  color="error"
                  size="small"
                  variant="text"
                  @click="handleDeleteOne(item)"
                >
                  删除
                </v-btn>
                <v-divider inset vertical />
                <v-btn
                  color="primary"
                  size="small"
                  variant="text"
                  @click="handleView(item)"
                >
                  查看
                </v-btn>
              </div>
            </template>
          </v-data-table-server>
        </v-card>
      </v-col>
    </v-row>

    <!-- 消息详情对话框 -->
    <v-dialog v-model="dialogVisible" max-width="800">
      <v-card class="message-dialog">
        <v-card-title class="dialog-title">消息详情</v-card-title>
        <v-card-text class="dialog-content">
          <v-tabs v-model="activeTab">
            <v-tab value="details">基本信息</v-tab>
            <v-tab v-if="selectedMessage?.errorInfo" value="error">错误信息</v-tab>
          </v-tabs>

          <v-window v-model="activeTab" class="mt-4">
            <v-window-item value="details">
              <v-row>
                <v-col cols="12" sm="8">
                  <strong>消息ID：</strong>
                  {{ selectedMessage?.message.messageId }}
                </v-col>
                <v-col cols="12" sm="4">
                  <strong>队列名：</strong>
                  {{ selectedMessage?.message.queueName }}
                </v-col>
                <v-col cols="12" sm="8">
                  <strong>状态：</strong>
                  <v-chip
                    v-if="selectedMessage"
                    class="text-white"
                    :color="getStatusColor(selectedMessage?.status)"
                    size="small"
                  >
                    {{ MessageStatus[selectedMessage?.status] }}
                  </v-chip>
                </v-col>
                <v-col cols="12" sm="6">
                  <strong>创建时间：</strong>
                  {{ selectedMessage?.message.createTimestamp ? new Date(selectedMessage.message.createTimestamp).toLocaleString() : '' }}
                </v-col>
                <v-col cols="12">
                  <strong>消息内容：</strong>
                  <v-card class="mt-2 pa-2 message-content-card" variant="outlined">
                    <pre class="message-content">{{ formatJson(selectedMessage?.message.messageContent) }}</pre>
                  </v-card>
                </v-col>
              </v-row>
            </v-window-item>

            <v-window-item value="error">
              <v-row>
                <v-col cols="12" sm="6">
                  <strong>重试次数：</strong>
                  <v-chip
                    :color="selectedMessage?.executionTimes && selectedMessage?.executionTimes - 1 > 0 ? 'warning' : 'success'"
                    size="small"
                  >
                    {{ selectedMessage && selectedMessage?.executionTimes - 1 }}
                  </v-chip>
                </v-col>
                <v-col cols="12" sm="6">
                  <strong>异常时间：</strong>
                  {{ selectedMessage?.errorInfo?.errorMessageTimestamp ? new Date(selectedMessage.errorInfo.errorMessageTimestamp).toLocaleString() : '' }}
                </v-col>
                <v-col cols="12">
                  <strong>异常信息：</strong>
                  <v-alert
                    class="mt-2 error-message"
                    density="compact"
                    type="error"
                    variant="tonal"
                  >
                    {{ selectedMessage?.errorInfo?.exceptionMessage }}
                  </v-alert>
                </v-col>
                <v-col cols="12">
                  <strong>堆栈跟踪：</strong>
                  <v-card class="mt-2 pa-2 stack-trace-card" variant="outlined">
                    <pre class="stack-trace">{{ selectedMessage?.errorInfo?.stackTrace }}</pre>
                  </v-card>
                </v-col>
              </v-row>
            </v-window-item>
          </v-window>
        </v-card-text>
        <v-card-actions class="dialog-actions">
          <v-spacer />
          <v-btn color="primary" @click="dialogVisible = false">关闭</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
  import { computed, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { messageApi } from '@/apis';
  import type MessageResponse from '@/apis/response/MessageResponse';
  import { MessageStatus } from '@/types/MessageStatus';
  import { useAppStore } from '@/stores/app';
  import type { MessageDataResponse } from '@/apis/response/MessageResponse';
  import toast from '@/plugins/toast';
  import type { DataTableHeader } from 'vuetify';

  const store = useAppStore();
  const selected = ref<MessageDataResponse[]>([]);
  const headers = ref<DataTableHeader[]>([
    { title: '消息ID', key: 'messageId' },
    { title: '队列', key: 'queueName' },
    { title: '状态', key: 'status', align: 'center' },
    { title: '创建时间', key: 'createTime', width: '160px', align: 'center' },
    { title: '操作', key: 'actions', sortable: false, width: '100px', align: 'center' },
  ]);
  const messages = ref<MessageResponse[]>([]);

  // 分页参数
  const pageIndex = ref(1);
  const pageSize = ref(10);
  const selectedStatus = ref<MessageStatus | null>(null);
  const totalCount = computed(() => getStatusCount(selectedStatus.value));

  // 状态列表
  const statusList = computed(() => [
    { label: 'All', value: null },
    { label: 'Pending', value: MessageStatus.Pending },
    { label: 'Processing', value: MessageStatus.Processing },
    { label: 'Retrying', value: MessageStatus.Retrying },
    { label: 'Completed', value: MessageStatus.Completed },
    { label: 'Failed', value: MessageStatus.Failed },
  ]);

  // 获取状态对应的计数
  const getStatusCount = (status: MessageStatus | null): number => {
    const stats = store.stats.realTimeStats;
    if (!stats) return 0;

    switch (status) {
      case MessageStatus.Pending:
        return stats.pendingCount;
      case MessageStatus.Processing:
        return stats.processingCount;
      case MessageStatus.Retrying:
        return stats.retryingCount;
      case MessageStatus.Completed:
        return stats.completedCount;
      case MessageStatus.Failed:
        return stats.failedCount;
      default:
        return stats.allCount;
    }
  };

  const fetchMessages = async () => {
    try {
      const response = await messageApi.getList({
        pageIndex: pageIndex.value,
        pageSize: pageSize.value,
        status: selectedStatus.value ?? undefined,
      });
      if (response.code === 0) {
        messages.value = response.data;
      } else {
        console.log(`fetch message failed: ${response.message}`);
      }
    } catch(error) {
      console.log(`fetch message error: ${error}`);
    }
  };

  const handlePageChange = (options: { page: number; itemsPerPage: number }) => {
    pageIndex.value = options.page;
    pageSize.value = options.itemsPerPage;
    fetchMessages();
  };

  // 获取状态对应的颜色
  const getStatusColor = (status: MessageStatus): string => {
    switch (status) {
      case MessageStatus.Pending:
        return 'grey';
      case MessageStatus.Processing:
        return 'blue';
      case MessageStatus.Retrying:
        return 'orange';
      case MessageStatus.Completed:
        return 'success';
      case MessageStatus.Failed:
        return 'error';
      default:
        return 'grey';
    }
  };

  // 修改删除处理函数
  const handleDelete = async () => {
    try {
      const response = await messageApi.delete(selected.value.map(x => x.messageId));
      if (response.code === 0) {
        toast.success(response.message);
        // 刷新数据
        fetchMessages();
        // 清空选择
        selected.value = [];
      } else {
        toast.error(response.message);
      }
    } catch {
      toast.error('删除失败');
    }
  };

  // 对话框相关
  const dialogVisible = ref(false);
  const selectedMessage = ref<MessageResponse | null>(null);
  const activeTab = ref('details'); // 已经设置为 'details'

  // 查看消息详情
  const handleView = (item: MessageResponse) => {
    selectedMessage.value = item;
    activeTab.value = 'details'; // 每次打开对话框时重置为第一个标签页
    dialogVisible.value = true;
  };

  // 格式化 JSON 字符串
  const formatJson = (jsonString: string | undefined): string => {
    if (!jsonString) return '';
    try {
      const obj = JSON.parse(jsonString);
      return JSON.stringify(obj, null, 2);
    } catch {
      return jsonString;
    }
  };

  // 删除单条消息
  const handleDeleteOne = async (item: MessageResponse) => {
    try {
      const response = await messageApi.delete([item.message.messageId]);
      if (response.code === 0) {
        toast.success(response.message);
        // 刷新数据
        fetchMessages();
      } else {
        toast.error(response.message);
      }
    } catch {
      toast.error('删除失败');
    }
  };

  const route = useRoute()
  const router = useRouter()

  // 从路由参数获取状态
  const routeStatus = computed(() => {
    const status = route.query.status
    return status ? Number(status) as MessageStatus : null
  })

  // 监听路由参数变化
  watch(routeStatus, newStatus => {
    if (selectedStatus.value !== newStatus) {
      selectedStatus.value = newStatus
      pageIndex.value = 1 // 重置页码
      fetchMessages()
    }
  })

  // 修改状态切换处理函数
  const handleStatusChange = (status: MessageStatus | null) => {
    // 更新路由参数
    router.push({
      query: {
        ...route.query,
        status: status?.toString() || undefined,
      },
    })
  }
</script>

<style scoped>
.message-dialog {
  height: 80vh;
  display: flex;
  flex-direction: column;
}

.dialog-title {
  flex: 0 0 auto;
}

.dialog-content {
  flex: 1 1 auto;
  overflow-y: auto;
  padding: 16px;
}

.dialog-actions {
  flex: 0 0 auto;
}

.message-content {
  white-space: pre-wrap;
  word-wrap: break-word;
  font-family: monospace;
  font-size: 14px;
  margin: 0;
}

.stack-trace-card {
  background-color: #2b2b2b;
}

.stack-trace {
  white-space: pre-wrap;
  word-wrap: break-word;
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 13px;
  line-height: 1.5;
  margin: 0;
  color: #e6e6e6;
  max-height: 400px;
  overflow-y: auto;
  padding: 8px;
}

.stack-trace::-webkit-scrollbar {
  width: 8px;
}

.stack-trace::-webkit-scrollbar-track {
  background: #1e1e1e;
  border-radius: 4px;
}

.stack-trace::-webkit-scrollbar-thumb {
  background: #666;
  border-radius: 4px;
}

.stack-trace::-webkit-scrollbar-thumb:hover {
  background: #888;
}

.message-content-card {
  background-color: #2b2b2b;
}

.message-content {
  white-space: pre-wrap;
  word-wrap: break-word;
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 13px;
  line-height: 1.5;
  padding: 8px 12px;
  margin: 0;
  color: #e6e6e6;
  max-height: 400px;
  overflow-y: auto;
  padding: 8px;
}

.message-content::-webkit-scrollbar {
  width: 8px;
}

.message-content::-webkit-scrollbar-track {
  background: #1e1e1e;
  border-radius: 4px;
}

.message-content::-webkit-scrollbar-thumb {
  background: #666;
  border-radius: 4px;
}

.message-content::-webkit-scrollbar-thumb:hover {
  background: #888;
}
</style>
