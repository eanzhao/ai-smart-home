using System.ClientModel;
using AISmartHome.Console.Agents;
using AISmartHome.Console.Services;
using AISmartHome.Console.Tools;
using Microsoft.Extensions.AI;
using OpenAI;
using Azure.AI.OpenAI;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Home Assistant
var haBaseUrl = builder.Configuration["HomeAssistant:BaseUrl"] 
    ?? throw new InvalidOperationException("HomeAssistant:BaseUrl not configured");
var haAccessToken = builder.Configuration["HomeAssistant:AccessToken"] 
    ?? throw new InvalidOperationException("HomeAssistant:AccessToken not configured");

// Configure OpenAI
var llmApiKey = builder.Configuration["LLM:ApiKey"] 
    ?? throw new InvalidOperationException("LLM:ApiKey not configured");
var llmModel = builder.Configuration["LLM:Model"] ?? "gpt-4o-mini";
var llmEndpoint = builder.Configuration["LLM:Endpoint"] ?? "https://api.openai.com/v1";

// Register services
builder.Services.AddSingleton(sp => new HomeAssistantClient(haBaseUrl, haAccessToken, ignoreSslErrors: true));
builder.Services.AddSingleton(sp => new EntityRegistry(sp.GetRequiredService<HomeAssistantClient>()));
builder.Services.AddSingleton(sp => new ServiceRegistry(sp.GetRequiredService<HomeAssistantClient>()));

// Register tools
builder.Services.AddSingleton<DiscoveryTools>();
builder.Services.AddSingleton<ControlTools>();
builder.Services.AddSingleton<ValidationTools>();

// Register chat client
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var chatClient = new ChatClientBuilder(
        new OpenAI.Chat.ChatClient(llmModel, new ApiKeyCredential(llmApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(llmEndpoint) }).AsIChatClient())
        .UseFunctionInvocation()
        .Build();
    return chatClient;
});

// Register agents
builder.Services.AddSingleton<DiscoveryAgent>();
builder.Services.AddSingleton<ExecutionAgent>();
builder.Services.AddSingleton<ValidationAgent>();
builder.Services.AddSingleton<OrchestratorAgent>();

var app = builder.Build();

// Initialize registries
var entityRegistry = app.Services.GetRequiredService<EntityRegistry>();
var serviceRegistry = app.Services.GetRequiredService<ServiceRegistry>();

Console.WriteLine("🔄 Initializing Home Assistant connection...");
await entityRegistry.RefreshAsync();
await serviceRegistry.RefreshAsync();
Console.WriteLine("✅ Initialization complete!");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Chat endpoint with SSE streaming
app.MapPost("/agent/chat", async (HttpContext context, OrchestratorAgent orchestrator) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<ChatRequest>();
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new { error = "Message is required" });
        }

        // Set SSE headers
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        var response = await orchestrator.ProcessMessageAsync(request.Message, context.RequestAborted);

        // Send response as SSE
        var sseData = $"data: {JsonSerializer.Serialize(new { message = response })}\n\n";
        await context.Response.WriteAsync(sseData, context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);

        return Results.Empty;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Chat endpoint failed: {ex.Message}");
        return Results.Problem(ex.Message);
    }
});

// Get device stats
app.MapGet("/agent/stats", async (EntityRegistry entityRegistry) =>
{
    var entities = await entityRegistry.GetAllEntitiesAsync();
    var stats = entities.GroupBy(e => e.Domain)
        .Select(g => new { domain = g.Key, count = g.Count() })
        .OrderByDescending(x => x.count)
        .Take(10)
        .ToList();
    
    return Results.Ok(new
    {
        totalDevices = entities.Count,
        domains = stats
    });
});

// List devices
app.MapGet("/agent/devices", async (EntityRegistry entityRegistry, string? domain = null) =>
{
    var entities = await entityRegistry.GetAllEntitiesAsync();
    
    if (!string.IsNullOrEmpty(domain))
    {
        entities = entities.Where(e => e.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var devices = entities.Select(e => new
    {
        entity_id = e.EntityId,
        friendly_name = e.GetFriendlyName(),
        state = e.State,
        domain = e.Domain
    }).ToList();

    return Results.Ok(devices);
});

Console.WriteLine("🚀 Smart Home AI API is running!");
Console.WriteLine($"📡 Home Assistant: {haBaseUrl}");
Console.WriteLine($"🤖 LLM Model: {llmModel}");
Console.WriteLine($"🌐 Open http://localhost:{builder.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "5000"} to access the UI");

app.Run();

record ChatRequest(string Message);
