export default interface BaseResponse<TData> {
  code: number,
  message: string,
  data: TData,
}
