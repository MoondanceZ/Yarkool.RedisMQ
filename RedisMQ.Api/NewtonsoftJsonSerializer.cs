using Newtonsoft.Json;
using Yarkool.RedisMQ;

namespace RedisMQ.Api;

public class NewtonsoftJsonSerializer : ISerializer
{
    public string Serialize<T>(T data)
    {
        return JsonConvert.SerializeObject(data, Formatting.None);
    }

    public string Serialize(object data)
    {
        return JsonConvert.SerializeObject(data, Formatting.None);
    }

    public T? Deserialize<T>(string? data)
    {
        return JsonConvert.DeserializeObject<T>(data ?? "");
    }

    public object? Deserialize(string? data, Type type)
    {
        return JsonConvert.DeserializeObject(data ?? "", type);
    }
}