# AISmartHome.AppHost

.NET Aspire AppHost for orchestrating the AI Smart Home system.

## 配置

### 使用User Secrets存储敏感信息

1. **配置Home Assistant Token**
   ```bash
   cd src/AISmartHome.AppHost
   dotnet user-secrets set "Parameters:homeassistant-token" "your-home-assistant-long-lived-token"
   ```

2. **配置OpenAI API Key**
   ```bash
   dotnet user-secrets set "Parameters:openai-apikey" "your-openai-api-key"
   ```

### 可选：覆盖默认配置

如果你的Home Assistant不在`http://homeassistant.local:8123`，可以设置：

```bash
dotnet user-secrets set "Parameters:homeassistant-url" "http://your-ip:8123"
```

如果你想使用不同的OpenAI模型：

```bash
dotnet user-secrets set "Parameters:openai-model" "gpt-4"
```

如果你使用自定义OpenAI端点（如Azure OpenAI）：

```bash
dotnet user-secrets set "Parameters:openai-endpoint" "https://your-azure-endpoint.openai.azure.com/v1"
```

## 运行

### 方式一：使用.NET CLI
```bash
cd src/AISmartHome.AppHost
dotnet run
```

### 方式二：使用Visual Studio/Rider
- 将AISmartHome.AppHost设置为启动项目
- 按F5开始调试

## Aspire Dashboard

运行AppHost后，会自动打开Aspire Dashboard：
```
http://localhost:15888
```

在Dashboard中你可以：
- 📊 查看所有服务状态
- 📝 查看日志输出
- 🔍 追踪请求
- 📈 监控性能指标
- 🌐 访问服务端点

## 访问服务

AppHost会自动配置以下端点：
- **API (HTTP)**: http://localhost:5000
- **API (HTTPS)**: https://localhost:5001
- **Web UI**: http://localhost:5000 或 https://localhost:5001

## 调试

### 查看服务日志
在Aspire Dashboard中：
1. 点击左侧的"Resources"
2. 选择"ai-smart-home-api"
3. 点击"Logs"标签查看实时日志

### 查看环境变量
在Aspire Dashboard中：
1. 点击"ai-smart-home-api"
2. 点击"Details"标签
3. 查看"Environment"部分

### 追踪请求
1. 在Aspire Dashboard中点击"Traces"
2. 查看/agent/chat请求的完整追踪
3. 分析性能瓶颈

## 优势

使用Aspire AppHost的好处：
- ✅ **统一启动**: 一键启动所有服务
- ✅ **配置管理**: 集中管理配置和密钥
- ✅ **可观测性**: 内置日志、追踪、指标
- ✅ **服务发现**: 自动服务发现和健康检查
- ✅ **开发体验**: 更好的调试和开发体验

## 故障排除

### 端口冲突
如果5000或5001端口被占用，可以修改`AppHost.cs`中的端口：

```csharp
.WithHttpEndpoint(port: 5002, name: "http")
.WithHttpsEndpoint(port: 5003, name: "https")
```

### User Secrets未配置
如果看到配置错误，确保已经设置了User Secrets：

```bash
dotnet user-secrets list
```

应该显示：
```
Parameters:homeassistant-token = ***
Parameters:openai-apikey = ***
```

### Home Assistant连接失败
检查：
1. Home Assistant URL是否正确
2. Access Token是否有效
3. 网络是否可达

## 架构

```
AppHost (Orchestrator)
   ↓
AISmartHome.API (Web API + UI)
   ↓
   ├─→ Home Assistant (External)
   └─→ OpenAI API (External)
```

## 下一步

- [ ] 添加Redis缓存
- [ ] 添加消息队列
- [ ] 添加多个API实例（负载均衡）
- [ ] 添加监控告警
- [ ] 集成CI/CD部署

