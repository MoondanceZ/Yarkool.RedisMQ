using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yarkool.RedisMQ
{
    internal class DefaultSerializer : ISerializer
    {
        public T? Deserialize<T>(string data)
        {
            return JsonSerializer.Deserialize<T>(data, new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            });
        }

        //public object? Deserialize(string data, Type type)
        //{
        //    return JsonSerializer.Deserialize(data, type, new JsonSerializerOptions()
        //    {
        //        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        //    });
        //}

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            });
        }
    }
}
