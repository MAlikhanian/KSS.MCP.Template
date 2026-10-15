using System.Net.Http.Headers;
using System.Reflection;

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

builder.Services.AddHttpClient("alborzdco", client =>
{
    client.BaseAddress = new Uri("https://alborzdco.ir");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("kss-mcp-sales/1.0");
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

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    service = "kss-mcp-template"
}));

// Debug endpoint to check what tools are registered
app.MapGet("/debug/tools", () => 
{
    try 
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var toolTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false).Any())
            .ToList();
        
        var tools = new List<object>();
        foreach (var toolType in toolTypes)
        {
            var methods = toolType.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false).Any())
                .Select(m => new { 
                    Name = m.Name,
                    Type = toolType.Name,
                    Description = m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? "No description",
                    Parameters = m.GetParameters().Select(p => new { 
                        Name = p.Name, 
                        Type = p.ParameterType.Name,
                        Description = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? "No description"
                    }).ToList()
                })
                .ToList();
            
            tools.AddRange(methods);
        }
        
        return Results.Ok(new { 
            timestamp = DateTime.UtcNow,
            toolCount = tools.Count,
            tools = tools,
            assemblyLocation = assembly.Location,
            assemblyName = assembly.FullName,
            allTypes = assembly.GetTypes().Length
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { 
            error = ex.Message,
            stackTrace = ex.StackTrace,
            timestamp = DateTime.UtcNow
        });
    }
});

app.MapMcp();

Console.WriteLine("===============================================");
Console.WriteLine("Kiwi Smart Solution MCP Service");
Console.WriteLine("===============================================");
Console.WriteLine("MCP Server running on http://localhost:5000");
Console.WriteLine("SSE endpoint: http://localhost:5000/sse");
Console.WriteLine("Health endpoint: http://localhost:5000/health");
Console.WriteLine("Debug tools endpoint: http://localhost:5000/debug/tools");
Console.WriteLine("-----------------------------------------------");

// Debug tool registration at startup
try
{
    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    var toolTypes = assembly.GetTypes()
        .Where(t => t.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false).Any())
        .ToList();
    
    Console.WriteLine($"Assembly loaded: {assembly.FullName}");
    Console.WriteLine($"Total types in assembly: {assembly.GetTypes().Length}");
    Console.WriteLine($"Tool types found: {toolTypes.Count}");
    
    var toolCount = 0;
    if (toolTypes.Any())
    {
        Console.WriteLine("Registered MCP Tool Types:");
        foreach (var toolType in toolTypes)
        {
            var methods = toolType.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false).Any())
                .ToList();
            toolCount += methods.Count;
            Console.WriteLine($" - {toolType.Name}: {methods.Count} tools");
            foreach (var method in methods)
            {
                Console.WriteLine($"   * {method.Name}");
            }
        }
        Console.WriteLine($"Total MCP Tools: {toolCount}");
    }
    else
    {
        Console.WriteLine("WARNING: No MCP tool types found!");
        Console.WriteLine("Looking for types in Tools namespace:");
        var toolsNamespaceTypes = assembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Tools") == true)
            .ToList();
        foreach (var type in toolsNamespaceTypes)
        {
            Console.WriteLine($" - Found type: {type.FullName}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to enumerate tools: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
Console.WriteLine("Registered HttpClients:");
Console.WriteLine(" - weather");
Console.WriteLine(" - coingecko");
Console.WriteLine(" - alborzdco");
Console.WriteLine("===============================================");
Console.WriteLine("Press Ctrl+C to stop the server");

await app.RunAsync();