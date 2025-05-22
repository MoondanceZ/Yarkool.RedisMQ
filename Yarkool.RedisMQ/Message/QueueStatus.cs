namespace Yarkool.RedisMQ;

public enum QueueStatus
{
    Processing, //运行中
    Stopping,  //停止中
    Stopped  //停止
}