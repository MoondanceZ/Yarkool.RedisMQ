namespace Yarkool.RedisMQ;

internal class TwentyFourHoursStatsResponse
{
    /// <summary>
    /// Time
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Stats
    /// </summary>
    public StatsResponse Stats { get; set; } = default!;
}