using Aspire.Hosting.GitHub;

var builder = DistributedApplication.CreateBuilder(args);

// Add configuration parameters
var homeAssistantUrl = builder.AddParameter("homeassistant-url", secret: false);
var homeAssistantToken = builder.AddParameter("homeassistant-token", secret: true);
var llmApiKey = builder.AddParameter("llm-apikey", secret: true);
var llmModel = builder.AddParameter("llm-model", secret: false);
var llmEndpoint = builder.AddParameter("llm-endpoint", secret: false);

// Add the AI Smart Home API
var api = builder.AddProject<Projects.AISmartHome_API>("ai-smart-home-api")
    .WithHttpEndpoint(port: 5000, name: "api-http")
    .WithHttpsEndpoint(port: 5001, name: "api-https")
    .WithEnvironment("HomeAssistant__BaseUrl", homeAssistantUrl)
    .WithEnvironment("HomeAssistant__AccessToken", homeAssistantToken)
    .WithEnvironment("LLM__ApiKey", llmApiKey)
    .WithEnvironment("LLM__Model", llmModel)
    .WithEnvironment("LLM__Endpoint", llmEndpoint)
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode)
{
    api.WithUrl("/index.html", "Home Assistant Chat UI");
}

builder.Build().Run();