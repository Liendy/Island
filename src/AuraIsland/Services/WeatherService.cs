using System.Net.Http;
using System.Text.Json;

namespace AuraIsland.Services;

public sealed record WeatherInfo(string City, double Temperature, string Description);

public static class WeatherService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public static async Task<WeatherInfo?> GetAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var q = Uri.EscapeDataString(city);
        using var geo = JsonDocument.Parse(await Http.GetStringAsync($"https://geocoding-api.open-meteo.com/v1/search?name={q}&count=1&language=ru&format=json"));
        if (!geo.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return null;
        var place = results[0];
        var lat = place.GetProperty("latitude").GetDouble();
        var lon = place.GetProperty("longitude").GetDouble();
        var name = place.GetProperty("name").GetString() ?? city;
        using var weather = JsonDocument.Parse(await Http.GetStringAsync($"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto"));
        var current = weather.RootElement.GetProperty("current");
        var temp = current.GetProperty("temperature_2m").GetDouble();
        var code = current.GetProperty("weather_code").GetInt32();
        return new WeatherInfo(name, temp, Describe(code));
    }

    private static string Describe(int code) => code switch
    {
        0 => "Ясно", 1 or 2 => "Преимущественно ясно", 3 => "Облачно",
        45 or 48 => "Туман", 51 or 53 or 55 => "Морось",
        61 or 63 or 65 => "Дождь", 71 or 73 or 75 => "Снег",
        80 or 81 or 82 => "Ливень", 95 or 96 or 99 => "Гроза",
        _ => "Переменная погода"
    };
}
