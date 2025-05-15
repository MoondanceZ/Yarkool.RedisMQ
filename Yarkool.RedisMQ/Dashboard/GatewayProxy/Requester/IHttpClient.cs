namespace Yarkool.RedisMQ;

public interface IHttpClient
{
    HttpClient Client { get; }

    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
}