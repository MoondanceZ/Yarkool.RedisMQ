using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Yarkool.RedisMQ;

public class GatewayProxyAgent
{
    private async Task SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response)
    {
        foreach (var httpResponseHeader in response.Content.Headers)
        {
            AddHeaderIfDoesntExist(context, httpResponseHeader);
        }

        var content = await response.Content.ReadAsByteArrayAsync();

        AddHeaderIfDoesntExist(context,
            new KeyValuePair<string, IEnumerable<string>>("Content-Length", new[] { content.Length.ToString() }));

        context.Response.OnStarting(state =>
        {
            var httpContext = (HttpContext)state;

            httpContext.Response.StatusCode = (int)response.StatusCode;

            return Task.CompletedTask;
        }, context);

        await using Stream stream = new MemoryStream(content);
        if (response.StatusCode != HttpStatusCode.NotModified)
            await stream.CopyToAsync(context.Response.Body);
    }

    private static void AddHeaderIfDoesntExist(HttpContext context, KeyValuePair<string, IEnumerable<string>> httpResponseHeader)
    {
        if (!context.Response.Headers.ContainsKey(httpResponseHeader.Key))
            context.Response.Headers.Append(httpResponseHeader.Key, new StringValues(httpResponseHeader.Value.ToArray()));
    }
}