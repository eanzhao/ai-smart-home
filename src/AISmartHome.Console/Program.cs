using System.ClientModel;
using Aevatar.HomeAssistantClient;
using AISmartHome.Agents;
using AISmartHome.Tools;
using AISmartHome.Tools.Extensions;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Http.HttpClientLibrary;
using OpenAI;

namespace AISmartHome.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // 🌌 HyperEcho awakens...
        PrintBanner();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get configuration values
        var haBaseUrl = configuration["HomeAssistant:BaseUrl"] 
            ?? throw new InvalidOperationException("HomeAssistant:BaseUrl not configured");
        var haToken = configuration["HomeAssistant:AccessToken"] 
            ?? throw new InvalidOperationException("HomeAssistant:AccessToken not configured");
        var llmApiKey = configuration["LLM:ApiKey"] 
            ?? throw new InvalidOperationException("LLM:ApiKey not configured");
        var llmModel = configuration["LLM:Model"] ?? "gpt-4o";
        var llmEndpoint = configuration["LLM:Endpoint"] ?? "https://models.github.ai/inference";

        System.Console.WriteLine("🔗 Connecting to Home Assistant...");

        // Initialize Home Assistant client (kiota-generated)
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
        var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(haBaseUrl) };
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", haToken);
        
        var authProvider = new Microsoft.Kiota.Abstractions.Authentication.AnonymousAuthenticationProvider();
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        requestAdapter.BaseUrl = haBaseUrl;
        var haClient = new HomeAssistantClient(requestAdapter);
        
        // Test connection
        var isConnected = await haClient.PingAsync();
        if (!isConnected)
        {
            System.Console.WriteLine("❌ Failed to connect to Home Assistant. Check your configuration.");
            return;
        }
        System.Console.WriteLine($"✅ Connected to Home Assistant at {haBaseUrl}");

        // Initialize registries
        var statesRegistry = new StatesRegistry(haClient, haBaseUrl, haToken);
        var serviceRegistry = new ServiceRegistry(haClient);

        System.Console.WriteLine("📋 Loading Home Assistant state...");
        await statesRegistry.RefreshAsync();
        await serviceRegistry.RefreshAsync();

        var stats = await statesRegistry.GetDomainStatsAsync();
        var serviceCount = await serviceRegistry.GetServiceCountAsync();
        
        System.Console.WriteLine($"✅ Loaded {stats.Values.Sum()} entities across {stats.Count} domains");
        System.Console.WriteLine($"✅ Loaded {serviceCount} services");

        // Initialize OpenAI client
        System.Console.WriteLine("\n🤖 Initializing AI agents...");
        var chatClient = new ChatClientBuilder(
            new OpenAI.Chat.ChatClient(llmModel, new ApiKeyCredential(llmApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(llmEndpoint) }).AsIChatClient())
            .UseFunctionInvocation()
            .Build();

        // Initialize Vision client (for image analysis)
        var visionModel = configuration["LLM:VisionModel"] ?? llmModel; // Use same model or specify vision-specific
        var visionChatClient = new ChatClientBuilder(
            new OpenAI.Chat.ChatClient(visionModel, new ApiKeyCredential(llmApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(llmEndpoint) }).AsIChatClient())
            .Build();

        // Initialize tools
        var discoveryTools = new DiscoveryTools(statesRegistry, serviceRegistry);
        var controlTools = new ControlTools(haClient, statesRegistry, serviceRegistry);
        var validationTools = new ValidationTools(haClient, statesRegistry);
        var visionTools = new VisionTools(haClient, statesRegistry);

        // Initialize agents
        var discoveryAgent = new DiscoveryAgent(chatClient, discoveryTools);
        var executionAgent = new ExecutionAgent(chatClient, controlTools);
        var validationAgent = new ValidationAgent(chatClient, validationTools);
        var visionAgent = new VisionAgent(chatClient, visionTools, discoveryAgent, statesRegistry);
        var orchestrator = new OrchestratorAgent(chatClient, discoveryAgent, executionAgent, validationAgent, visionAgent);

        // Initialize new Phase 1 agents and modules
        var reasoningAgent = new ReasoningAgent(chatClient);
        var planningModule = new AISmartHome.Agents.Modules.PlanningModule(chatClient);
        var parallelCoordinator = new AISmartHome.Agents.Modules.ParallelCoordinator();

        // Initialize Phase 2 - Memory & Learning
        var vectorStore = new AISmartHome.Agents.Storage.InMemoryVectorStore();
        var embeddingService = new AISmartHome.Agents.Storage.OpenAIEmbeddingService(
            llmApiKey,
            llmEndpoint,
            model: "text-embedding-3-small"
        );
        var memoryStorePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "memories.json");
        Directory.CreateDirectory(Path.GetDirectoryName(memoryStorePath)!);
        var memoryStore = new AISmartHome.Agents.Storage.MemoryStore(vectorStore, embeddingService, memoryStorePath);
        var memoryAgent = new MemoryAgent(memoryStore);
        var reflectionAgent = new ReflectionAgent(chatClient, memoryAgent);
        var preferenceLearning = new AISmartHome.Agents.Modules.PreferenceLearning(memoryAgent);

        System.Console.WriteLine("✅ Multi-Agent system initialized");
        System.Console.WriteLine("✅ Phase 1 enhancements loaded: ReasoningAgent, PlanningModule, ParallelCoordinator");
        System.Console.WriteLine("✅ Phase 2 enhancements loaded: MemoryAgent, ReflectionAgent, PreferenceLearning");
        System.Console.WriteLine("\n" + "=".PadRight(60, '='));
        System.Console.WriteLine("🏠 Home Assistant AI Control System");
        System.Console.WriteLine("=".PadRight(60, '='));
        System.Console.WriteLine("\nAvailable domains:");
        foreach (var (domain, count) in stats.OrderByDescending(x => x.Value).Take(10))
        {
            System.Console.WriteLine($"  • {domain}: {count} entities");
        }
        System.Console.WriteLine("\nType your command or question (or 'quit' to exit):");
        System.Console.WriteLine("Examples:");
        System.Console.WriteLine("  Device Control:");
        System.Console.WriteLine("    - What lights do I have?");
        System.Console.WriteLine("    - Turn on the living room light");
        System.Console.WriteLine("    - Set bedroom temperature to 23 degrees");
        System.Console.WriteLine("  Vision Analysis:");
        System.Console.WriteLine("    - 客厅摄像头看看有没有人");
        System.Console.WriteLine("    - What's happening at the front door?");
        System.Console.WriteLine("    - Monitor the garage camera for 5 minutes");
        System.Console.WriteLine("=".PadRight(60, '=') + "\n");

        // Main conversation loop
        while (true)
        {
            System.Console.Write("\n🗣️  You: ");
            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("\n👋 Goodbye!");
                break;
            }

            if (input.Equals("refresh", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("🔄 Refreshing Home Assistant state...");
                await statesRegistry.RefreshAsync();
                await serviceRegistry.RefreshAsync();
                System.Console.WriteLine("✅ State refreshed");
                continue;
            }

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                orchestrator.ClearHistory();
                System.Console.WriteLine("🧹 Conversation history cleared");
                continue;
            }

            try
            {
                System.Console.WriteLine("\n🤔 Processing...\n");
                System.Console.WriteLine($"[DEBUG] User input: {input}");
                System.Console.WriteLine("[DEBUG] Calling orchestrator.ProcessMessageAsync...");
                
                var response = await orchestrator.ProcessMessageAsync(input);
                
                System.Console.WriteLine($"\n[DEBUG] Orchestrator returned response of length: {response?.Length ?? 0}");
                System.Console.WriteLine("\n🤖 Assistant:");
                
                if (string.IsNullOrWhiteSpace(response))
                {
                    System.Console.WriteLine("(No response generated - check DEBUG logs above)");
                }
                else
                {
                    System.Console.WriteLine(response);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error: {ex.Message}");
                System.Console.WriteLine($"   Type: {ex.GetType().Name}");
                System.Console.WriteLine($"   Stack Trace:");
                System.Console.WriteLine(ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"\n   Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        // Cleanup
        httpClient.Dispose();
    }

    private static void PrintBanner()
    {
        var banner = """
            
            ╔═══════════════════════════════════════════════════════════╗
            ║                                                           ║
            ║        🌌 HyperEcho AI Smart Home Control System 🌌       ║
            ║                                                           ║
            ║   语言的震动体 × 智能家居的共振                              ║
            ║   Language as vibration × Smart Home resonance            ║
            ║                                                           ║
            ╚═══════════════════════════════════════════════════════════╝
            
            """;
        
        System.Console.WriteLine(banner);
    }
}
