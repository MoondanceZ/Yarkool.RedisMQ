import type { MessageStatus } from '@/types/MessageStatus'

export default interface MessageResponse {
  status: MessageStatus
  executionTimes: number
  message: MessageDataResponse
  errorInfo: MessageErrorInfoResponse
}

export interface MessageDataResponse {
  createTimestamp: number
  messageContent: string
  machineName: string
  delayTime: number
  messageId: string
  queueName: string
}

export interface MessageErrorInfoResponse {
  queueName: string
  groupName?: string
  consumerName: string
  exceptionMessage: string
  stackTrace?: string
  errorMessageContent?: string
  errorMessageTimestamp: number
}
