namespace Yarkool.RedisMQ;

public class DiagnosticListenerNames
{
    //Metrics
    public const string MetricListenerName = "EventCounter";
    public const string PublishedPerSec = "published-per-second";
    public const string ConsumePerSec = "consume-per-second";
    public const string InvokeSubscriberPerSec = "invoke-subscriber-per-second";
    public const string InvokeSubscriberElapsedMs = "invoke-subscriber-elapsed-ms";
}