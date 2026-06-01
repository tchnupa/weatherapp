namespace WeatherAppBackend
{
    public class WeatherHttpClient
    {
        private readonly HttpClient _httpClient;
        public string url = String.Empty;
        public WeatherHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<string> GetWeatherAsync()
        {
            if(url == String.Empty)
            {
                throw new Exception("Error: set .url in WeatherHttpClient before calling GetWeatherAsync().");
            }
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                return response;
            }
            catch(Exception ex)
            {
                 return $"Error: {ex.Message}";
            }
        }

    }
}
