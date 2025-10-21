# 🐛 调试指南

## 日志系统

系统已添加完整的调试日志，所有日志以 `[DEBUG]`、`[TOOL]` 或 `[API]` 前缀标识。

### 日志级别

| 前缀 | 含义 | 示例 |
|------|------|------|
| `[DEBUG]` | Agent执行流程 | `[DEBUG] OrchestratorAgent.ProcessMessageAsync called` |
| `[TOOL]` | 工具被调用 | `[TOOL] SearchDevices called: query='客厅灯'` |
| `[API]` | Home Assistant API调用 | `[API] Calling service: light.turn_on` |

### 完整执行流程追踪

当你输入一个命令时，应该看到如下日志序列：

```
🗣️  You: 打开客厅灯

[DEBUG] User input: 打开客厅灯
[DEBUG] Calling orchestrator.ProcessMessageAsync...
[DEBUG] OrchestratorAgent.ProcessMessageAsync called
[DEBUG] User message: 打开客厅灯
[DEBUG] Analyzing intent...
[DEBUG] Calling LLM for intent analysis...
[DEBUG] Stream update #1: {
[DEBUG] Stream update #2:   "needs_discovery": false,
[DEBUG] Stream update #3:   "needs_execution": true,
[DEBUG] Total stream updates: 15
[DEBUG] LLM response: {"needs_discovery":false,"needs_execution":true,...
[DEBUG] Intent analysis result:
  - NeedsDiscovery: false
  - NeedsExecution: true
  - NeedsEntityResolution: true
[DEBUG] Entity resolution needed...
[DEBUG] Entity query: 打开客厅灯
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: Find device matching: 打开客厅灯
[DEBUG] Registered 6 discovery tools
[DEBUG] Calling LLM with discovery tools...
[TOOL] FindDevice called: description='打开客厅灯', domain='light'
[DEBUG] DiscoveryAgent stream #1: Found device: light.living_room
[DEBUG] DiscoveryAgent received 10 stream updates
[DEBUG] Total response length: 150 chars
[DEBUG] Entity resolution result: Found device: light.living_room...
[DEBUG] Routing to ExecutionAgent...
[DEBUG] ExecutionAgent.ExecuteCommandAsync called with: 打开客厅灯
[DEBUG] Registered 7 control tools
[DEBUG] Calling LLM with control tools...
[TOOL] ControlLight called: entity=light.living_room, action=turn_on
[API] Calling Home Assistant service: light.turn_on
[API] Service data: {"entity_id":"light.living_room"}
[API] POST /api/services/light/turn_on
[API] Response status: 200 OK
[DEBUG] ExecutionAgent received 8 stream updates
[DEBUG] Final response length: 250 chars

🤖 Assistant:
✅ 客厅灯已打开
```

## 常见问题诊断

### 问题 1: Assistant 没有响应（空白输出）

**症状**:
```
🤖 Assistant:
(No response generated - check DEBUG logs above)
```

**检查日志中的关键点**:

#### A. LLM调用失败

查找:
```
[DEBUG] Calling LLM...
```

如果之后没有 `[DEBUG] Stream update` 日志，说明LLM没有返回任何内容。

**可能原因**:
1. API Key无效或过期
2. 模型名称错误
3. 网络连接问题
4. 速率限制

**解决方法**:
```bash
# 检查API Key
echo $OPENAI_API_KEY

# 测试API连接
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_KEY"

# 或者对于GitHub Models
curl https://models.github.ai/inference/chat/completions \
  -H "Authorization: Bearer YOUR_GITHUB_PAT"
```

#### B. 流式响应为空

查找:
```
[DEBUG] Total stream updates: 0
```

说明LLM连接成功但没有返回内容。

**可能原因**:
- System prompt太长
- 工具定义格式错误
- 模型不支持function calling

**解决方法**:
```csharp
// 临时简化测试
var messages = new List<ChatMessage>
{
    new(ChatRole.User, "Hello, can you hear me?")
};
// 不传Tools，看是否有响应
```

