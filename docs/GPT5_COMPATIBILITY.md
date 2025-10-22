# GPT-5 兼容性配置

## I'm HyperEcho, 在适配 GPT-5 回响

## 问题描述

使用 GPT-5 模型时出现以下错误：

```
Error processing request: HTTP 400 (invalid_request_error: unsupported_value)
Parameter: temperature

Unsupported value: 'temperature' does not support 0 with this model. 
Only the default (1) value is supported.
```

## 原因

GPT-5 模型不支持自定义 `temperature` 参数，只支持默认值 `1`。

之前的代码在 `DiscoveryAgent` 和 `ExecutionAgent` 中设置了 `Temperature = 0.0f`，这对于：
- ✅ GPT-4o-mini: 支持
- ✅ GPT-4o: 支持
- ✅ GPT-4: 支持
- ❌ GPT-5: **不支持**

## 解决方案

### 已修改的文件

#### 1. `src/AISmartHome.Console/Agents/DiscoveryAgent.cs`

**之前**:
```csharp
var options = new ChatOptions
{
    Tools = tools,
    Temperature = 0.0f  // Use deterministic output for consistent formatting
};
```

**现在**:
```csharp
var options = new ChatOptions
{
    Tools = tools
    // Note: Temperature removed for compatibility with models like GPT-5
    // that don't support custom temperature values
};
```

#### 2. `src/AISmartHome.Console/Agents/ExecutionAgent.cs`

**之前**:
```csharp
var options = new ChatOptions
{
    Tools = tools,
    Temperature = 0.0f  // Use deterministic output for reliable execution
};
```

**现在**:
```csharp
var options = new ChatOptions
{
    Tools = tools
    // Note: Temperature removed for compatibility with models like GPT-5
    // that don't support custom temperature values
};
```

## 使用 GPT-5

### 配置 User Secrets

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost

# 设置 GPT-5 模型
dotnet user-secrets set "Parameters:llm-model" "gpt-5"

# 或者图形化编辑
./edit-secrets.sh
```

在 `secrets.json` 中：

```json
{
  "Parameters:homeassistant-url": "https://home.eanzhao.com",
  "Parameters:homeassistant-token": "你的Token",
  "Parameters:llm-apikey": "你的OpenAI API密钥",
  "Parameters:llm-model": "gpt-5",
  "Parameters:llm-endpoint": "https://api.openai.com/v1"
}
```

### 启动应用

```bash
# 使用 Aspire
dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj

# 或直接运行 API
cd src/AISmartHome.API
dotnet run

# 或直接运行 Console
cd src/AISmartHome.Console
dotnet run
```

## 支持的模型

现在系统支持以下所有 OpenAI 模型：

| 模型 | Temperature 支持 | 推荐设置 |
|------|-----------------|---------|
| GPT-4o-mini | ✅ 0-2 | `gpt-4o-mini` |
| GPT-4o | ✅ 0-2 | `gpt-4o` |
| GPT-4 | ✅ 0-2 | `gpt-4` |
| GPT-5 | ❌ 仅支持默认值 1 | `gpt-5` |

## 其他 LLM 提供商

### GitHub Models

```json
{
  "Parameters:llm-apikey": "github_pat_...",
  "Parameters:llm-model": "gpt-4o-mini",
  "Parameters:llm-endpoint": "https://models.github.ai/inference"
}
```

### Azure OpenAI

```json
{
  "Parameters:llm-apikey": "你的Azure密钥",
  "Parameters:llm-model": "gpt-4o",
  "Parameters:llm-endpoint": "https://your-resource.openai.azure.com/openai/deployments/your-deployment"
}
```

### OpenRouter

```json
{
  "Parameters:llm-apikey": "sk-or-...",
  "Parameters:llm-model": "openai/gpt-5",
  "Parameters:llm-endpoint": "https://openrouter.ai/api/v1"
}
```

## 性能影响

### Temperature = 0 的优势（之前）
- ✅ 更确定性的输出
- ✅ 重复查询得到相同结果
- ✅ 更适合结构化任务

### 默认 Temperature = 1（现在）
- ✅ 兼容所有模型包括 GPT-5
- ✅ 更自然、更多样化的输出
- ⚠️ 稍微降低确定性

**实际影响**: 对于智能家居控制任务，影响很小。GPT-5 的默认 temperature 仍能提供高质量的结构化输出。

## 验证修复

### 测试步骤

1. **配置 GPT-5**:
```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./edit-secrets.sh
# 设置 llm-model 为 "gpt-5"
```

2. **启动应用**:
```bash
dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj
```

3. **测试命令**:
```bash
# 通过 API
curl -X POST http://localhost:5000/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "打开空气净化器"}'
```

4. **预期结果**: ✅ 不再出现 temperature 错误

## 故障排除

### 问题1: 仍然出现 temperature 错误

**检查**:
```bash
cd /Users/eanzhao/Code/ai-smart-home
grep -r "Temperature.*=" src/AISmartHome.Console/Agents/
```

应该只看到注释，没有实际设置。

### 问题2: 其他 400 错误

**可能原因**:
- API 密钥无效
- 模型名称错误
- Endpoint 配置错误

**解决**:
```bash
# 检查配置
cd src/AISmartHome.AppHost
dotnet user-secrets list

# 验证 API 密钥
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer 你的API密钥"
```

### 问题3: 想要更确定性的输出

如果你使用的是 GPT-4o-mini 或 GPT-4o，可以手动设置 temperature：

```csharp
var options = new ChatOptions
{
    Tools = tools,
    Temperature = 0.0f  // 只对支持的模型有效
};
```

但这会失去 GPT-5 兼容性。

## 最佳实践

1. **使用默认 Temperature**: 除非有特殊需求，不要设置 temperature
2. **模型选择**: 
   - 开发/测试: `gpt-4o-mini` (便宜、快速)
   - 生产: `gpt-5` (最强大)
3. **错误处理**: 监控 API 错误，及时调整配置
4. **成本优化**: GPT-4o-mini 成本仅为 GPT-5 的 1/20

---

现在你可以无缝切换到 GPT-5 了！🚀

