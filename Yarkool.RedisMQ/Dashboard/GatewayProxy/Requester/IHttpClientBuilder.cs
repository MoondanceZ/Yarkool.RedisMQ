namespace Yarkool.RedisMQ;

public interface IHttpClientBuilder
{
    /// <summary>
    /// Creates the <see cref="HttpClient" />
    /// </summary>
    IHttpClient Create();
}