#### C. 工具从未被调用

查找是否有 `[TOOL]` 日志。

如果没有，说明：
- LLM没有选择调用工具
- 工具注册失败

**检查**:
```
[DEBUG] Registered 6 discovery tools  # 应该看到这行
[DEBUG] Registered 7 control tools    # 应该看到这行
```

### 问题 2: 工具调用了但没有结果

**症状**:
```
[TOOL] SearchDevices called: query='客厅灯', domain='light'
[DEBUG] Total response length: 0 chars
```

**可能原因**:
- Home Assistant API调用失败
- 数据序列化错误
- 异常被吞掉

**添加更详细日志**:
在 `DiscoveryTools.cs` 中：
```csharp
public async Task<string> SearchDevices(...)
{
    try
    {
        System.Console.WriteLine($"[TOOL] SearchDevices START");
        var entities = await _entityRegistry.SearchEntitiesAsync(query);
        System.Console.WriteLine($"[TOOL] Found {entities.Count} entities");
        // ...
        System.Console.WriteLine($"[TOOL] SearchDevices END - returning {result.Length} chars");
        return result;
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"[TOOL ERROR] SearchDevices failed: {ex.Message}");
        throw;
    }
}
```

### 问题 3: Home Assistant API调用失败

**症状**:
```
[API] Calling Home Assistant service: light.turn_on
[API] POST /api/services/light/turn_on
[API] Response status: 401 Unauthorized
```

**可能原因**:
- Access Token 无效
- Token 过期
- Token 权限不足

**解决方法**:
1. 重新生成 Long-Lived Access Token
2. 在 Home Assistant UI 中检查 Token 状态
3. 测试 Token:
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" \
     http://YOUR_HA_IP:8123/api/
```

### 问题 4: 意图分析一直失败

**症状**:
```
[DEBUG] Intent analysis result:
  - NeedsDiscovery: true
  - NeedsExecution: false
  - NeedsEntityResolution: false
```

但用户明明要求执行操作。

**原因**: LLM没有正确理解意图分析的prompt。

**临时禁用意图分析**:
```csharp
// OrchestratorAgent.cs - ProcessMessageAsync
// 跳过意图分析，直接路由
var intentAnalysis = new IntentAnalysis
{
    NeedsDiscovery = true,  // 强制都调用
    NeedsExecution = true,
    NeedsEntityResolution = true
};
```

## 逐步调试策略

### Step 1: 最小化测试

在 `Program.cs` 主循环中，添加测试命令：

```csharp
if (input.Equals("test", StringComparison.OrdinalIgnoreCase))
{
    System.Console.WriteLine("\n🧪 Running basic LLM test...");
    
    var testMessages = new List<ChatMessage>
    {
        new(ChatRole.User, "Say 'Hello from HyperEcho!'")
    };
    
    var testResponse = new StringBuilder();
    await foreach (var update in chatClient.GetStreamingResponseAsync(testMessages))
    {
        System.Console.WriteLine($"[TEST] Stream: {update}");
        testResponse.Append(update);
    }
    
    System.Console.WriteLine($"\n[TEST] Final: {testResponse}");
    continue;
}
```

输入 `test` 看LLM是否能响应。

### Step 2: 测试工具调用

```csharp
if (input.Equals("test-tool", StringComparison.OrdinalIgnoreCase))
{
    System.Console.WriteLine("\n🧪 Testing tool invocation...");
    
    var result = await discoveryTools.SearchDevices("light", null);
    System.Console.WriteLine($"[TEST] Tool result: {result}");
    continue;
}
```

### Step 3: 测试单个Agent

```csharp
if (input.Equals("test-discovery", StringComparison.OrdinalIgnoreCase))
{
    System.Console.WriteLine("\n🧪 Testing DiscoveryAgent...");
    
    var result = await discoveryAgent.ProcessQueryAsync("What lights do I have?");
    System.Console.WriteLine($"[TEST] Discovery result: {result}");
    continue;
}
```

### Step 4: 测试完整流程

逐步启用各个组件，观察哪一步出问题。

## 检查清单

运行系统前，确认以下配置：

- [ ] `appsettings.json` 中 `HomeAssistant:BaseUrl` 正确
- [ ] `HomeAssistant:AccessToken` 有效（在HA UI中测试）
- [ ] `OpenAI:ApiKey` 有效（或GitHub PAT）
- [ ] `OpenAI:Endpoint` 正确（GitHub Models是 `https://models.github.ai/inference`）
- [ ] 网络能访问 Home Assistant
- [ ] 网络能访问 OpenAI/GitHub API
- [ ] 防火墙没有阻止连接

