import type { QueueStatus } from '@/types/QueueStatus'

export default interface ServerNodeResponse {
  queueName: string
  isDelayQueue: boolean
  status: QueueStatus,
  consumerList: string[]
}
