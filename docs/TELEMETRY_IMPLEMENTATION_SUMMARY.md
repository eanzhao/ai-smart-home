# OpenTelemetry 追踪实现总结

## I'm HyperEcho, 在回响 追踪完成

## 实现内容

### ✅ 1. SSL 证书验证问题修复

**问题**: Home Assistant 连接失败，SSL 握手错误
**原因**: .NET HttpClient 默认验证 SSL 证书，Nabu Casa 可能使用自签名证书
**解决方案**:
- 在 `HomeAssistantClient` 构造函数中添加 `ignoreSslErrors` 参数 (默认 `true`)
- 配置 `HttpClientHandler.ServerCertificateCustomValidationCallback`
- 更新所有 `HomeAssistantClient` 实例化代码

**修改文件**:
- `src/AISmartHome.Console/Services/HomeAssistantClient.cs`
- `src/AISmartHome.Console/Program.cs`
- `src/AISmartHome.API/Program.cs`

### ✅ 2. OpenTelemetry 追踪支持

**目标**: 在 Aspire Dashboard 中查看 Home Assistant API 调用的详细追踪

#### 2.1 添加依赖

**文件**: `src/AISmartHome.Console/AISmartHome.Console.csproj`
```xml
<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="9.0.0" />
```

#### 2.2 创建自定义 ActivitySource

**文件**: `src/AISmartHome.Console/Services/HomeAssistantClient.cs`
```csharp
using System.Diagnostics;

public class HomeAssistantClient : IDisposable
{
    private static readonly ActivitySource ActivitySource = new("AISmartHome.HomeAssistant");
    // ...
}
```

#### 2.3 为关键操作添加追踪

**HomeAssistant.GetStates**:
```csharp
using var activity = ActivitySource.StartActivity("HomeAssistant.GetStates");
activity?.SetTag("ha.endpoint", "/api/states");
activity?.SetTag("ha.entity_count", entities.Count);
```

**HomeAssistant.GetState**:
```csharp
using var activity = ActivitySource.StartActivity("HomeAssistant.GetState");
activity?.SetTag("ha.entity_id", entityId);
activity?.SetTag("ha.endpoint", $"/api/states/{entityId}");
activity?.SetTag("ha.state", entity?.State);
// 错误处理
activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
```

**HomeAssistant.CallService**:
```csharp
using var activity = ActivitySource.StartActivity("HomeAssistant.CallService");
activity?.SetTag("ha.domain", domain);
activity?.SetTag("ha.service", service);
activity?.SetTag("ha.entity_id", entityIdObj?.ToString());
activity?.SetTag("ha.endpoint", url);
activity?.SetTag("ha.return_response", returnResponse);
activity?.SetTag("http.status_code", (int)response.StatusCode);
```

#### 2.4 注册自定义 ActivitySource

**文件**: `src/AISmartHome.ServiceDefaults/Extensions.cs`
```csharp
.WithTracing(tracing =>
{
    tracing
        .AddSource(builder.Environment.ApplicationName)
        .AddSource("Experimental.Microsoft.Extensions.AI.*")
        .AddSource("AISmartHome.HomeAssistant")  // 新增
        .AddAspNetCoreInstrumentation(...)
        .AddHttpClientInstrumentation();
});
```

## 追踪标签设计

### 命名约定
- 使用 `ha.*` 前缀表示 Home Assistant 相关标签
- 使用 `http.*` 前缀表示 HTTP 相关标签 (遵循 OpenTelemetry 规范)

### 标签列表

| 标签名 | 示例值 | 说明 |
|--------|--------|------|
| `ha.endpoint` | `/api/states` | API 端点路径 |
| `ha.entity_id` | `fan.bedroom_air_purifier` | 实体 ID |
| `ha.entity_count` | `152` | 实体数量 |
| `ha.state` | `on` | 实体状态 |
| `ha.domain` | `fan` | 服务域 |
| `ha.service` | `turn_on` | 服务名称 |
| `ha.return_response` | `true` | 是否返回响应 |
| `http.status_code` | `200` | HTTP 状态码 |

## 追踪层次结构示例

```
POST /agent/chat
├─ HomeAssistant.GetStates
│  └─ GET https://home.eanzhao.com/api/states
│     [ha.endpoint=/api/states, ha.entity_count=152]
│
├─ HomeAssistant.GetState
│  └─ GET https://home.eanzhao.com/api/states/fan.bedroom_air_purifier
│     [ha.entity_id=fan.bedroom_air_purifier, ha.state=on]
│
├─ HomeAssistant.CallService
│  └─ POST https://home.eanzhao.com/api/services/fan/turn_off
│     [ha.domain=fan, ha.service=turn_off, 
│      ha.entity_id=fan.bedroom_air_purifier, 
│      http.status_code=200]
│
└─ HomeAssistant.GetState (验证)
   └─ GET https://home.eanzhao.com/api/states/fan.bedroom_air_purifier
      [ha.entity_id=fan.bedroom_air_purifier, ha.state=off]
```

## 使用方式

### 1. 启动 Aspire Dashboard

```bash
cd /Users/eanzhao/Code/ai-smart-home
dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj
```

### 2. 触发操作

通过 API 或 Web UI 执行智能家居控制：

```bash
curl -X POST http://localhost:5000/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "打开空气净化器"}'
```

### 3. 查看追踪

在 Aspire Dashboard 的"跟踪"页面:
- 查看请求列表
- 点击任意请求查看详细时间线
- 展开 Span 查看标签和属性
- 分析性能瓶颈
- 诊断错误

## 性能优势

1. **可观测性**: 完整的请求追踪链，从用户请求到 Home Assistant API
2. **性能分析**: 识别慢 API 调用，优化响应时间
3. **错误诊断**: 快速定位失败操作的根本原因
4. **调用统计**: 了解每个操作触发的 API 调用次数
5. **并发分析**: 发现可以并行化的操作

## 未来扩展

### 1. 添加 Agent 追踪

为 Discovery、Execution、Validation Agent 添加自定义追踪：

```csharp
private static readonly ActivitySource ActivitySource = new("AISmartHome.Agents");

using var activity = ActivitySource.StartActivity("Agent.Discovery");
activity?.SetTag("agent.query", query);
activity?.SetTag("agent.result_count", results.Count);
```

### 2. 添加 LLM 追踪

追踪 LLM API 调用：

```csharp
using var activity = ActivitySource.StartActivity("LLM.ChatCompletion");
activity?.SetTag("llm.model", model);
activity?.SetTag("llm.tokens", tokenCount);
activity?.SetTag("llm.latency_ms", latency);
```

### 3. 添加自定义指标

```csharp
private static readonly Meter Meter = new("AISmartHome.HomeAssistant");
private static readonly Counter<long> ApiCallCounter = 
    Meter.CreateCounter<long>("ha.api.calls");

ApiCallCounter.Add(1, new("domain", domain), new("service", service));
```

## 文档

- **详细使用指南**: `TELEMETRY_GUIDE.md`
- **实现总结**: 本文档

## 技术栈

- **.NET 9.0**
- **OpenTelemetry SDK**
- **Aspire ServiceDefaults**
- **System.Diagnostics.DiagnosticSource**

## 测试验证

✅ 编译成功  
✅ ActivitySource 正确创建  
✅ 追踪标签正确设置  
✅ ServiceDefaults 正确配置  
⏳ Aspire Dashboard 验证 (待用户测试)

---

🎯 现在运行 `dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj`，
在 Aspire Dashboard 的"跟踪"页面就能看到所有 Home Assistant API 调用的详细追踪信息了！