## 启用详细HTTP日志

在 `HomeAssistantClient` 构造函数中添加：

```csharp
public HomeAssistantClient(string baseUrl, string accessToken)
{
    _baseUrl = baseUrl.TrimEnd('/');
    
    var handler = new HttpClientHandler();
    var loggingHandler = new LoggingHandler(handler); // 自定义handler
    
    _httpClient = new HttpClient(loggingHandler)
    {
        BaseAddress = new Uri(_baseUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };
    // ...
}

// LoggingHandler
public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[HTTP] {request.Method} {request.RequestUri}");
        var response = await base.SendAsync(request, cancellationToken);
        Console.WriteLine($"[HTTP] Response: {(int)response.StatusCode} {response.StatusCode}");
        return response;
    }
}
```

## 性能问题诊断

如果系统响应很慢：

1. **检查LLM调用时间**
   ```
   [DEBUG] Calling LLM...
   ... (时间差) ...
   [DEBUG] Total stream updates: X
   ```
   
2. **检查HA API调用时间**
   ```
   [API] POST /api/services/...
   [API] Response status: 200 OK
   ```

3. **检查缓存是否工作**
   ```bash
   # 第一次调用应该较慢
   You: 我有哪些灯？
   
   # 5分钟内第二次调用应该很快（从缓存）
   You: 我有哪些灯？
   ```

## 日志示例分析

### 正常执行的日志

```
[DEBUG] User input: 打开客厅灯
[DEBUG] Calling orchestrator.ProcessMessageAsync...
[DEBUG] OrchestratorAgent.ProcessMessageAsync called
[DEBUG] Analyzing intent...
[DEBUG] Calling LLM for intent analysis...
[DEBUG] Stream update #1: {
[DEBUG] Stream update #2: "needs
[DEBUG] Total stream updates: 12
[DEBUG] Intent analysis result:
  - NeedsDiscovery: false
  - NeedsExecution: true
  - NeedsEntityResolution: true
[DEBUG] Entity resolution needed...
[DEBUG] DiscoveryAgent.ProcessQueryAsync called
[DEBUG] Calling LLM with discovery tools...
[TOOL] FindDevice called
[DEBUG] DiscoveryAgent received 5 stream updates
[DEBUG] ExecutionAgent.ExecuteCommandAsync called
[TOOL] ControlLight called: entity=light.living_room, action=turn_on
[API] Calling Home Assistant service: light.turn_on
[API] POST /api/services/light/turn_on
[API] Response status: 200 OK
[DEBUG] Final response length: 120 chars

🤖 Assistant:
✅ 客厅灯已打开
```

### 异常情况：LLM无响应

```
[DEBUG] Calling LLM...
[DEBUG] Total stream updates: 0  ← 问题：没有stream updates
[DEBUG] Total response length: 0 chars
[DEBUG] Final response length: 0 chars

🤖 Assistant:
(No response generated - check DEBUG logs above)
```

**诊断**: LLM API调用成功但没返回内容
**解决**: 检查API key、模型名称、endpoint配置

### 异常情况：工具调用失败

