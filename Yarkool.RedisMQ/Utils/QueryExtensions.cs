using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Yarkool.RedisMQ;

public static class QueryExtensions
{
    public static T ToObject<T>(this IQueryCollection query) where T : new()
    {
        var obj = new T();
        foreach (var prop in typeof(T).GetProperties())
        {
            // 获取查询参数名称（优先取属性上的FromQuery注解）
            var paramName = prop.GetCustomAttribute<FromQueryAttribute>()?.Name ?? prop.Name;

            if (query.TryGetValue(paramName, out var value))
            {
                var convertedValue = ConvertValue(value.ToString(), prop.PropertyType);
                prop.SetValue(obj, convertedValue);
            }
        }

        return obj;
    }

    private static object? ConvertValue(string? value, Type targetType)
    {
        if (targetType == typeof(string))
            return value;
        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            return string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, underlyingType);
        }

        return Convert.ChangeType(value, targetType);
    }
}