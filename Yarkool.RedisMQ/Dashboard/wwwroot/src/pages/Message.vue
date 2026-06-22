<template>
  <v-container class="message-page" fluid>
    <div class="message-layout">
      <section class="status-pane">
        <v-card class="status-card" elevation="2">
          <v-card-title>消息状态</v-card-title>
          <v-list class="status-list">
            <v-list-item
              v-for="status in statusList"
              :key="status.value ?? 'All'"
              :active="selectedStatus === status.value"
              @click="handleStatusChange(status.value)"
            >
              <v-list-item-title class="status-list-item">
                <span class="status-label">{{ status.label }}</span>
                <v-chip class="status-count" color="primary" size="small">
                  {{ getStatusCount(status.value) }}
                </v-chip>
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>
      </section>
      <section class="message-list-pane">
        <v-card class="message-list-card" elevation="2">
          <v-card-title class="message-card-title d-flex align-center">
            <span>消息列表</span>
            <v-spacer />
            <v-btn
              class="batch-delete-btn"
              color="error"
              :disabled="!selected.length"
              size="small"
              @click="handleDelete"
            >
              <v-icon>mdi-delete</v-icon>
              删除
            </v-btn>
          </v-card-title>
          <div class="message-table-wrap">
            <v-data-table-server
              v-model="selected"
              class="message-table"
              fixed-header
              :headers="headers"
              hide-default-footer
              item-value="message"
              :items="messages"
              :items-length="totalCount"
              :items-per-page="pageSize"
              :page="pageIndex"
              :show-select="!smAndDown"
            >
              <template #item.messageId="{ item }">
                <span class="message-id">{{ item.message.messageId }}</span>
              </template>
              <template #item.queueName="{ item }">
                {{ item.message.queueName }}
              </template>
              <template #item.status="{ item }">
                <v-chip class="text-white" :color="getStatusColor(item.status)" size="small">
                  {{ MessageStatus[item.status] }}
                </v-chip>
              </template>
              <template #item.delay="{ item }">
                <span v-if="item.message.delayTime > 0" class="delay-cell">
                  <span class="delay-time text-info">{{ formatDelayTime(item.message.delayTime) }}</span>
                </span>
                <span v-else class="delay-cell">-</span>
              </template>
              <template #item.createTime="{ item }">
                {{ new Date(item.message.createTimestamp).toLocaleString() }}
              </template>
              <template #item.actions="{ item }">
                <div class="row-actions">
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
          </div>
          <div class="message-pagination">
            <div class="page-size-control">
              <span class="page-size-label">Items per page:</span>
              <v-select
                v-model="pageSize"
                class="page-size-select"
                density="compact"
                hide-details
                :items="pageSizeOptions"
                variant="outlined"
                @update:model-value="handlePageSizeChange"
              />
            </div>
            <div class="page-range-text">{{ pageRangeText }}</div>
            <v-pagination
              v-model="pageIndex"
              density="comfortable"
              :length="pageCount"
              :total-visible="7"
              @update:model-value="handlePageIndexChange"
            />
          </div>
        </v-card>
      </section>
    </div>

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
                <v-col v-if="selectedMessage && selectedMessage?.message.delayTime > 0" cols="12" sm="4">
                  <strong>延迟时间：</strong>
                  {{ selectedMessage?.message.delayTime }}秒
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
  import { computed, onMounted, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { useDisplay } from 'vuetify'
  import { messageApi } from '@/apis';
  import type MessageResponse from '@/apis/response/MessageResponse';
  import { MessageStatus } from '@/types/MessageStatus';
  import { useAppStore } from '@/stores/app';
  import type { MessageDataResponse } from '@/apis/response/MessageResponse';
  import toast from '@/plugins/toast';
  import type { DataTableHeader } from 'vuetify';

  const store = useAppStore();
  const { smAndDown } = useDisplay();
  const selected = ref<MessageDataResponse[]>([]);
  const desktopHeaders: DataTableHeader[] = [
    { title: '消息ID', key: 'messageId', minWidth: '240px' },
    { title: '队列', key: 'queueName', minWidth: '100px' },
    { title: '状态', key: 'status', align: 'center', width: '120px' },
    { title: '延迟', key: 'delay', align: 'center', width: '150px', minWidth: '150px' },
    { title: '创建时间', key: 'createTime', width: '170px', align: 'center' },
    { title: '操作', key: 'actions', sortable: false, width: '120px', align: 'center' },
  ];
  const mobileHeaders: DataTableHeader[] = [
    { title: '消息ID', key: 'messageId', minWidth: '240px' },
    { title: '队列', key: 'queueName', minWidth: '90px' },
    { title: '状态', key: 'status', align: 'center', width: '100px' },
    { title: '延迟', key: 'delay', align: 'center', width: '150px', minWidth: '150px' },
    { title: '创建时间', key: 'createTime', width: '160px', align: 'center' },
    { title: '操作', key: 'actions', sortable: false, width: '110px', align: 'center' },
  ];
  const headers = computed(() => smAndDown.value ? mobileHeaders : desktopHeaders);
  const messages = ref<MessageResponse[]>([]);

  // 分页参数
  const pageIndex = ref(1);
  const pageSize = ref(20);
  const pageSizeOptions = [10, 20, 50, 100];
  const selectedStatus = ref<MessageStatus | null>(null);
  const totalCount = computed(() => getStatusCount(selectedStatus.value));
  const pageCount = computed(() => Math.max(Math.ceil(totalCount.value / pageSize.value), 1));
  const pageRangeText = computed(() => {
    if (totalCount.value === 0) return '0-0 of 0';

    const start = (pageIndex.value - 1) * pageSize.value + 1;
    const end = Math.min(pageIndex.value * pageSize.value, totalCount.value);
    return `${start}-${end} of ${totalCount.value}`;
  });

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

  const handlePageIndexChange = () => {
    fetchMessages();
  };

  const handlePageSizeChange = () => {
    pageIndex.value = 1;
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

  const formatDelayTime = (delayTime: number): string => {
    const totalSeconds = Math.floor(delayTime);
    if (totalSeconds <= 0) return '-';

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    if (hours > 0) {
      return [
        `${hours}小时`,
        minutes > 0 ? `${minutes}分` : '',
        seconds > 0 ? `${seconds}秒` : '',
      ].filter(Boolean).join('');
    }

    if (minutes > 0) {
      return [
        `${minutes}分`,
        seconds > 0 ? `${seconds}秒` : '',
      ].filter(Boolean).join('');
    }

    return `${seconds}秒`;
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
      selected.value = []
      fetchMessages()
    }
  })

  watch(pageCount, count => {
    if (pageIndex.value > count) {
      pageIndex.value = count
      fetchMessages()
    }
  })

  onMounted(() => {
    selectedStatus.value = routeStatus.value
    fetchMessages()
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
.message-page {
  height: 100%;
  max-width: 100%;
  overflow: visible;
  padding: 0;
}

.message-layout {
  display: grid;
  grid-template-columns: minmax(180px, 220px) minmax(0, 1fr);
  gap: 16px;
  height: 100%;
  min-height: 0;
}

.status-pane,
.message-list-pane {
  display: flex;
  min-height: 0;
  min-width: 0;
}

.status-card,
.message-list-card {
  height: 100%;
  width: 100%;
}

.message-list-card {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
  min-height: 0;
  overflow: hidden;
}

.message-dialog {
  height: 80vh;
  display: flex;
  flex-direction: column;
}

.status-list-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  min-width: 0;
}

.status-label {
  flex: 1 1 auto;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status-count {
  flex: 0 0 auto;
  min-width: 36px;
  justify-content: center;
}

.message-card-title {
  gap: 12px;
}

.message-table-wrap {
  min-height: 0;
  min-width: 0;
  width: 100%;
  overflow: hidden;
}

.message-table {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  min-width: 0;
  width: 100%;
}

.message-table :deep(.v-table__wrapper) {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
}

.message-table :deep(.v-table__wrapper > table) {
  min-width: 720px;
}

.message-id {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
  white-space: nowrap;
}

.row-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 112px;
}

.delay-cell,
.delay-time {
  display: inline-block;
  white-space: nowrap;
}

.delay-cell {
  width: 100%;
  text-align: center;
}

.message-pagination {
  display: flex;
  align-items: center;
  min-height: 64px;
  justify-content: flex-end;
  gap: 16px;
  padding: 10px 16px;
  border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  background: rgb(var(--v-theme-surface));
}

.page-size-control {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-size-label,
.page-range-text {
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-size: 14px;
  white-space: nowrap;
}

.page-size-select {
  width: 92px;
}

.dialog-title {
  flex: 0 0 auto;
}

.dialog-content {
  flex: 1 1 auto;
  overflow-y: auto;
  overflow-wrap: anywhere;
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

@media (max-width: 960px) {
  .message-page {
    height: auto;
    overflow: visible;
  }

  .message-layout {
    display: flex;
    flex-direction: column;
    gap: 16px;
    height: auto;
  }

  .status-pane,
  .message-list-pane,
  .status-card,
  .message-list-card {
    height: auto;
  }

  .message-table :deep(.v-table__wrapper) {
    max-height: 58vh;
  }

  .status-card :deep(.v-card-title) {
    padding-bottom: 8px;
  }

  .status-list {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 6px;
    padding: 0 12px 12px;
  }

  .status-list :deep(.v-list-item) {
    border-radius: 6px;
    padding-inline: 8px;
  }

  .message-table {
    min-width: 430px;
  }
}

@media (max-width: 600px) {
  .status-list {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .status-list-item {
    gap: 4px;
  }

  .status-label {
    flex: 0 1 auto;
    font-size: 14px;
  }

  .status-count {
    min-width: 42px;
  }

  .message-card-title {
    align-items: flex-start !important;
    flex-wrap: wrap;
    padding: 12px;
  }

  .message-card-title :deep(.v-spacer) {
    display: none;
  }

  .message-card-title :deep(.v-btn) {
    margin-left: auto;
  }

  .batch-delete-btn {
    display: none;
  }

  .message-table {
    min-width: 0;
  }

  .message-table :deep(.v-table__wrapper > table) {
    min-width: 850px;
  }

  .message-table :deep(th),
  .message-table :deep(td) {
    padding-inline: 8px !important;
  }

  .message-id {
    max-width: 240px;
  }

  .row-actions {
    min-width: 100px;
  }

  .row-actions :deep(.v-btn) {
    padding-inline: 4px;
  }

  .message-pagination {
    align-items: stretch;
    flex-direction: column;
    gap: 8px;
    padding: 12px;
  }

  .page-size-control {
    justify-content: space-between;
  }

  .page-range-text {
    text-align: center;
  }

  .message-pagination :deep(.v-pagination) {
    justify-content: center;
  }

  .message-pagination :deep(.v-pagination__list) {
    flex-wrap: wrap;
    justify-content: center;
  }

  .message-dialog {
    height: 92vh;
  }
}
</style>