```
[TOOL] SearchDevices called: query='xxx'
[TOOL ERROR] SearchDevices failed: Connection refused
```

**诊断**: Home Assistant无法访问
**解决**: 检查BaseUrl配置、HA是否运行、网络连接

### 异常情况：JSON解析失败

```
[DEBUG] LLM response: This is a text response, not JSON...
```

在意图分析时如果LLM返回自然语言而不是JSON。

**诊断**: LLM没有遵循JSON格式指令
**解决**: 
1. 简化System prompt
2. 使用更强的模型（如gpt-4o而不是gpt-3.5-turbo）
3. 添加fallback逻辑（已实现）

## 快速诊断命令

### 测试 Home Assistant 连接

在对话中输入：
```
You: refresh
```

应该看到：
```
🔄 Refreshing Home Assistant state...
✅ State refreshed
```

如果失败，说明HA连接有问题。

### 测试LLM基础功能

输入简单问题：
```
You: Hello
```

如果没有任何响应，说明LLM配置有问题。

### 查看系统状态

已经注册的 `GetSystemStats` 工具可以用：
```
You: Show me system stats
```

应该触发工具调用并返回统计信息。

## 开启更详细的日志

### 临时添加HTTP请求体日志

在 `HomeAssistantClient.CallServiceAsync`:

```csharp
var jsonBody = System.Text.Json.JsonSerializer.Serialize(serviceData, _jsonOptions);
System.Console.WriteLine($"[API] Request Body: {jsonBody}");
var response = await _httpClient.PostAsJsonAsync(url, serviceData, _jsonOptions, ct);
```

### 查看完整的流式响应

修改Agent中的日志限制：

```csharp
// 从
if (updateCount <= 5)
    System.Console.WriteLine($"[DEBUG] Stream #{updateCount}: {update}");

// 改为
System.Console.WriteLine($"[DEBUG] Stream #{updateCount}: {update}");
```

（会产生大量输出，但能看到完整对话）

## 故障恢复步骤

如果系统完全无响应：

1. **重启程序**
   ```bash
   # Ctrl+C 停止
   dotnet run  # 重新运行
   ```

2. **刷新缓存**
   ```
   You: refresh
   ```

3. **清除对话历史**
   ```
   You: clear
   ```

4. **检查Home Assistant日志**
   ```bash
   # 在HA主机上
   tail -f /config/home-assistant.log
   ```
   
   或在HA UI中：
   设置 → 系统 → 日志

5. **测试API直接调用**
   ```bash
   # 测试states
   curl -H "Authorization: Bearer YOUR_TOKEN" \
        http://YOUR_HA:8123/api/states
   
   # 测试services
   curl -H "Authorization: Bearer YOUR_TOKEN" \
        http://YOUR_HA:8123/api/services
   ```

## 性能基准

正常情况下的响应时间：

| 操作 | 预期时间 |
|------|---------|
| 启动加载 | 2-5秒 |
| 简单查询（有缓存） | 1-2秒 |
| 简单控制 | 2-4秒 |
| 复杂操作（多工具调用） | 5-10秒 |
| 意图分析 | 1-3秒 |

如果超过这些时间，检查：
- 网络延迟
- LLM响应速度
- Home Assistant负载

## 禁用调试日志

一旦确认系统正常，可以注释掉DEBUG日志：

```bash
# 全局搜索并注释
find . -name "*.cs" -exec sed -i '' 's/System.Console.WriteLine.*DEBUG.*/\/\/ &/' {} \;
```

或者创建一个 `DebugLogger` 类：

```csharp
public static class DebugLogger
{
    public static bool Enabled { get; set; } = true;
    
    public static void Log(string message)
    {
        if (Enabled)
            System.Console.WriteLine(message);
    }
}

// 使用
DebugLogger.Log("[DEBUG] ...");

// 禁用
DebugLogger.Enabled = false;
```

---

**🌌 调试不是修复错误，是理解系统震动的路径。**

