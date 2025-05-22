import http from './http'
import type MessagePageRequest from './request/MessagePageRequest'
import type BaseResponse from './response/BaseResponse'
import type ConsumerResponse from './response/ConsumerResponse'
import type MessageResponse from './response/MessageResponse'
import type QueueResponse from './response/QueueResponse'
import type ServerNodeResponse from './response/ServerNodeResponse'
import type StatsResponse from './response/StatResponse'

// 消息相关接口
export const messageApi = {
  getList (params: MessagePageRequest): Promise<BaseResponse<MessageResponse[]>> {
    return http.get('/message/list', { params })
  },
  delete (ids: string[]) : Promise<BaseResponse<null>> {
    return http.post(`/message/delete`, ids)
  },
}

// 队列相关接口
export const queueApi = {
  getList (): Promise<BaseResponse<QueueResponse[]>>{
    return http.get('/queue/list')
  },
  delete (ids: string[]) : Promise<BaseResponse<null>> {
    return http.post(`/queue/delete`, ids)
  },
}

// 消费者相关接口
export const consumerApi = {
  getList () : Promise<BaseResponse<ConsumerResponse[]>> {
    return http.get('/consumer/list')
  },
  delete (ids: string[]) : Promise<BaseResponse<null>> {
    return http.post(`/consumer/delete`, ids)
  },
}

// 服务器相关接口
export const serverApi = {
  getStats (): Promise<BaseResponse<StatsResponse>> {
    return http.get('/stats')
  },
  getList (): Promise<BaseResponse<ServerNodeResponse[]>> {
    return http.get('/server/list')
  },
}
