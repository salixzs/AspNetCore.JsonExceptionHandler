using Microsoft.AspNetCore.Mvc;

namespace JsonExceptionHandler.Sample.Api.Controllers
{
    [ApiController]
    public class TestingController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public TestingController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/test/throw")]
        public IEnumerable<WeatherForecast> Throw()
        {
            throw new NotImplementedException("Should be returned as JSON.");
        }
    }
}
