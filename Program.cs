using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000);
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/geo+json"));
});

builder.Services.AddHttpClient("coingecko", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("kss-mcp-coinprice/1.0");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddTransient<HttpClient>(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("weather"));

builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.MapMcp();

Console.WriteLine("===============================================");
Console.WriteLine("Kiwi Smart Solution MCP Service");
Console.WriteLine("===============================================");
Console.WriteLine("MCP Server running on http://localhost:5000");
Console.WriteLine("SSE endpoint: http://localhost:5000/sse");
Console.WriteLine("-----------------------------------------------");
Console.WriteLine("Registered HttpClients:");
Console.WriteLine(" - weather");
Console.WriteLine(" - coingecko");
//Console.WriteLine(" - github");
//Console.WriteLine(" - openweather");
//Console.WriteLine(" - internal-api");
Console.WriteLine("===============================================");
Console.WriteLine("Press Ctrl+C to stop the server");

await app.RunAsync();