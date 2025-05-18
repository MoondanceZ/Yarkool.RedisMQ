namespace Yarkool.RedisMQ;

internal class BaseResponse
{
    /// <summary>
    /// Code, success = 0
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// T
    /// </summary>
    public object? Data { get; set; }

    public static BaseResponse Success(string? message = null, object? data = null)
    {
        return new BaseResponse
        {
            Code = 0,
            Message = message,
            Data = data
        };
    }

    public static BaseResponse Error(string? message = null, object? data = null, int code = -1)
    {
        return new BaseResponse
        {
            Code = code,
            Message = message,
            Data = data
        };
    }
}