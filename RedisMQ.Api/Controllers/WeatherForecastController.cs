using Microsoft.AspNetCore.Mvc;
using Yarkool.RedisMQ;

namespace RedisMQ.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        {
            "Freezing",
            "Bracing",
            "Chilly",
            "Cool",
            "Mild",
            "Warm",
            "Balmy",
            "Hot",
            "Sweltering",
            "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IRedisMQPublisher _publisher;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRedisMQPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();
        }

        [HttpGet("GenMessage")]
        public async Task<string> GenMessage()
        {
            var input = Guid.NewGuid().ToString("N");
            var messageId = await _publisher.PublishAsync("Test", new TestMessage
            {
                Input = input
            });
            return $"{messageId}-{input}";
        }
    }
}