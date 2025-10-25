using System.ClientModel;
using AISmartHome.Agents;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Text.Json;
using Aevatar.HomeAssistantClient;
using AISmartHome.Tools;
using AISmartHome.Tools.Extensions;
using Microsoft.Kiota.Http.HttpClientLibrary;

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

// Register Home Assistant client
builder.Services.AddSingleton<HomeAssistantClient>(sp =>
{
    Console.WriteLine($"\n[HomeAssistantClient Factory] Initializing client...");
    Console.WriteLine($"[HomeAssistantClient Factory] BaseUrl from config: '{haBaseUrl}'");
    Console.WriteLine($"[HomeAssistantClient Factory] Token length: {haAccessToken.Length} chars");
    
    var httpClientHandler = new HttpClientHandler();
    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(haBaseUrl) };
    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", haAccessToken);
    
    Console.WriteLine($"[HomeAssistantClient Factory] HttpClient.BaseAddress: '{httpClient.BaseAddress}'");
    
    var authProvider = new Microsoft.Kiota.Abstractions.Authentication.AnonymousAuthenticationProvider();
    var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
    requestAdapter.BaseUrl = haBaseUrl;
    
    Console.WriteLine($"[HomeAssistantClient Factory] RequestAdapter.BaseUrl: '{requestAdapter.BaseUrl}'");
    Console.WriteLine($"[HomeAssistantClient Factory] ✅ Client initialized\n");
    
    return new HomeAssistantClient(requestAdapter);
});

// Register registries
builder.Services.AddSingleton<StatesRegistry>(sp =>
{
    var client = sp.GetRequiredService<HomeAssistantClient>();
    return new StatesRegistry(client, haBaseUrl, haAccessToken);
});
builder.Services.AddSingleton<ServiceRegistry>();

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

// Register new Phase 1 agents and modules
builder.Services.AddSingleton<ReasoningAgent>();
builder.Services.AddSingleton<AISmartHome.Agents.Modules.PlanningModule>();
builder.Services.AddSingleton(sp => new AISmartHome.Agents.Modules.ParallelCoordinator());

// Register Phase 2 - Memory & Learning
builder.Services.AddSingleton<AISmartHome.Agents.Storage.IVectorStore>(sp => 
    new AISmartHome.Agents.Storage.InMemoryVectorStore());
builder.Services.AddSingleton<AISmartHome.Agents.Storage.IEmbeddingService>(sp =>
    new AISmartHome.Agents.Storage.OpenAIEmbeddingService(
        llmApiKey,
        llmEndpoint,
        model: "text-embedding-3-small"
    ));
builder.Services.AddSingleton<AISmartHome.Agents.Storage.MemoryStore>(sp =>
{
    var vectorStore = sp.GetRequiredService<AISmartHome.Agents.Storage.IVectorStore>();
    var embeddingService = sp.GetRequiredService<AISmartHome.Agents.Storage.IEmbeddingService>();
    var persistencePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "memories.json");
    Directory.CreateDirectory(Path.GetDirectoryName(persistencePath)!);
    return new AISmartHome.Agents.Storage.MemoryStore(vectorStore, embeddingService, persistencePath);
});
builder.Services.AddSingleton<MemoryAgent>();
builder.Services.AddSingleton<ReflectionAgent>();
builder.Services.AddSingleton<AISmartHome.Agents.Modules.PreferenceLearning>();

var app = builder.Build();

// Initialize registries
var statesRegistry = app.Services.GetRequiredService<StatesRegistry>();
var serviceRegistry = app.Services.GetRequiredService<ServiceRegistry>();

Console.WriteLine("🔄 Initializing Home Assistant connection...");
Console.WriteLine($"[Program] Home Assistant URL: {haBaseUrl}");
Console.WriteLine($"[Program] Token configured: {!string.IsNullOrEmpty(haAccessToken)} (length: {haAccessToken?.Length ?? 0})");

try
{
    Console.WriteLine("\n[Program] 📡 Step 1: Refreshing States Registry...");
    await statesRegistry.RefreshAsync();
    Console.WriteLine("[Program] ✅ States Registry refreshed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[Program] ❌ FAILED to refresh States Registry");
    Console.WriteLine($"[Program] Exception: {ex.GetType().FullName}");
    Console.WriteLine($"[Program] Message: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"[Program] Inner Exception: {ex.InnerException.GetType().FullName}");
        Console.WriteLine($"[Program] Inner Message: {ex.InnerException.Message}");
    }
    
    Console.WriteLine($"\n[Program] Full Stack Trace:");
    Console.WriteLine(ex.StackTrace);
    
    throw; // Re-throw to stop the application
}

try
{
    Console.WriteLine("\n[Program] 📡 Step 2: Refreshing Service Registry...");
    await serviceRegistry.RefreshAsync();
    Console.WriteLine("[Program] ✅ Service Registry refreshed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[Program] ❌ FAILED to refresh Service Registry");
    Console.WriteLine($"[Program] Exception: {ex.GetType().FullName}");
    Console.WriteLine($"[Program] Message: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"[Program] Inner Exception: {ex.InnerException.GetType().FullName}");
        Console.WriteLine($"[Program] Inner Message: {ex.InnerException.Message}");
    }
    
    throw; // Re-throw to stop the application
}

Console.WriteLine("\n✅ Initialization complete!");

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
app.MapGet("/agent/stats", async (StatesRegistry statesRegistry) =>
{
    var entities = await statesRegistry.GetAllEntitiesAsync();
    var stats = entities.GroupBy(e => e.GetDomain())
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
app.MapGet("/agent/devices", async (StatesRegistry statesRegistry, string? domain = null) =>
{
    var entities = await statesRegistry.GetAllEntitiesAsync();
    
    if (!string.IsNullOrEmpty(domain))
    {
        entities = entities.Where(e => e.GetDomain().Equals(domain, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var devices = entities.Select(e => new
    {
        entity_id = e.EntityId,
        friendly_name = e.GetFriendlyName(),
        state = e.State,
        domain = e.GetDomain()
    }).ToList();

    return Results.Ok(devices);
});

Console.WriteLine("🚀 Smart Home AI API is running!");
Console.WriteLine($"📡 Home Assistant: {haBaseUrl}");
Console.WriteLine($"🤖 LLM Model: {llmModel}");
Console.WriteLine($"🌐 Open http://localhost:{builder.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "5000"} to access the UI");

app.Run();

record ChatRequest(string Message);
