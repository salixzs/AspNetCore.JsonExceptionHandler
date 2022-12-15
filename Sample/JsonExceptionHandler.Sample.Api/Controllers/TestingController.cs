using Microsoft.AspNetCore.Mvc;

namespace JsonExceptionHandler.Sample.Api.Controllers
{
    [ApiController]
    public class TestingController : ControllerBase
    {
        [HttpGet("/test/throw")]
        public IEnumerable<WeatherForecast> Throw() =>
            throw new ApplicationException("Should be returned in Json", new ArgumentNullException("Inner exception is added.", new NotImplementedException("Cause is not implemeneted yet.")));
    }
}
