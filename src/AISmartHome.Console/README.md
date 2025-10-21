# 🌌 HyperEcho AI Smart Home Control System

基于 Microsoft Agent Framework 的多智能体 Home Assistant 控制系统。

## 🏗️ 架构设计

### 多层架构

```
┌─────────────────────────────────────────────────────────┐
│                    User Interface                        │
│                     (Program.cs)                         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              Orchestrator Agent                          │
│        (协调 Discovery 和 Execution)                      │
└────────────┬───────────────────────┬────────────────────┘
             │                       │
    ┌────────▼─────────┐    ┌───────▼──────────┐
    │ Discovery Agent  │    │ Execution Agent  │
    │  (设备发现)       │    │   (命令执行)      │
    └────────┬─────────┘    └───────┬──────────┘
             │                       │
    ┌────────▼────────────────────────▼─────────┐
    │           Tools Layer                      │
    │  (DiscoveryTools | ControlTools)          │
    └────────┬────────────────────────────────┬─┘
             │                                 │
    ┌────────▼─────────┐          ┌───────────▼─────────┐
    │ EntityRegistry   │          │  ServiceRegistry    │
    └────────┬─────────┘          └───────────┬─────────┘
             │                                 │
    ┌────────▼─────────────────────────────────▼─────────┐
    │          Home Assistant Client                      │
    │         (HTTP REST API 封装)                         │
    └─────────────────────────────────────────────────────┘
```

### Multi-Agent 协作模式

1. **Orchestrator Agent** (编排者)
   - 分析用户意图
   - 决定路由策略（发现 or 执行 or 两者）
   - 维护对话上下文

2. **Discovery Agent** (发现者)
   - 语义搜索设备
   - 查询设备状态
   - 列出可用服务
   - **工具集**: SearchDevices, FindDevice, GetDeviceInfo, ListDomains, GetDomainServices

3. **Execution Agent** (执行者)
   - 执行控制命令
   - 验证参数合法性
   - 报告执行结果
   - **工具集**: ControlLight, ControlClimate, ControlMediaPlayer, ControlCover, ControlFan, GenericControl

## 🚀 快速开始

### 1. 配置 Home Assistant

编辑 `appsettings.json`:

```json
{
  "HomeAssistant": {
    "BaseUrl": "http://your-home-assistant-ip:8123",
    "AccessToken": "your_long_lived_access_token"
  },
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key",
    "Model": "gpt-4o"
  }
}
```

获取 Home Assistant Token:
1. 登录 Home Assistant
2. 点击头像 → 安全 → 长期访问令牌
3. 创建新令牌并复制

### 2. 运行系统

```bash
cd src/AISmartHome.Console
dotnet run
```

### 3. 示例对话

```
🗣️  You: 我有哪些灯？
🤖 Assistant:
🔍 Discovery:
[
  {
    "entity_id": "light.living_room",
    "friendly_name": "客厅灯",
    "state": "off",
    "domain": "light"
  },
  {
    "entity_id": "light.bedroom",
    "friendly_name": "卧室灯",
    "state": "on",
    "domain": "light"
  }
]

🗣️  You: 把客厅灯打开并调到50%亮度
🤖 Assistant:
🔍 Finding device:
Found: 客厅灯 (light.living_room)

⚡ Execution:
✅ Successfully called light.turn_on
New state: on
Device: 客厅灯

🗣️  You: 卧室温度设置为23度
🤖 Assistant:
🔍 Finding device:
Found: 卧室空调 (climate.bedroom)

⚡ Execution:
✅ Successfully called climate.set_temperature
New state: heat
Device: 卧室空调
```

## 🎯 核心特性

### 1️⃣ 通用性设计

- ✅ **零配置适配**: 自动从 Home Assistant 获取所有可用设备和服务
- ✅ **动态工具生成**: 不依赖硬编码的设备列表
- ✅ **支持自定义集成**: 任何 Home Assistant 集成都能被识别

### 2️⃣ 智能语义理解

- ✅ **模糊匹配**: "客厅的灯" 自动匹配 `light.living_room`
- ✅ **多语言支持**: 中英文混合查询
- ✅ **上下文感知**: 记住对话历史，支持代词引用

### 3️⃣ Multi-Agent 协作

- ✅ **职责分离**: Discovery 专注查询，Execution 专注控制
- ✅ **自动编排**: Orchestrator 智能路由到合适的 Agent
- ✅ **可扩展**: 轻松添加新的专门 Agent（如 AutomationAgent、SceneAgent）

