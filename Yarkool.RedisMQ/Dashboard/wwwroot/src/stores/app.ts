// Utilities
import { defineStore } from 'pinia'
import type { ServerInfoResponse, StatsResponse, TwentyFourHoursStatsResponse } from '@/apis/response/StatResponse'
import { serverApi } from '@/apis'

const now = new Date();
const times: string[] = Array.from({ length: 12 }, (_, i) => {
  const time = new Date(now.getTime() - (11 - i) * 5000); // 从过去的时间开始，每5秒一个点
  const hours = time.getHours().toString().padStart(2, '0');
  const minutes = time.getMinutes().toString().padStart(2, '0');
  const seconds = (Math.floor(Number(time.getSeconds()) / 5) * 5)
    .toString()
    .padStart(2, '0');
  return `${hours}:${minutes}:${seconds}`;
});

export const useAppStore = defineStore('app', {
  state: () => ({
    stats: {
      realTimeStats: undefined as unknown as StatsResponse,
      twentyFourHoursStats: undefined as unknown as TwentyFourHoursStatsResponse[],
      serverInfo: undefined as unknown as ServerInfoResponse,
    },
    chart: {
      realTimeChart: {
        times,
        chartData: {
          publishSucceededData: Array(12).fill(0),
          publishFailedData: Array(12).fill(0),
          consumeSucceededData: Array(12).fill(0),
          consumeFailedData: Array(12).fill(0),
          ackCountData: Array(12).fill(0),
        },
      },
    },
    ackSpeed: 0 as number,
  }),
  actions: {
    async fetchStats () {
      const isFirstTimeFetch= !this.stats.realTimeStats
      const oldStats = this.stats.realTimeStats
      try {
        const response = await serverApi.getStats()
        if (response.code === 0) {
          const newStats = response.data.realTimeStats
          this.stats = response.data
          if(!isFirstTimeFetch){
            times.shift();
            const time = new Date();
            const hours = time.getHours().toString().padStart(2, '0');
            const minutes = time.getMinutes().toString().padStart(2, '0');
            const seconds = (Math.floor(Number(time.getSeconds()) / 5) * 5)
              .toString()
              .padStart(2, '0');
            this.chart.realTimeChart.times.push(`${hours}:${minutes}:${seconds}`);

            const publishSucceeded = newStats.publishSucceeded - oldStats.publishSucceeded;
            const publishFailed = newStats.publishFailed - oldStats.publishFailed;
            const consumeSucceeded = newStats.consumeSucceeded - oldStats.consumeSucceeded;
            const consumeFailed = newStats.consumeFailed - oldStats.consumeFailed;
            const ackCount = newStats.ackCount - oldStats.ackCount;

            this.chart.realTimeChart.chartData.publishSucceededData.shift();
            this.chart.realTimeChart.chartData.publishSucceededData.push(publishSucceeded < 0 ? 0 : publishSucceeded);
            this.chart.realTimeChart.chartData.publishFailedData.shift();
            this.chart.realTimeChart.chartData.publishFailedData.push(publishFailed < 0 ? 0 : publishFailed);
            this.chart.realTimeChart.chartData.consumeSucceededData.shift();
            this.chart.realTimeChart.chartData.consumeSucceededData.push(consumeSucceeded < 0 ? 0 : consumeSucceeded);
            this.chart.realTimeChart.chartData.consumeFailedData.shift();
            this.chart.realTimeChart.chartData.consumeFailedData.push(consumeFailed < 0 ? 0 : consumeFailed);
            this.chart.realTimeChart.chartData.ackCountData.shift();
            this.chart.realTimeChart.chartData.ackCountData.push(ackCount < 0 ? 0 : ackCount);
            this.ackSpeed = Number((ackCount / 5).toFixed(2));
          }
        } else {
          console.error('get stats error:', response.message)
        }
      } catch (error) {
        console.error('get stats error:', error)
      }
    },
  },
})
