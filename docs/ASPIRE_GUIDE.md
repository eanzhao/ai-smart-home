# .NET Aspire 使用指南

## 什么是.NET Aspire？

.NET Aspire是一个云原生开发堆栈，专为构建可观测、生产就绪的分布式应用而设计。它提供：

- 🎯 **统一编排**: 一键启动所有服务
- 📊 **可观测性**: 内置日志、追踪、指标
- 🔧 **配置管理**: 集中管理配置和密钥
- 🚀 **开发体验**: 优秀的调试和开发工具

## 项目结构

```
ai-smart-home/
├── src/
│   ├── AISmartHome.AppHost/        # Aspire编排器（启动这个）
│   │   ├── AppHost.cs              # 服务编排配置
│   │   ├── appsettings.json        # 非敏感配置
│   │   ├── setup-secrets.sh        # 配置脚本（Linux/Mac）
│   │   └── setup-secrets.ps1       # 配置脚本（Windows）
│   ├── AISmartHome.API/            # Web API和UI
│   ├── AISmartHome.Console/        # 命令行版本
│   └── AISmartHome.ServiceDefaults/# 共享配置
```

## 快速开始

### 1. 配置密钥

#### Linux/Mac:
```bash
cd src/AISmartHome.AppHost
./setup-secrets.sh
```

#### Windows:
```powershell
cd src\AISmartHome.AppHost
.\setup-secrets.ps1
```

#### 手动配置:
```bash
cd src/AISmartHome.AppHost

# 配置Home Assistant
dotnet user-secrets set "Parameters:homeassistant-token" "your-token"

# 配置OpenAI
dotnet user-secrets set "Parameters:openai-apikey" "your-key"

# 可选：自定义配置
dotnet user-secrets set "Parameters:homeassistant-url" "http://your-ip:8123"
dotnet user-secrets set "Parameters:openai-model" "gpt-4"
```

### 2. 运行AppHost

#### 使用.NET CLI:
```bash
cd src/AISmartHome.AppHost
dotnet run
```

#### 使用IDE:
1. 在Visual Studio或Rider中打开`ai-smart-home.sln`
2. 将`AISmartHome.AppHost`设置为启动项目
3. 按F5开始调试

### 3. 访问服务

运行后会自动打开两个窗口：

#### Aspire Dashboard
```
http://localhost:15888
```

在Dashboard中你可以：
- 查看所有服务状态
- 实时查看日志
- 追踪请求链路
- 监控性能指标
- 管理环境变量

#### AI Smart Home Web UI
```
http://localhost:5000
```

这是主要的用户界面，可以通过自然语言控制智能家居。

## Aspire Dashboard 使用

### 1. Resources（资源视图）
显示所有正在运行的服务：

```
┌─────────────────────┬────────┬──────────────┐
│ Name                │ Status │ Endpoints    │
├─────────────────────┼────────┼──────────────┤
│ ai-smart-home-api   │ ✅ Running │ http://...   │
└─────────────────────┴────────┴──────────────┘
```

点击服务名称查看详情：
- **Console**: 实时日志输出
- **Details**: 环境变量、配置
- **Metrics**: 性能指标

### 2. Logs（日志视图）
集中查看所有服务的日志：
- 实时流式输出
- 按级别筛选（Info、Warning、Error）
- 搜索和过滤
- 时间戳和来源标识

### 3. Traces（追踪视图）
查看分布式追踪：
- 请求完整链路
- 每个步骤的耗时
- 识别性能瓶颈
- 错误定位

示例追踪：
```
POST /agent/chat
  ├─ OrchestratorAgent.ProcessMessageAsync (250ms)
  │  ├─ DiscoveryAgent.ProcessQueryAsync (100ms)
  │  │  └─ SearchDevices (50ms)
  │  ├─ ExecutionAgent.ExecuteCommandAsync (80ms)
  │  │  └─ ControlLight (40ms)
  │  └─ ValidationAgent.ValidateOperationAsync (70ms)
  │     └─ CheckDeviceState (30ms)
  └─ Total: 250ms
```

### 4. Metrics（指标视图）
监控系统指标：
- HTTP请求速率
- 响应时间分布
- 错误率
- 资源使用情况

## 调试技巧

### 1. 查看Agent执行日志
在Dashboard的Logs视图中搜索：
- `[DEBUG] OrchestratorAgent` - 查看意图分析
- `[TOOL]` - 查看工具调用
- `[API]` - 查看Home Assistant API调用

