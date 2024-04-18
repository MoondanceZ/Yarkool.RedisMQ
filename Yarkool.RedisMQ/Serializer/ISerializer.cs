namespace Yarkool.RedisMQ
{
    public interface ISerializer
    {
        string Serialize<T>(T data);

        string Serialize(object data);

        T? Deserialize<T>(string? data);

        object? Deserialize(string? data, Type type);
    }
}