using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configure to listen on port 5000
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000);
});

// Add MCP Server with HTTP/SSE transport
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Configure HttpClient for National Weather Service API
builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient() { BaseAddress = new Uri("https://api.weather.gov") };
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
    return client;
});

// Add CORS for development
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

// Map MCP endpoints (automatically creates /sse endpoint)
app.MapMcp();

// Console logging works now since we're using HTTP/SSE!
Console.WriteLine("===============================================");
Console.WriteLine("Weather MCP Server running on http://localhost:5000");
Console.WriteLine("SSE endpoint: http://localhost:5000/sse");
Console.WriteLine("===============================================");
Console.WriteLine("Press Ctrl+C to stop the server");

await app.RunAsync();