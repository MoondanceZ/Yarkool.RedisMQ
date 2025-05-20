namespace Yarkool.RedisMQ;

public enum MessageStatus
{
    Pending, //待处理
    Processing, //处理中
    Retrying,  //重试中
    Completed, //已完成
    Failed //处理失败
}