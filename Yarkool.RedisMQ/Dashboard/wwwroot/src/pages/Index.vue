<template>
  <v-container>
    <v-row>
      <v-col cols="9">
        <v-card class="pa-2" elevation="2">
          <v-card-title>实时数据</v-card-title>
          <div ref="chartRef" :style="{ height: '242px', width: '100%' }" />
        </v-card>
      </v-col>
      <v-col cols="3">
        <v-row>
          <v-col cols="6">
            <v-card
              class="cursor-pointer"
              elevation="2"
              @click="router.push('/queue')"
            >
              <v-card-title class="text-subtitle-1">队列数量</v-card-title>
              <v-card-text class="text-h6 text-center text-primary">{{ stats.serverInfo?.queueCount || 0 }}</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card
              class="cursor-pointer"
              elevation="2"
              @click="router.push('/queue')"
            >
              <v-card-title class="text-subtitle-1">消费者数量</v-card-title>
              <v-card-text class="text-h6 text-center text-primary">{{ stats.serverInfo?.consumerCount || 0 }}</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card
              class="cursor-pointer"
              elevation="2"
              @click="router.push('/server')"
            >
              <v-card-title class="text-subtitle-1">服务器数量</v-card-title>
              <v-card-text class="text-h6 text-center text-primary">{{ stats.serverInfo?.serverCount || 0 }}</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card
              elevation="2"
              @click="router.push('/message')"
            >
              <v-card-title class="text-subtitle-1">消息数量</v-card-title>
              <v-card-text class="text-h6 text-center text-primary">{{ stats.realTimeStats?.pendingCount || 0 }}</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card
              elevation="2"
              @click="router.push('/message?status=4')"
            >
              <v-card-title class="text-subtitle-1">错误数量</v-card-title>
              <v-card-text class="text-h6 text-center text-red">{{ stats.realTimeStats?.failedCount || 0 }}</v-card-text>
            </v-card>
          </v-col>
          <v-col cols="6">
            <v-card
              elevation="2"
              @click="router.push('/message')"
            >
              <v-card-title class="text-subtitle-1">Ack</v-card-title>
              <v-card-text class="text-h6 text-center text-green">{{ ackSpeed }}/s</v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-col>
    </v-row>
    <v-row>
      <v-col cols="12">
        <v-card class="pa-2" elevation="2">
          <v-card-title>24小时数据</v-card-title>
          <div ref="twentyFourHoursChartRef" :style="{ height: '302px', width: '100%' }" />
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { useRouter } from 'vue-router'
  import { useECharts } from '@/hooks/useECharts';
  import { useAppStore } from '@/stores/app';

  const store = useAppStore();
  const stats = computed(() => store.stats);
  const realTimeChartInfo = computed(() => store.chart.realTimeChart);
  const ackSpeed = computed(() => store.ackSpeed);
  const router = useRouter();

  const chartRef = ref<HTMLDivElement | null>(null);
  const twentyFourHoursChartRef = ref<HTMLDivElement | null>(null);
  const { setOptions: setRealTimeChartOptions } = useECharts(chartRef as Ref<HTMLDivElement>);
  const { setOptions: setTwentyFourHoursChartOptions } = useECharts(twentyFourHoursChartRef as Ref<HTMLDivElement>);

  const updateRealTimeChartInfo = () => {
    setRealTimeChartOptions({
      legend: {
        data: ['发布成功', '发布失败', '消费成功', '消费失败', 'Ack数量'],
        textStyle: {
          color: '#ccc',
        },
      },
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          lineStyle: {
            width: 1,
            color: '#019680',
          },
        },
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: realTimeChartInfo.value.times,
        splitLine: {
          show: true,
          lineStyle: {
            width: 1,
            type: 'solid',
            color: 'rgba(226,226,226,0.5)',
          },
        },
        axisTick: {
          show: false,
        },
      },
      yAxis: [
        {
          type: 'value',
          splitLine: { show: false },
          axisTick: {
            show: false,
          },
          minInterval: 1, // 设置最小间隔为1，确保只显示整数
          splitArea: {
            show: true,
            areaStyle: {
              color: ['rgba(255,255,255,0.2)', 'rgba(226,226,226,0.2)'],
            },
          },
        },
      ],
      grid: {
        left: '2%',
        right: '3%',
        top: '15%',
        bottom: 0,
        containLabel: true,
      },
      series: [
        {
          name: '发布成功',
          type: 'line',
          smooth: true,
          data: realTimeChartInfo.value.chartData.publishSucceededData,
          itemStyle: {
            color: '#5ab1ef',
          },
        },
        {
          name: '发布失败',
          type: 'line',
          smooth: true,
          data: realTimeChartInfo.value.chartData.publishFailedData,
          itemStyle: {
            color: '#f12c2c',
          },
        },
        {
          name: '消费成功',
          type: 'line',
          smooth: true,
          data: realTimeChartInfo.value.chartData.consumeSucceededData,
          itemStyle: {
            color: '#7b68ee',
          },
        },
        {
          name: '消费失败',
          type: 'line',
          smooth: true,
          data: realTimeChartInfo.value.chartData.consumeFailedData,
          itemStyle: {
            color: '#f1761f',
          },
        },
        {
          name: 'Ack数量',
          type: 'line',
          smooth: true,
          data: realTimeChartInfo.value.chartData.ackCountData,
          itemStyle: {
            color: '#1fcfae',
          },
        },
      ],
    }, false);
  }

  const updateTwentyFourHoursChartData = () => {
    setTwentyFourHoursChartOptions({
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          lineStyle: {
            width: 1,
            color: '#019680',
          },
        },
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: stats.value.twentyFourHoursStats?.map(x => x.time),
        splitLine: {
          show: true,
          lineStyle: {
            width: 1,
            type: 'solid',
            color: 'rgba(226,226,226,0.5)',
          },
        },
        axisTick: {
          show: false,
        },
      },
      yAxis: [
        {
          type: 'value',
          splitLine: { show: false },
          axisTick: {
            show: false,
          },
          minInterval: 1, // 设置最小间隔为1，确保只显示整数
          splitArea: {
            show: true,
            areaStyle: {
              color: ['rgba(255,255,255,0.2)', 'rgba(226,226,226,0.2)'],
            },
          },
        },
      ],
      grid: {
        left: '1%',
        right: '2%',
        top: '15%',
        bottom: 0,
        containLabel: true,
      },
      series: [
        {
          name: '发布成功',
          type: 'line',
          smooth: true,
          data: stats.value.twentyFourHoursStats?.map(x => x.stats.publishSucceeded),
          itemStyle: {
            color: '#5ab1ef',
          },
        },
        {
          name: '发布失败',
          type: 'line',
          smooth: true,
          data: stats.value.twentyFourHoursStats?.map(x => x.stats.publishFailed),
          itemStyle: {
            color: '#f12c2c',
          },
        },
        {
          name: '消费成功',
          type: 'line',
          smooth: true,
          data: stats.value.twentyFourHoursStats?.map(x => x.stats.consumeSucceeded),
          itemStyle: {
            color: '#7b68ee',
          },
        },
        {
          name: '消费失败',
          type: 'line',
          smooth: true,
          data: stats.value.twentyFourHoursStats?.map(x => x.stats.consumeFailed),
          itemStyle: {
            color: '#f1761f',
          },
        },
        {
          name: 'Ack数量',
          type: 'line',
          smooth: true,
          data: stats.value.twentyFourHoursStats?.map(x => x.stats.ackCount),
          itemStyle: {
            color: '#1fcfae',
          },
        },
      ],
    }, false);
  }

  onMounted(() => {
    updateRealTimeChartInfo();
    updateTwentyFourHoursChartData();
  });

  watch(stats, () => {
    updateRealTimeChartInfo();
    updateTwentyFourHoursChartData();
  })
</script>

<style scoped>
.cursor-pointer {
  cursor: pointer;
  transition: transform 0.2s;
}

.cursor-pointer:hover {
  transform: translateY(-2px);
}
</style>
