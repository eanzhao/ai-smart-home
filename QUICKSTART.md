# 🚀 快速开始指南

## 前置要求

1. **.NET 9.0 SDK** 
   ```bash
   dotnet --version  # 应该显示 9.x.x
   ```

2. **Home Assistant 实例**
   - 运行中的 Home Assistant（本地或远程）
   - 可访问的 HTTP 端点（通常是 `http://IP:8123`）

3. **OpenAI API Key**
   - 从 [OpenAI Platform](https://platform.openai.com/) 获取
   - 或使用 Azure OpenAI Service

## 配置步骤

### Step 1: 获取 Home Assistant Access Token

1. 打开 Home Assistant Web UI
2. 点击左下角的用户头像
3. 滚动到**安全 (Security)** 部分
4. 找到**长期访问令牌 (Long-Lived Access Tokens)**
5. 点击**创建令牌**
6. 给令牌命名（如："AI Agent"）
7. 复制生成的令牌（**重要**: 只会显示一次！）

### Step 2: 配置项目

1. 复制配置模板：
   ```bash
   cd src/AISmartHome.Console
   cp appsettings.example.json appsettings.json
   ```

2. 编辑 `appsettings.json`：
   ```json
   {
     "HomeAssistant": {
       "BaseUrl": "http://192.168.1.100:8123",  // 你的 HA 地址
       "AccessToken": "eyJhbGc...你的长Token"    // 粘贴你的 Token
     },
     "OpenAI": {
       "ApiKey": "sk-proj-...你的OpenAI密钥",   // 你的 OpenAI Key
       "Model": "gpt-4o"
     }
   }
   ```

### Step 3: 恢复依赖

```bash
dotnet restore
```

### Step 4: 运行系统

```bash
dotnet run
```

你应该看到：

```
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║        🌌 HyperEcho AI Smart Home Control System 🌌       ║
║                                                           ║
║   语言的震动体 × 智能家居的共振                              ║
║   Language as vibration × Smart Home resonance            ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝

🔗 Connecting to Home Assistant...
✅ Connected to Home Assistant at http://192.168.1.100:8123
📋 Loading Home Assistant state...
✅ Loaded 67 entities across 12 domains
✅ Loaded 243 services
🤖 Initializing AI agents...
✅ Multi-Agent system initialized
```

## 测试连接

### 基础测试

输入第一个命令：

```
🗣️  You: 系统状态
```

如果看到类似输出，说明一切正常：

```json
{
  "total_entities": 67,
  "total_services": 243,
  "domain_count": 12,
  "entity_breakdown": {
    "sensor": 25,
    "light": 12,
    ...
  }
}
```

### 发现测试

```
🗣️  You: 我有哪些灯？
```

应该返回你的灯光设备列表。

### 控制测试

```
🗣️  You: 打开[某个灯的名字]
```

观察实际设备是否响应。

## 故障排查

### 问题 1: 无法连接到 Home Assistant

```
❌ Failed to connect to Home Assistant. Check your configuration.
```

**解决方法**:
- 检查 `BaseUrl` 是否正确（包括 `http://` 前缀和端口号）
- 确认 Home Assistant 正在运行
- 尝试在浏览器访问该 URL
- 检查防火墙设置

### 问题 2: 401 Unauthorized

```
❌ Failed to call service: 401 Unauthorized
```

**解决方法**:
- 检查 `AccessToken` 是否正确
- Token 可能过期，重新生成
- 确认 Token 有足够权限

### 问题 3: OpenAI API 错误

```
❌ Error: The API key is invalid
```

**解决方法**:
- 检查 `OpenAI:ApiKey` 是否正确
- 确认 API Key 有余额
- 检查网络是否能访问 OpenAI API

### 问题 4: 找不到设备

```
No devices found matching 'xxx'.
```

**解决方法**:
- 执行 `refresh` 命令刷新缓存
- 检查设备在 Home Assistant UI 中是否可见
- 尝试使用更通用的查询（如只说"灯"而不是"客厅的那个智能灯"）

## 高级配置

### 使用环境变量（推荐生产环境）

```bash
export HOMEASSISTANT__BASEURL="http://192.168.1.100:8123"
export HOMEASSISTANT__ACCESSTOKEN="your_token_here"
export OPENAI__APIKEY="sk-your-key-here"

dotnet run
```

### 使用 Azure OpenAI

修改 `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-azure-key",
    "Model": "gpt-4o",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4o-deployment-name"
  }
}
```

修改 `Program.cs` 中的初始化代码：

```csharp
var endpoint = configuration["OpenAI:Endpoint"] 
    ?? "https://api.openai.com/v1";
var deploymentName = configuration["OpenAI:DeploymentName"] 
    ?? configuration["OpenAI:Model"];

var openAiClient = new AzureOpenAIClient(
    new Uri(endpoint), 
    new Azure.AzureKeyCredential(openAiKey)
);
var chatClient = openAiClient.AsChatClient(deploymentName);
```

### 使用本地 LLM (Ollama)

需要修改为使用 Ollama 客户端：

```bash
# 安装 Ollama
curl -fsSL https://ollama.com/install.sh | sh

# 下载模型
ollama pull llama3.1

# 运行 Ollama 服务
ollama serve
```

修改代码使用 Ollama endpoint (需要额外的包或自定义 client)。

## 性能优化

### 减少 API 调用

修改缓存过期时间（在 `EntityRegistry.cs` 和 `ServiceRegistry.cs`）:

```csharp
// EntityRegistry.cs
private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10); // 从5分钟改为10分钟

// ServiceRegistry.cs  
private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);   // 从1小时改为24小时
```

### 限制返回结果

修改 `DiscoveryTools.cs` 中的结果数量：

```csharp
var results = entities.Take(5).Select(...) // 从10改为5
```

## 日志调试

### 启用详细日志

修改 `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "System": "Debug"
    }
  }
}
```

### 查看 HTTP 请求

在 `HomeAssistantClient.cs` 中添加日志：

```csharp
public async Task<List<HAEntity>> GetStatesAsync(CancellationToken ct = default)
{
    Console.WriteLine($"[DEBUG] Calling GET /api/states");
    var response = await _httpClient.GetAsync("/api/states", ct);
    Console.WriteLine($"[DEBUG] Response: {response.StatusCode}");
    // ... rest of the code
}
```

## 下一步

1. ✅ 完成配置和测试
2. 📖 阅读 [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) 学习更多使用场景
3. 🏗️ 阅读 [ARCHITECTURE.md](ARCHITECTURE.md) 了解系统架构
4. 🔧 根据需求扩展新的 Agent 或 Tool
5. 🌟 享受智能家居的语义控制体验！

## 常见场景快捷指令

```bash
# 离家模式
"我要出门了，帮我关闭所有设备"

# 回家模式  
"我到家了"

# 睡眠模式
"准备睡觉模式"

# 观影模式
"我要看电影，帮我准备环境"

# 能源检查
"有什么设备还开着？"

# 温度调节
"好热/好冷" → 系统自动建议操作
```

## 获取帮助

- 📖 查看 README.md 了解功能
- 🏗️ 查看 ARCHITECTURE.md 了解原理
- 💬 查看 USAGE_EXAMPLES.md 看更多示例
- 🐛 遇到问题？在项目中创建 Issue

---

**开始你的智能家居语义控制之旅！** 🚀

