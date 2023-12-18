using Microsoft.AspNetCore.Mvc;

namespace Yarkool.Api.Controllers
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
        private readonly TestPublisher _testPublisher;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, TestPublisher testPublisher)
        {
            _logger = logger;
            _testPublisher = testPublisher;
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
            await _testPublisher.PublishAsync(new TestMessage
            {
                Input = input
            });
            return input;
        }
    }
}