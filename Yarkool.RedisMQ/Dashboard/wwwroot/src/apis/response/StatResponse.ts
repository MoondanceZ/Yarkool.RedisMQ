export interface StatsResponse {
  publishSucceeded: number
  publishFailed: number
  consumeSucceeded: number
  consumeFailed: number
  ackCount: number
  pendingCount: number
  failedCount: number
  processingCount: number
  retryingCount: number
  completedCount: number
  allCount: number
}

export interface TwentyFourHoursStatsResponse {
  time: string
  stats: StatsResponse
}

export interface ServerInfoResponse {
  queueCount: number
  consumerCount: number
  serverCount: number
  messageCount: number
  redisMQVersion: string
  redisVersion: string
  serverTimestamp: number
}

export default interface ServerResponse {
  realTimeStats: StatsResponse
  twentyFourHoursStats: TwentyFourHoursStatsResponse[]
  serverInfo: ServerInfoResponse
}
