using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WeatherAppBackend.Services
{
    public class WeatherService
    {
        private readonly WeatherHttpClient _httpClient;

        public WeatherService(WeatherHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherResponse> GetWeatherAsync(decimal lat, decimal lon)
        {
            _httpClient.url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=weather_code,temperature_2m_max,temperature_2m_min,cloud_cover_mean,precipitation_probability_max,&current=temperature_2m,cloud_cover,relative_humidity_2m,weather_code,is_day,wind_speed_10m,wind_direction_10m&timezone=auto&wind_speed_unit=mph&temperature_unit=fahrenheit&precipitation_unit=inch";
            var response = await _httpClient.GetWeatherAsync();
            Rootobject? deserializedResult = null;
            WeatherResponse wr = new WeatherResponse();
            try
            {
                deserializedResult = ParseJson(response.ToString());
            }
            catch (Exception ex)
            {
                wr.ErrorMessage = $"Error processing weather data: {ex.Message}";
                return wr;
            }
            if (deserializedResult == null)
            {
                wr.ErrorMessage = "Error processing weather data: deserialized result is null.";
                return wr;
            }
            wr = FillWeatherResponse(deserializedResult, response.ToString());
            return wr;
        }

        public string GetImage(string condition)
        {
            var path = Path.Combine("Images", $"{condition}.png");
            if (!System.IO.File.Exists(path)) return "";

            var bytes = System.IO.File.ReadAllBytes(path);
            var base64 = Convert.ToBase64String(bytes);
            string image = $"data:image/png;base64,{base64}";
            return image;
        }

        private WeatherResponse FillWeatherResponse(Rootobject deserializedResult, string result)
        {
            WeatherResponse wr = new WeatherResponse();
            int dayNumber = 0;

            //current conditions
            WeatherCodeObject weathercode = WeatherCodeLookup(deserializedResult.Current.WeatherCode, deserializedResult.Current.CloudCover, deserializedResult.Current.IsDay);
            wr.Conditions = new WeatherConditions();
            wr.Conditions.Condition = weathercode.Condition; 
            wr.Conditions.Temperature = deserializedResult.Current.Temperature2m;
            wr.Conditions.WindDirectionDegrees = deserializedResult.Current.WindDirection10m;
            wr.Conditions.WindDirection = GetWindDirectionString(deserializedResult.Current.WindDirection10m); ;
            wr.Conditions.WindSpeed = deserializedResult.Current.WindSpeed10m;
            wr.Conditions.Image = GetImage(weathercode.ImageToLoad); 

            //daily forecast
            foreach (string day in deserializedResult.Daily.Time)
            {
                DayWeather dayWeather = new DayWeather();
                WeatherCodeObject dayweathercode = WeatherCodeLookup(deserializedResult.Daily.WeatherCode[dayNumber], deserializedResult.Daily.CloudCoverMean[dayNumber]);
                dayWeather.Date = Convert.ToDateTime(day);
                dayWeather.Condition = dayweathercode.Condition;
                dayWeather.High = deserializedResult.Daily.Temperature2mMax[dayNumber];
                dayWeather.Low = deserializedResult.Daily.Temperature2mMin[dayNumber];
                dayWeather.DayOfWeek = dayWeather.Date.DayOfWeek.ToString();
                dayWeather.PrecipChance = deserializedResult.Daily.PrecipitationProbabilityMax[dayNumber];
                dayWeather.Image = GetImage(dayweathercode.ImageToLoad);
                wr.Day.Add(dayWeather);
                dayNumber++;
            }
            return wr;

        }

        private string GetWindDirectionString(int WindDirection10m)
        {
            string windDirectionName = WindDirection10m switch  //.Net 8.0+ syntax
            {
                > 348 or < 11 => "N",
                > 10 and < 34 => "NNE",
                > 33 and < 56 => "NE",
                > 55 and < 79 => "ENE",
                > 78 and < 101 => "E",
                > 100 and < 124 => "ESE",
                > 123 and < 146 => "SE",
                > 145 and < 169 => "SSE",
                > 168 and < 191 => "S",
                > 190 and < 214 => "SSW",
                > 213 and < 236 => "SW",
                > 235 and < 259 => "WSW",
                > 258 and < 281 => "W",
                > 280 and < 304 => "WNW",
                > 303 and < 326 => "NW",
                > 325 and < 349 => "NNW"
            };
            return windDirectionName;
        }

        private WeatherCodeObject WeatherCodeLookup(int WeatherCode, int CloudCover, bool IsDay = true)
        {
            switch(WeatherCode)
            {
                case 45: return new WeatherCodeObject("Fog", "Fog"); 
                case 48: return new WeatherCodeObject("Freezing Fog", "Fog");
                case 51: return new WeatherCodeObject("Light Drizzle", "Rain");
                case 53: return new WeatherCodeObject("Drizzle", "Rain");
                case 55: return new WeatherCodeObject("Heavy Drizzle", "Rain");
                case 56: return new WeatherCodeObject("Light Freezing Drizzle", "Ice");
                case 57: return new WeatherCodeObject("Freezing Drizzle", "Ice");
                case 61: return new WeatherCodeObject("Light Rain", "Rain");
                case 63: return new WeatherCodeObject("Rain", "Rain");
                case 65: return new WeatherCodeObject("Heavy Rain", "Rain");
                case 66: return new WeatherCodeObject("Light Freezing Rain", "Ice");
                case 67: return new WeatherCodeObject("Heavy Freezing Rain", "Ice");
                case 71: return new WeatherCodeObject("Light Snow", "Snow");
                case 73: return new WeatherCodeObject("Snow", "Snow");
                case 75: return new WeatherCodeObject("Heavy Snow", "Snow");
                case 77: return new WeatherCodeObject("Icy Snow", "Ice");
                case 80: return new WeatherCodeObject("Light Showers", "Rain");
                case 81: return new WeatherCodeObject("Rain Showers", "Rain");
                case 82: return new WeatherCodeObject("Heavy Showers", "Rain");
                case 85: return new WeatherCodeObject("Light Snow Showers", "Snow");
                case 86: return new WeatherCodeObject("Snow Showers", "Snow");
                case 95: return new WeatherCodeObject("Thunderstorm", "Thunderstorm");
                //Handle cloudiness condition by cloud cover % to determine type; weather code may not be adequate
                default: return CloudCoverLookup(CloudCover, IsDay); 
            }
        }

        public class ConditionImage
        {
            public string ConditionName { get; set; } = "";
            public string ImageToLoad { get; set; } = "";
        };
        
        private WeatherCodeObject CloudCoverLookup(int CloudCover, bool IsDay = true)
        {
            string conditionName = CloudCover switch  //.Net 8.0+ syntax
            {
                > -1 and < 11 => "Clear",
                > 10 and < 21 => "Mostly Clear",
                > 20 and < 60 => "Partly Cloudy",
                > 59 and < 90 => "Mostly Cloudy",
                > 89 => "Cloudy",
                < 0 => "Unknown"
            };

            string imageToLoad = conditionName;
            if (!IsDay && conditionName != "Cloudy" && conditionName != "Unknown")
            {
                imageToLoad += " Night";
            }
            return new WeatherCodeObject(conditionName, imageToLoad);
        }

        private Rootobject? ParseJson(string json)
        {
            json = json.Replace("\\", "");
            json = json.Replace("{\"result\":", "");
            Rootobject? insideJson = new Rootobject();
            try
            {
                insideJson = JsonConvert.DeserializeObject<Rootobject>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deserializing JSON: {ex.Message}");
            }
            return insideJson;
        }
    }

    public class WeatherResponse
    {
        public List<DayWeather> Day { get; set; } = new List<DayWeather>();
        public WeatherConditions Conditions { get; set; } = new WeatherConditions();
        public string ErrorMessage { get; set; } = "";
    }

    public class DayWeather
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = "";
        public string Condition { get; set; } = "";
        public int PrecipChance { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public string Image { get; set; } = "";
    }

    public class WeatherConditions
    {
        public string Condition { get; set; } = "";
        public float Temperature { get; set; }
        public int WindDirectionDegrees { get; set; }
        public string WindDirection { get; set; } = "";
        public float WindSpeed { get; set; }
        public string Image { get; set; } = "";
    }

    public class WeatherCodeObject
    {
        public string Condition { get; set; } = "";
        public string ImageToLoad { get; set; } = "";
        public WeatherCodeObject(string condition, string imageToLoad)
        {
            this.Condition = condition;
            this.ImageToLoad = imageToLoad;
        }
    }

    public class DeserializedJson
    {
        public required Rootobject Result { get; set; }
    }

    public class Rootobject
    {
        [JsonProperty("latitude")]
        public float Latitude { get; set; }
        [JsonProperty("longitude")]
        public float Longitude { get; set; }
        [JsonProperty("generationtime_ms")]
        public float GenerationTimeMs { get; set; }
        [JsonProperty("utc_offset_seconds")]
        public int UTCOffsetSeconds { get; set; }
        [JsonProperty("timezone")]
        public string TimeZone { get; set; } = "";
        [JsonProperty("timezone_abbreviation")]
        public string TimeZoneAbreviation { get; set; } = "";
        [JsonProperty("elevation")]
        public float Elevation { get; set; }
        [JsonProperty("current_units")]
        public CurrentUnits CurrentUnits { get; set; } = new CurrentUnits();
        [JsonProperty("Current")]
        public Current Current { get; set; } = new Current();
        [JsonProperty("daily_units")]
        public DailyUnits DailyUnits { get; set; } = new DailyUnits();
        [JsonProperty("daily")]
        public Daily Daily { get; set; } = new Daily();
    }

    public class CurrentUnits
    {
        [JsonProperty("time")]
        public string Time { get; set; } = "";
        [JsonProperty("interval")]
        public string Interval { get; set; } = "";
        [JsonProperty("temperature_2m")]
        public string Temperature2m { get; set; } = "";
        [JsonProperty("cloud_cover")]
        public string CloudCover { get; set; } = "";
        [JsonProperty("relative_humidity_2m")]
        public string RelativeHumidity2m { get; set; } = "";
        [JsonProperty("wind_speed_10m")]
        public string WindSpeed10m { get; set; } = "";
        [JsonProperty("wind_direction_10m")]
        public string WindDirection10m { get; set; } = "";
        [JsonProperty("weather_code")]
        public string WeatherCode { get; set; } = "";
    }

    public class Current
    {
        [JsonProperty("time")]
        public string Time { get; set; } = "";
        [JsonProperty("interval")]
        public int Interval { get; set; }
        [JsonProperty("temperature_2m")]
        public float Temperature2m { get; set; }
        [JsonProperty("cloud_cover")]
        public int CloudCover { get; set; }
        [JsonProperty("relative_humidity_2m")]
        public int RelativeHumidity2m { get; set; }
        [JsonProperty("wind_speed_10m")]
        public float WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")]
        public int WindDirection10m { get; set; }
        [JsonProperty("weather_code")]
        public int WeatherCode { get; set; }
        [JsonProperty("is_day")]
        public bool IsDay { get; set; }
    }

    public class DailyUnits
    {
        [JsonProperty("time")]
        public string Time { get; set; } = "";
        [JsonProperty("weather_code")]
        public string WeatherCode { get; set; } = "";
        [JsonProperty("temperature_2m_max")]
        public string Temperature2mMax { get; set; } = "";
        [JsonProperty("temperature_2m_min")]
        public string Temperature2mMin { get; set; } = "";
        [JsonProperty("cloud_cover_mean")]
        public string CloudCoverMean { get; set; } = "";
        [JsonProperty("precipitation_probability_max")]
        public string PrecipitationProbabilityMax { get; set; } = "";
    }

    public class Daily
    {
        [JsonProperty("time")]
        public string[] Time { get; set; } = ["", "", "", "", "", "", ""];
        [JsonProperty("weather_code")]
        public int[] WeatherCode { get; set; } = [-1, -1, -1, -1, -1, -1, -1];
        [JsonProperty("temperature_2m_max")]
        public float[] Temperature2mMax { get; set; } = [0, 0, 0, 0, 0, 0, 0];
        [JsonProperty("temperature_2m_min")]
        public float[] Temperature2mMin { get; set; } = [0, 0, 0, 0, 0, 0, 0];
        [JsonProperty("cloud_cover_mean")]
        public int[] CloudCoverMean { get; set; } = [-1, -1, -1, -1, -1, -1, -1];
        [JsonProperty("precipitation_probability_max")]
        public int[] PrecipitationProbabilityMax { get; set; } = [0, 0, 0, 0, 0, 0, 0];
    }
}