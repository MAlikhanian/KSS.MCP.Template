using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace KSS.MCP.Template.Tools;

[McpServerToolType]
public static class WeatherTools
{
    [McpServerTool, Description("Get weather alerts for a US state.")]
    public static async Task<string> GetAlerts(
        HttpClient client,
        [Description("Two-letter US state code (e.g. CA, NY)")] string state)
    {
        using var jsonDocument = await client.ReadJsonDocumentAsync($"/alerts/active/area/{state}");
        var jsonElement = jsonDocument.RootElement;
        var alerts = jsonElement.GetProperty("features").EnumerateArray();

        if (!alerts.Any())
        {
            return "No active alerts for this state.";
        }

        return string.Join("\n---\n", alerts.Select(alert =>
        {
            JsonElement properties = alert.GetProperty("properties");
            return $"""
                Event: {properties.GetProperty("event").GetString()}
                Area: {properties.GetProperty("areaDesc").GetString()}
                Severity: {properties.GetProperty("severity").GetString()}
                Description: {properties.GetProperty("description").GetString()}
                Instructions: {properties.GetProperty("instruction").GetString()}
                """;
        }));
    }

    [McpServerTool, Description("Get weather forecast for a location.")]
    public static async Task<string> GetForecast(
        HttpClient client,
        [Description("Latitude of the location")] double latitude,
        [Description("Longitude of the location")] double longitude)
    {
        var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");

        using var jsonDocument = await client.ReadJsonDocumentAsync(pointUrl);
        var forecastUrl = jsonDocument.RootElement
            .GetProperty("properties")
            .GetProperty("forecast")
            .GetString()
            ?? throw new Exception($"No forecast URL provided by api.weather.gov for {latitude},{longitude}");

        using var forecastDocument = await client.ReadJsonDocumentAsync(forecastUrl);
        var periods = forecastDocument.RootElement
            .GetProperty("properties")
            .GetProperty("periods")
            .EnumerateArray()
            .Take(5); // Only show next 5 periods

        return string.Join("\n---\n", periods.Select(period => $"""
            {period.GetProperty("name").GetString()}:
            Temperature: {period.GetProperty("temperature").GetInt32()}°{period.GetProperty("temperatureUnit").GetString()}
            Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
            Forecast: {period.GetProperty("detailedForecast").GetString()}
            """));
    }
}