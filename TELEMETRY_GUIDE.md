# OpenTelemetry 追踪指南

## 概述

AI Smart Home 项目已配置了 OpenTelemetry 追踪，可以在 Aspire Dashboard 中查看所有 HTTP 请求和自定义操作的详细追踪信息。

## 已添加的追踪功能

### 1. Home Assistant API 调用追踪

`HomeAssistantClient` 中的所有关键操作都添加了自定义追踪：

#### GetStatesAsync
- **操作名称**: `HomeAssistant.GetStates`
- **追踪标签**:
  - `ha.endpoint`: `/api/states`
  - `ha.entity_count`: 返回的实体数量

#### GetStateAsync
- **操作名称**: `HomeAssistant.GetState`
- **追踪标签**:
  - `ha.entity_id`: 查询的实体 ID
  - `ha.endpoint`: `/api/states/{entityId}`
  - `ha.state`: 实体当前状态
  - 错误时设置 `ActivityStatusCode.Error`

#### CallServiceAsync
- **操作名称**: `HomeAssistant.CallService`
- **追踪标签**:
  - `ha.domain`: 服务域 (如 `fan`, `light`)
  - `ha.service`: 服务名称 (如 `turn_on`, `turn_off`)
  - `ha.entity_id`: 目标实体 ID
  - `ha.endpoint`: API 端点
  - `ha.return_response`: 是否返回响应
  - `http.status_code`: HTTP 响应状态码

### 2. 自动追踪

Aspire ServiceDefaults 自动配置了以下追踪：

- **ASP.NET Core**: 所有 HTTP 请求 (排除 `/health` 和 `/alive`)
- **HttpClient**: 所有出站 HTTP 请求
- **Microsoft.Extensions.AI**: AI 相关操作
- **自定义 ActivitySource**: `AISmartHome.HomeAssistant`

## 如何查看追踪

### 1. 启动 Aspire Dashboard

```bash
cd /Users/eanzhao/Code/ai-smart-home
dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj
```

### 2. 打开 Dashboard

浏览器会自动打开 Aspire Dashboard，或手动访问控制台输出中显示的 URL。

### 3. 导航到"跟踪"页面

在 Dashboard 左侧菜单中，点击"跟踪"(Traces)。

### 4. 触发一些操作

使用 API 或 Web UI 执行一些智能家居控制操作，例如：

```bash
# 通过 API 发送聊天消息
curl -X POST http://localhost:5000/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "打开空气净化器"}'
```

### 5. 查看追踪详情

在"跟踪"页面，你会看到：

- **请求列表**: 显示所有 HTTP 请求
- **追踪时间线**: 点击任意请求查看详细的时间线
- **Span 详情**: 展开每个 Span 查看标签和属性

## 追踪示例

### 控制设备的完整追踪链

当用户说"打开空气净化器"时，你会在 Dashboard 中看到：

```
POST /agent/chat
  ├─ HomeAssistant.GetStates
  │   └─ GET https://home.eanzhao.com/api/states
  ├─ HomeAssistant.GetState
  │   └─ GET https://home.eanzhao.com/api/states/fan.xxx
  ├─ HomeAssistant.CallService
  │   └─ POST https://home.eanzhao.com/api/services/fan/turn_on
  └─ HomeAssistant.GetState (验证)
      └─ GET https://home.eanzhao.com/api/states/fan.xxx
```

### 查看 Span 标签

点击任意 Span，你可以看到详细标签：

**HomeAssistant.CallService** Span:
- `ha.domain`: fan
- `ha.service`: turn_on
- `ha.entity_id`: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
- `ha.endpoint`: /api/services/fan/turn_on
- `http.status_code`: 200

## 性能分析

使用追踪数据，你可以：

1. **识别慢请求**: 查看哪些 Home Assistant API 调用耗时最长
2. **分析调用链**: 了解每个用户请求触发了多少个 API 调用
3. **错误诊断**: 快速定位失败的操作和错误原因
4. **优化机会**: 发现可以并行化或缓存的操作

## 配置位置

### ServiceDefaults (src/AISmartHome.ServiceDefaults/Extensions.cs)

```csharp
.WithTracing(tracing =>
{
    tracing
        .AddSource(builder.Environment.ApplicationName)
        .AddSource("Experimental.Microsoft.Extensions.AI.*")
        .AddSource("AISmartHome.HomeAssistant")  // 我们的自定义追踪
        .AddAspNetCoreInstrumentation(...)
        .AddHttpClientInstrumentation();
});
```

### HomeAssistantClient (src/AISmartHome.Console/Services/HomeAssistantClient.cs)

```csharp
private static readonly ActivitySource ActivitySource = new("AISmartHome.HomeAssistant");

public async Task<ExecutionResult> CallServiceAsync(...)
{
    using var activity = ActivitySource.StartActivity("HomeAssistant.CallService");
    activity?.SetTag("ha.domain", domain);
    activity?.SetTag("ha.service", service);
    // ... 更多标签
}
```

## 故障排除

### Dashboard 中没有显示追踪

1. **检查 OTEL 配置**: 确保 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量已配置
2. **验证 ActivitySource**: 确保 `AISmartHome.HomeAssistant` 已添加到 `ServiceDefaults`
3. **检查过滤器**: 确认你的请求没有被过滤器排除

### 追踪不完整

1. **检查 Activity 创建**: 确保所有关键操作都使用 `using var activity = ...`
2. **验证标签**: 使用 `activity?.SetTag(...)` 添加有用的上下文信息
3. **错误处理**: 在 `catch` 块中设置 `activity?.SetStatus(ActivityStatusCode.Error, ...)`

## 最佳实践

1. **有意义的操作名称**: 使用清晰的名称，如 `HomeAssistant.CallService` 而不是 `DoSomething`
2. **丰富的标签**: 添加足够的上下文信息，但不要过度（避免敏感数据）
3. **一致的命名**: 使用统一的标签命名约定（如 `ha.*` 前缀）
4. **错误记录**: 总是在异常时设置 Activity 状态
5. **嵌套 Span**: 利用 `using` 语句自动管理 Span 的生命周期

## 扩展追踪

### 添加新的 ActivitySource

```csharp
// 1. 创建 ActivitySource
private static readonly ActivitySource ActivitySource = new("YourNamespace.YourComponent");

// 2. 在 ServiceDefaults 中注册
.AddSource("YourNamespace.YourComponent")

// 3. 在代码中使用
using var activity = ActivitySource.StartActivity("YourOperation");
activity?.SetTag("your.tag", value);
```

### 添加自定义标签

```csharp
activity?.SetTag("custom.user_id", userId);
activity?.SetTag("custom.operation_type", "discovery");
activity?.SetTag("custom.result_count", results.Count);
```

---

现在你可以在 Aspire Dashboard 的"跟踪"页面中看到所有 Home Assistant API 调用的详细信息了！🎯

