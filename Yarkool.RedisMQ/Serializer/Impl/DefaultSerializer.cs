using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yarkool.RedisMQ
{
    internal class DefaultSerializer : ISerializer
    {
        public T? Deserialize<T>(string? data)
        {
            if (string.IsNullOrEmpty(data))
                return default;
            return JsonSerializer.Deserialize<T>(data, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            });
        }

        public object? Deserialize(string? data, Type type)
        {
            if (string.IsNullOrEmpty(data))
                return null;
            return JsonSerializer.Deserialize(data, type, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            });
        }

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            });
        }
    }
}