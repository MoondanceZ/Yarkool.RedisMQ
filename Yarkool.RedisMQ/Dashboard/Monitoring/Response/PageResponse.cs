namespace Yarkool.RedisMQ;

public class PageResponse<T>
{
    /// <summary>
    /// Items
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// TotalCount
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// PageIndex
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// PageSize
    /// </summary>
    public int PageSize { get; set; }
}