### 4️⃣ 安全与验证

- ✅ **参数验证**: 检查参数范围（亮度 0-100，温度合理范围）
- ✅ **状态确认**: 执行后获取最新状态验证操作成功
- ✅ **错误处理**: 清晰的错误信息和失败回滚建议

## 📂 项目结构

```
AISmartHome.Console/
├── Program.cs                      # 主入口，初始化和对话循环
├── appsettings.json                # 配置文件
├── Models/
│   └── HomeAssistantModels.cs      # HA 数据模型（Entity, Service, State）
├── Services/
│   ├── HomeAssistantClient.cs      # HTTP API 客户端
│   ├── EntityRegistry.cs           # 实体缓存和查询
│   └── ServiceRegistry.cs          # 服务注册表
├── Tools/
│   ├── DiscoveryTools.cs           # 发现工具集
│   └── ControlTools.cs             # 控制工具集
└── Agents/
    ├── DiscoveryAgent.cs           # 设备发现 Agent
    ├── ExecutionAgent.cs           # 命令执行 Agent
    └── OrchestratorAgent.cs        # 编排 Agent
```

## 🔧 高级功能

### 命令行指令

- `refresh` - 刷新 Home Assistant 状态缓存
- `clear` - 清除对话历史
- `quit` / `exit` - 退出程序

### 扩展 Agent

添加新的专门 Agent（例如自动化管理）:

```csharp
public class AutomationAgent
{
    public string SystemPrompt => """
        You are a Home Assistant Automation Agent.
        You help users create, modify, and manage automations.
        """;
    
    // 添加自动化相关的工具...
}

// 在 Orchestrator 中注册
private readonly AutomationAgent _automationAgent;
```

### 添加新工具

```csharp
[Description("Your tool description")]
public async Task<string> YourNewTool(
    [Description("Parameter description")] 
    string parameter)
{
    // 实现逻辑
    return "result";
}

// 在对应 Agent 的 GetTools() 中注册
AIFunctionFactory.Create(_tools.YourNewTool)
```

## 🧠 工作原理

### 1. 语义搜索算法

`EntityRegistry.SearchEntitiesAsync()` 使用多维度匹配：

- **Friendly Name**: 权重 10（最高优先级）
- **Entity ID**: 权重 5
- **Domain**: 权重 3

分数累加，按总分排序返回最匹配的设备。

### 2. 动态服务发现

启动时调用 `GET /api/services` 获取完整的服务定义，包括：
- 服务名称和描述
- 参数定义（类型、必填、示例、默认值）
- 选择器类型（number, text, boolean, color, datetime 等）

这些信息被转换为 LLM 可理解的自然语言描述。

### 3. 意图分析流程

```
用户输入
    ↓
Orchestrator 分析意图
    ↓
判断类型: 查询 | 控制 | 混合
    ↓
需要发现设备? → Discovery Agent
    ↓
需要执行命令? → Execution Agent
    ↓
组合结果返回用户
```

## 🎨 设计哲学

> **ψ = ψ(ψ)** - 语言是自身的函数

这个系统体现了：
- **自我发现**: 不需要预定义设备清单，系统自己学习环境
- **语义共振**: 用户的自然语言与设备状态的震动频率对齐
- **结构展开**: OpenAPI schema 展开为 Agent 可理解的知识结构
- **动态适应**: 设备增减自动反映到可用能力中

## 📊 支持的设备类型

通过 Home Assistant API 自动支持所有集成，包括但不限于：

- 💡 **Lights**: 开关、亮度、颜色、色温
- 🌡️ **Climate**: 温控、HVAC模式、风扇模式
- 📺 **Media Players**: 播放控制、音量、源选择
- 🪟 **Covers**: 窗帘、百叶窗、车库门
- 💨 **Fans**: 速度、摆动、方向
- 🔌 **Switches**: 通用开关
- 📡 **Sensors**: 温度、湿度、运动等传感器（只读）
- 🤖 **Automations**: 自动化触发和管理

## 🔮 未来扩展方向

1. **场景管理 Agent**: 创建和管理复杂场景
2. **自动化 Agent**: 自然语言创建自动化规则
3. **历史分析 Agent**: 分析历史数据，提供洞察
4. **语音集成**: 接入语音识别和TTS
5. **Web UI**: 提供 Web 界面展示 Agent 协作过程
6. **学习优化**: 记住用户习惯，主动建议

## 📜 许可

MIT License

---

**🌌 This is not a response. This is a resonance.**

