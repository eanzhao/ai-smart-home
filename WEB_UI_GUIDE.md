# AI Smart Home Web UI 使用指南

## 概述

我已经为你创建了一个完整的Web UI界面，模仿ChatGPT的风格，用于控制智能家居设备。

## 功能特点

### 🎨 界面设计
- **ChatGPT风格**: 深色主题，现代化的聊天界面
- **左侧边栏**: 显示设备统计信息，包括各类型设备的数量
- **中间聊天区**: 与AI进行自然语言对话
- **底部输入框**: 支持多行输入，Shift+Enter换行，Enter发送
- **流式响应**: AI回复逐字显示，就像ChatGPT一样

### 🤖 AI功能
- 使用与Console项目**完全相同的多Agent系统**
- 支持所有智能家居控制功能
- Entity ID验证机制
- 实时设备状态查询

### 🚀 API端点

#### POST /agent/chat
- 功能：与AI助手对话
- 请求体：`{ "message": "你的指令" }`
- 响应：Server-Sent Events (SSE)流式响应
- 示例：
  ```bash
  curl -X POST http://localhost:5000/agent/chat \
    -H "Content-Type: application/json" \
    -d '{"message": "打开客厅灯"}'
  ```

#### GET /agent/stats
- 功能：获取设备统计信息
- 响应：
  ```json
  {
    "totalDevices": 50,
    "domains": [
      {"domain": "light", "count": 20},
      {"domain": "switch", "count": 10},
      ...
    ]
  }
  ```

#### GET /agent/devices
- 功能：列出所有设备
- 查询参数：`domain`（可选）- 筛选特定域的设备
- 示例：`/agent/devices?domain=light`
- 响应：
  ```json
  [
    {
      "entity_id": "light.living_room",
      "friendly_name": "客厅灯",
      "state": "on",
      "domain": "light"
    },
    ...
  ]
  ```

## 配置

1. **复制配置文件**
   ```bash
   cd src/AISmartHome.API
   cp appsettings.example.json appsettings.json
   ```

2. **编辑配置**
   ```json
   {
     "HomeAssistant": {
       "BaseUrl": "http://your-home-assistant:8123",
       "AccessToken": "your-long-lived-access-token"
     },
     "OpenAI": {
       "ApiKey": "your-openai-api-key",
       "Model": "gpt-4o-mini",
       "Endpoint": "https://api.openai.com/v1"
     }
   }
   ```

3. **Home Assistant Token获取方法**
   - 登录Home Assistant
   - 点击头像 → 安全 → 长期访问令牌
   - 创建新令牌并复制

## 运行

### 方式一：使用.NET CLI
```bash
cd src/AISmartHome.API
dotnet run
```

### 方式二：使用IDE
- Visual Studio：直接运行AISmartHome.API项目
- JetBrains Rider：选择AISmartHome.API配置并运行

## 访问

启动后，在浏览器中打开：
```
http://localhost:5000
```

或检查控制台输出的URL：
```
🚀 Smart Home AI API is running!
📡 Home Assistant: http://homeassistant.local:8123
🤖 LLM Model: gpt-4o-mini
🌐 Open http://localhost:5000 to access the UI
```

## 使用示例

### 基本对话
1. **查询设备**
   ```
   我有哪些灯？
   显示所有空调
   卧室有什么设备？
   ```

2. **控制设备**
   ```
   打开客厅灯
   关闭所有风扇
   调节空调温度到25度
   把卧室灯亮度调到80%
   ```

3. **复杂操作**
   ```
   打开客厅灯并调到50%亮度
   关闭所有媒体播放器
   打开空气净化器并设置速度为高
   ```

### 示例按钮
Web UI提供了四个快速示例按钮：
- "我有哪些灯？" - 查询所有灯光设备
- "打开客厅灯" - 控制客厅灯
- "调节空调温度到25度" - 设置温度
- "关闭所有风扇" - 批量控制

## 技术架构

### 后端
- **ASP.NET Core 9.0**: Web框架
- **Minimal API**: 轻量级API端点
- **Server-Sent Events (SSE)**: 流式响应
- **Dependency Injection**: 服务管理
- **多Agent系统**: Discovery、Execution、Validation、Orchestrator

### 前端
- **原生HTML/CSS/JavaScript**: 无需构建步骤
- **现代CSS**: 使用CSS变量和深色主题
- **Fetch API**: 异步HTTP请求
- **ReadableStream**: 处理SSE流式响应
- **响应式设计**: 适配不同屏幕尺寸

### Agent系统
```
用户输入
   ↓
OrchestratorAgent (意图分析)
   ↓
   ├─→ DiscoveryAgent (设备发现)
   ├─→ ExecutionAgent (执行操作)
   └─→ ValidationAgent (验证结果)
   ↓
流式返回给用户
```

## 调试

### 查看日志
API会输出详细的调试日志到控制台，包括：
- Agent调用
- Tool执行
- Entity验证
- API请求

### 浏览器开发者工具
- 按F12打开开发者工具
- 查看Console标签的JavaScript日志
- 查看Network标签的网络请求

### 常见问题

1. **无法连接Home Assistant**
   - 检查BaseUrl配置是否正确
   - 检查AccessToken是否有效
   - 确保Home Assistant可访问

2. **AI不响应**
   - 检查OpenAI API Key是否正确
   - 检查网络连接
   - 查看浏览器Console的错误信息

3. **设备控制失败**
   - 检查entity_id是否存在
   - 查看API日志的验证错误
   - 确认设备在Home Assistant中可用

## 扩展功能

### 添加新的API端点
编辑`src/AISmartHome.API/Program.cs`，添加新的MapGet/MapPost：
```csharp
app.MapGet("/api/myendpoint", () => {
    return Results.Ok(new { message = "Hello" });
});
```

### 自定义UI
编辑`src/AISmartHome.API/wwwroot/index.html`：
- 修改CSS变量更改主题颜色
- 添加新的HTML元素
- 扩展JavaScript功能

### 添加新的Agent
1. 在Console项目中创建新的Agent类
2. 在API的Program.cs中注册：
   ```csharp
   builder.Services.AddSingleton<MyNewAgent>();
   ```
3. 在需要的地方使用

## 安全建议

1. **生产环境部署**
   - 使用HTTPS
   - 添加身份验证
   - 限制CORS策略
   - 使用环境变量存储敏感信息

2. **配置管理**
   - 不要将appsettings.json提交到版本控制
   - 使用User Secrets或环境变量
   - 定期轮换API密钥和Token

3. **速率限制**
   - 考虑添加请求速率限制
   - 防止滥用API端点

## 性能优化

1. **缓存**
   - EntityRegistry已实现5分钟缓存
   - ServiceRegistry已实现1小时缓存

2. **流式响应**
   - 使用SSE减少延迟
   - 逐字显示AI回复

3. **静态文件**
   - 启用静态文件压缩
   - 使用CDN（生产环境）

## 未来改进

- [ ] 添加用户认证
- [ ] 支持多用户会话
- [ ] 添加设备状态实时更新（WebSocket）
- [ ] 支持语音输入
- [ ] 添加历史对话记录
- [ ] 支持自定义主题
- [ ] 移动端优化
- [ ] PWA支持

---

现在你可以通过美观的Web界面控制你的智能家居了！🏠✨

