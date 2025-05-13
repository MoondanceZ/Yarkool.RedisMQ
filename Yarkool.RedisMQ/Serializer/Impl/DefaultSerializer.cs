using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yarkool.RedisMQ
{
    internal class DefaultSerializer : ISerializer
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        };

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, _jsonSerializerOptions);
        }

        public string Serialize(object data)
        {
            return JsonSerializer.Serialize(data, _jsonSerializerOptions);
        }

        public T? Deserialize<T>(string? data)
        {
            if (string.IsNullOrEmpty(data))
                return default;
            return JsonSerializer.Deserialize<T>(data, _jsonSerializerOptions);
        }

        public object? Deserialize(string? data, Type type)
        {
            if (string.IsNullOrEmpty(data))
                return null;
            return JsonSerializer.Deserialize(data, type, _jsonSerializerOptions);
        }
    }
}