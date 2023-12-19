namespace Yarkool.RedisMQ;

public class RedisMQException : Exception
{
    public RedisMQException(string? message) : base(message)
    {
    }

    public RedisMQException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}