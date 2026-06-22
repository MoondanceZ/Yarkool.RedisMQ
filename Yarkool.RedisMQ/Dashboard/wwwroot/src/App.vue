<template>
  <v-app>
    <v-app-bar class="app-bar" :elevation="2">
      <v-container class="app-bar-container d-flex align-center justify-center">
        <v-app-bar-title>Redis MQ Web</v-app-bar-title>

        <v-spacer />

        <div class="nav-actions d-flex align-center">
          <v-btn class="nav-btn" text to="/">
            <v-icon>mdi-home</v-icon>
            <span class="nav-label">首页</span>
          </v-btn>

          <v-btn class="nav-btn" text to="/message">
            <v-icon>mdi-message</v-icon>
            <span class="nav-label">消息</span>
          </v-btn>

          <v-btn class="nav-btn" text to="/queue">
            <v-icon>mdi-database</v-icon>
            <span class="nav-label">队列</span>
          </v-btn>

          <v-btn class="nav-btn" text to="/server">
            <v-icon>mdi-server</v-icon>
            <span class="nav-label">服务器</span>
          </v-btn>

          <v-btn icon @click="toggleTheme">
            <v-icon>{{ themeIcon }}</v-icon>
          </v-btn>
        </div>
      </v-container>
    </v-app-bar>

    <v-main class="app-main">
      <div class="main-content">
        <router-view />
      </div>
    </v-main>

    <v-footer class="px-4 footer-custom flex-0">
      <v-container class="footer-content d-flex justify-space-between align-center w-100 py-1">
        <div class="d-flex align-center">
          <v-icon class="me-2" color="primary" size="small">mdi-package-variant</v-icon>
          <span class="text-primary">Redis MQ: {{ stats.serverInfo?.redisMQVersion }}</span>
        </div>

        <div class="d-flex align-center">
          <v-icon class="me-2" color="info" size="small">mdi-database</v-icon>
          <span class="text-info">Redis: {{ stats.serverInfo?.redisVersion }}</span>
        </div>

        <div class="d-flex align-center">
          <v-icon class="me-2" color="success" size="small">mdi-clock-outline</v-icon>
          <span class="text-success">{{ formatTimestamp(stats.serverInfo?.serverTimestamp) }}</span>
        </div>
      </v-container>
    </v-footer>
  </v-app>
</template>

<script setup lang="ts">
  import { useTheme } from 'vuetify'
  import { useAppStore } from '@/stores/app'
  import { computed, onMounted, onUnmounted } from 'vue'

  const theme = useTheme()
  const store = useAppStore()
  const stats = computed(() => store.stats);

  // 主题切换
  function toggleTheme () {
    theme.global.name.value = theme.global.current.value.dark ? 'light' : 'dark'
  }

  const themeIcon = computed(() => {
    return theme.global.current.value.dark
      ? 'mdi-weather-sunny'
      : 'mdi-weather-night'
  })

  // 定时获取统计数据
  let statsTimer: ReturnType<typeof setInterval> | null = null

  onMounted(() => {
    const searchParams = new URLSearchParams(window.location.search);
    const accessToken = searchParams.get('access_token');
    if (accessToken) {
      localStorage.setItem('token', accessToken)
    }

    // 立即获取一次数据
    store.fetchStats()

    // 设置定时器，每5秒获取一次数据
    statsTimer = setInterval(() => {
      store.fetchStats()
    }, 5000)
  })

  onUnmounted(() => {
    // 组件卸载时清除定时器
    if (statsTimer) {
      clearInterval(statsTimer)
      statsTimer = null
    }
  })

  // 格式化时间戳
  const formatTimestamp = (timestamp: number) => {
    if (!timestamp) return '';
    const date = new Date(timestamp);
    return date.toLocaleString();
  }
</script>

<style scoped>
.footer-custom {
  flex: 0 1 auto;
  min-height: 40px;
  border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  font-size: 0.875rem;
}

.app-bar-container,
.footer-content {
  max-width: 100%;
}

.nav-actions {
  gap: 4px;
}

.nav-btn {
  min-width: 48px;
}

.app-main {
  flex: 1 1 auto;
  min-height: 0;
  overflow: hidden;
  padding-bottom: 0 !important;
}

.app-main :deep(.v-main__wrap) {
  display: flex;
  height: 100%;
  min-height: 0;
}

.main-content {
  flex: 1 1 auto;
  height: 100%;
  min-height: 0;
  overflow: hidden;
  padding: 16px;
}

@media (max-width: 600px) {
  .app-bar-container {
    padding-inline: 8px;
  }

  .app-bar :deep(.v-toolbar-title) {
    flex: 0 0 auto;
    font-size: 16px;
    margin-inline-start: 0;
  }

  .nav-actions {
    gap: 0;
  }

  .nav-btn {
    padding-inline: 8px;
  }

  .nav-label {
    display: none;
  }

  .main-content {
    padding: 8px;
  }

  .footer-content {
    align-items: flex-start !important;
    flex-direction: column;
    gap: 4px;
    padding-block: 8px !important;
  }
}

</style>
