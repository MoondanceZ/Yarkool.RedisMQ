namespace Yarkool.RedisMQ;

public interface IHttpRequester
{
    Task<HttpResponseMessage> GetResponse(HttpRequestMessage request);
}