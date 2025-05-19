namespace Yarkool.RedisMQ;

internal class StatsResponse
{
    /// <summary>
    /// RealTimeStats
    /// </summary>
    public Types.StatsInfo RealTimeStats { get; set; } = null!;

    /// <summary>
    /// TwentyFourHoursStats
    /// </summary>
    public IEnumerable<Types.TwentyFourHoursStatsInfo> TwentyFourHoursStats { get; set; } = [];

    /// <summary>
    /// ServerInfo
    /// </summary>
    public Types.ServerInfo ServerInfo { get; set; } = null!;
    
    public static class Types
    {
        public class StatsInfo
        {
            /// <summary>
            /// PublishSucceeded
            /// </summary>
            public long PublishSucceeded { get; set; }

            /// <summary>
            /// PublishFailed
            /// </summary>
            public long PublishFailed { get; set; }

            /// <summary>
            /// ConsumeSucceeded
            /// </summary>
            public long ConsumeSucceeded { get; set; }

            /// <summary>
            /// ConsumeFailed
            /// </summary>
            public long ConsumeFailed { get; set; }

            /// <summary>
            /// AckCount
            /// </summary>
            public long AckCount { get; set; }

            /// <summary>
            /// ErrorQueueLength
            /// </summary>
            public long ErrorQueueLength { get; set; }
        }
        
        public class TwentyFourHoursStatsInfo
        {
            /// <summary>
            /// Time
            /// </summary>
            public string Time { get; set; } = null!;

            /// <summary>
            /// Stats
            /// </summary>
            public StatsInfo Stats { get; set; } = null!;
        }

        public class ServerInfo
        {
            /// <summary>
            /// QueueCount
            /// </summary>
            public long QueueCount { get; set; }

            /// <summary>
            /// ConsumerCount
            /// </summary>
            public long ConsumerCount { get; set; }

            /// <summary>
            /// ServerCount
            /// </summary>
            public long ServerCount { get; set; }

            /// <summary>
            /// MessageCount
            /// </summary>
            public long MessageCount { get; set; }
        }
    }
}