import React, { useState } from "react";
import "./App.css";

function App() {
  const [lat, setLat] = useState("");
  const [lon, setLon] = useState("");
  const [weather, setWeather] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPopup, setShowPopup] = useState(false);

  const fetchWeather = async (e) => {
    e.preventDefault();
	
	const latNum = parseFloat(lat);
	const lonNum = parseFloat(lon);
	
	// Validate coordinates
	let errorMessages = [];

	if (latNum < -90 || latNum > 90) {
		errorMessages.push("Latitude must be a number between -90 and 90.");
	}

	if (lonNum < -180 || lonNum > 180) {
		errorMessages.push("Longitude must be a number between -180 and 180.");
	}

	if (errorMessages.length > 0) {
		setError(errorMessages.join("\n"));
		setShowPopup(true);
		return; 
	}
	
    setLoading(true);
    setError("");
    setWeather(null);

    try {
      const response = await fetch(
		`https://localhost:7087/api/weather/getweatherbycoords?lat=${lat}&lon=${lon}`
      );

      if (!response.ok) {
        throw new Error("Failed to fetch weather data");
      }

      const data = await response.json();
	  if (data.errorMessage !== "") {
               throw new Error(data.errorMessage);
      }
      setWeather(data);
      

    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app weather-bg">
      <h1>Weather Forecast</h1>
	
	  {/* Popup Error Modal */}
	  {showPopup && (
		<div className="error-popup-overlay">
			<div className="error-popup-box">
				<h3>Coordinate Error</h3>
				<p>{error}</p>
				<button onClick={() => setShowPopup(false)}>Close</button>
			</div>
		</div>
	  )}

      {/* Coordinates Form */}
      <form className="coords-form" onSubmit={fetchWeather}>
        <input
          type="number"
          step="any"
          placeholder="Latitude"
          value={lat}
          onChange={(e) => {setLat(e.target.value); setError("");}}
          required
        />
        <input
          type="number"
          step="any"
          placeholder="Longitude"
          value={lon}
          onChange={(e) => {setLon(e.target.value); setError("");}}
          required
        />
        <button type="submit">Get Weather</button>
      </form>

      {loading && <p>Loading weather...</p>}
      {error && <p className="error">{error}</p>}

      {/* Current Conditions */}
      {weather && (
        <>
          <div className="current-weather">
            <h2>Current Conditions</h2>
            <img
              src={weather.conditions.image}
              alt={weather.conditions.condition}
            />
            <p className="condition">{weather.conditions.condition}</p>
            <p>Temperature: {weather.conditions.temperature}°F</p>
            <p>Wind: {weather.conditions.windSpeed} mph</p>
	    <p>Direction: {weather.conditions.windDirectionDegrees}° {weather.conditions.windDirection}</p>
          </div>

          {/* Forecast */}
          <div className="forecast">
            <h2>7-Day Forecast</h2>
            <div className="forecast-grid">
              {weather.day.map((d) => (
                <div key={d.date} className="forecast-card">
                  <h3>{d.dayOfWeek}</h3>
                  <img
                    src={d.image} 
                    alt={d.condition}
                  />
                  <p className="condition">{d.condition}</p>
                  <p>High: {d.high}°F</p>
                  <p>Low: {d.low}°F</p>
                  <p>Precip: {d.precipChance}%</p>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
	<br/>  
	<p>Weather forecast by Open-Meteo</p>
    </div>
  );
}

export default App;