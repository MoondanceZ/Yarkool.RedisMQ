namespace Yarkool.RedisMQ;

public enum MessageStatus
{
    Pending, // 消息待处理
    Processing, // 消息正在处理中
    Completed, // 消息已完成处理
    Failed // 消息处理失败
}