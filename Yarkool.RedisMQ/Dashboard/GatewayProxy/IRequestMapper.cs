using Microsoft.AspNetCore.Http;

namespace Yarkool.RedisMQ;

public interface IRequestMapper
{
    Task<HttpRequestMessage> Map(HttpRequest request);
}