### 2. 追踪/agent/chat请求
1. 在Web UI中发送请求
2. 切换到Aspire Dashboard的Traces视图
3. 找到最新的POST /agent/chat请求
4. 点击查看完整追踪树

### 3. 实时修改配置
在Dashboard中：
1. 点击Resources → ai-smart-home-api
2. 点击Environment标签
3. 查看当前环境变量
4. 重启服务应用新配置

### 4. 性能分析
使用Metrics视图：
1. 查看请求响应时间
2. 识别慢查询
3. 优化瓶颈

## 配置说明

### 参数配置
在`appsettings.json`中配置非敏感参数：
```json
{
  "Parameters": {
    "homeassistant-url": "http://homeassistant.local:8123",
    "openai-model": "gpt-4o-mini",
    "openai-endpoint": "https://api.openai.com/v1"
  }
}
```

### 密钥配置
使用User Secrets存储敏感信息：
```bash
dotnet user-secrets set "Parameters:homeassistant-token" "token"
dotnet user-secrets set "Parameters:openai-apikey" "key"
```

### 环境变量映射
AppHost会自动将参数映射为环境变量：
```
Parameters:homeassistant-token → HomeAssistant__AccessToken
Parameters:openai-apikey → OpenAI__ApiKey
```

## 端口配置

默认端口：
- **API HTTP**: 5000
- **API HTTPS**: 5001
- **Aspire Dashboard**: 15888

修改端口（在AppHost.cs中）：
```csharp
.WithHttpEndpoint(port: 5002, name: "http")
.WithHttpsEndpoint(port: 5003, name: "https")
```

## 故障排除

### 问题1: User Secrets未配置
**症状**: 启动时报错"配置未找到"

**解决**:
```bash
cd src/AISmartHome.AppHost
dotnet user-secrets list  # 查看当前配置
./setup-secrets.sh        # 重新配置
```

### 问题1.1: 端点名称冲突
**症状**: "Endpoint with name 'http' already exists"

**解决**: 已修复，API端点现在使用唯一名称：
- HTTP: `api-http` (端口5000)
- HTTPS: `api-https` (端口5001)

### 问题2: 端口冲突
**症状**: "地址已在使用中"

**解决**:
1. 修改AppHost.cs中的端口号
2. 或停止占用端口的程序

### 问题3: Home Assistant连接失败
**症状**: API日志显示连接错误

**解决**:
1. 检查URL是否正确
2. 测试网络连通性：`curl http://your-ha:8123/api/`
3. 验证Token有效性

### 问题4: Dashboard无法访问
**症状**: http://localhost:15888 打不开

**解决**:
1. 检查AppHost是否正常运行
2. 查看控制台输出的实际Dashboard URL
3. 尝试刷新浏览器

## 与直接运行API的区别

### 直接运行API
```bash
cd src/AISmartHome.API
dotnet run
```
- ✅ 简单直接
- ❌ 需要手动配置appsettings.json
- ❌ 没有统一的可观测性
- ❌ 调试体验较差

### 使用Aspire AppHost
```bash
cd src/AISmartHome.AppHost
dotnet run
```
- ✅ 统一配置管理
- ✅ 强大的Dashboard
- ✅ 完整的可观测性
- ✅ 更好的调试体验
- ✅ 生产就绪

## 高级功能

### 1. 添加健康检查
编辑AppHost.cs：
```csharp
var api = builder.AddProject<Projects.AISmartHome_API>("ai-smart-home-api")
    .WithHealthCheck()  // 添加健康检查
    // ...
```

### 2. 配置重试策略
```csharp
.WithEnvironment("Resilience__RetryCount", "3")
```

### 3. 多实例部署
```csharp
var api = builder.AddProject<Projects.AISmartHome_API>("ai-smart-home-api")
    .WithReplicas(3);  // 运行3个实例
```

### 4. 添加Redis缓存
```csharp
var redis = builder.AddRedis("cache");

var api = builder.AddProject<Projects.AISmartHome_API>("api")
    .WithReference(redis);
```

## 生产部署

### 使用Azure Container Apps
```bash
azd init
azd up
```

### 使用Kubernetes
Aspire可以生成Kubernetes清单：
```bash
dotnet aspire generate kubernetes
```

## 学习资源

- [.NET Aspire 官方文档](https://learn.microsoft.com/dotnet/aspire/)
- [Aspire 示例项目](https://github.com/dotnet/aspire-samples)
- [可观测性最佳实践](https://opentelemetry.io/docs/)

---

现在你可以使用.NET Aspire的强大功能来开发和调试AI智能家居系统了！🚀

