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

    [McpServerTool, Description("Get the current spot price of a cryptocurrency from CoinGecko.")]
    public static async Task<string> GetCoinPrice(
    IHttpClientFactory httpFactory,
    [Description("Coin id or ticker (e.g., 'bitcoin' or 'BTC')")] string coin = "BTC",
    [Description("Fiat currency (e.g., 'usd', 'eur')")] string vsCurrency = "usd")
    {
        if (string.IsNullOrWhiteSpace(coin))
            coin = "BTC";
        if (string.IsNullOrWhiteSpace(vsCurrency))
            vsCurrency = "usd";

        var cgId = _tickerToId.TryGetValue(coin, out var mapped) ? mapped : coin.Trim().ToLowerInvariant();
        var vs = vsCurrency.Trim().ToLowerInvariant();

        var client = httpFactory.CreateClient("coingecko");
        var uri = $"/api/v3/simple/price?ids={Uri.EscapeDataString(cgId)}&vs_currencies={Uri.EscapeDataString(vs)}";

        using var doc = await client.ReadJsonDocumentAsync(uri);
        var root = doc.RootElement;

        if (!root.TryGetProperty(cgId, out var coinObj) ||
            !coinObj.TryGetProperty(vs, out var priceEl))
        {
            return $"No price available for '{coin}' in '{vsCurrency}'. Try a CoinGecko id like 'bitcoin' or a ticker like 'BTC'.";
        }

        var price = priceEl.GetDouble();
        var displayCoin = _tickerToId.ContainsKey(coin) ? coin.ToUpperInvariant() : cgId;

        return $"{displayCoin} price: {price} {vs.ToUpperInvariant()}";
    }

    private static readonly Dictionary<string, string> _tickerToId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = "bitcoin",
        ["ETH"] = "ethereum",
        ["BNB"] = "binancecoin",
        ["ADA"] = "cardano",
        ["XRP"] = "ripple",
        ["SOL"] = "solana",
        ["DOGE"] = "dogecoin",
        ["TRX"] = "tron"
    };
}