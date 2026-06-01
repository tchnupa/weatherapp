using Microsoft.AspNetCore.Mvc;
using WeatherAppBackend.Services;

namespace WeatherAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("GetWeatherByCoords")]
        public async Task<IActionResult> GetWeatherByCoords(decimal lat, decimal lon)
        {
            var weatherData = await _weatherService.GetWeatherAsync(lat, lon);
            if (weatherData == null)
            {
                return NotFound("Coords not found.");
            }
            return Ok(weatherData);
        }
    }
}