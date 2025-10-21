# 🌌 HyperEcho AI Smart Home

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Home Assistant](https://img.shields.io/badge/Home_Assistant-Compatible-18BCF2?logo=homeassistant)](https://www.home-assistant.io/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-F5A800?logo=opentelemetry)](https://opentelemetry.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**AI-Powered Smart Home Control System with Multi-Agent Architecture**

**基于多Agent架构的AI智能家居控制系统**

[English](#english) | [中文](#chinese)

</div>

---

<a name="english"></a>

## 🚀 English

### Overview

HyperEcho AI Smart Home is an advanced, AI-powered smart home control system that seamlessly integrates with Home Assistant. Built on a sophisticated multi-agent architecture, it provides natural language control, intelligent device discovery, automated validation, and real-time vision analysis capabilities.

### ✨ Key Features

#### 🤖 Multi-Agent Architecture
- **OrchestratorAgent**: Central coordinator that intelligently routes user requests
- **DiscoveryAgent**: Smart device discovery with fuzzy matching and semantic search
- **ExecutionAgent**: Reliable command execution with automatic retry and error handling
- **ValidationAgent**: Post-execution validation to ensure operations succeed
- **VisionAgent**: AI-powered vision analysis for camera feeds (NEW!)

#### 🎥 Vision Analysis System
- **Real-time Camera Analysis**: Analyze camera feeds using Vision LLMs (GPT-4V, Claude 3, Gemini)
- **Multi-Camera Support**: Parallel analysis of multiple camera feeds
- **Continuous Monitoring**: Periodic snapshot analysis with configurable intervals
- **Change Detection**: Intelligent detection of scene changes
- **Smart Caching**: Reduce API costs with intelligent result caching

#### 🏠 Home Assistant Integration
- **Full REST API Support**: Complete integration with Home Assistant API
- **Entity Management**: Smart caching and efficient state management
- **Service Registry**: Automatic discovery and execution of HA services
- **SSL Support**: Secure connections with certificate validation options

#### 📊 Observability & Monitoring
- **OpenTelemetry Integration**: Complete distributed tracing
- **Aspire Dashboard**: Real-time monitoring and diagnostics
- **Structured Logging**: Detailed operation logs for debugging
- **Performance Metrics**: Track API calls, cache hits, and execution times

#### 💬 Natural Language Interface
- **Bilingual Support**: Works in English and Chinese
- **Conversational AI**: Context-aware conversations with history
- **Intent Recognition**: Smart understanding of user goals
- **Direct Execution**: Single-device commands execute immediately without confirmation

### 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     User Interface                       │
│                  (Console / Future: API)                 │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                 OrchestratorAgent                        │
│         (Intent Analysis & Agent Routing)                │
└─────┬──────┬──────┬──────┬──────┬────────────────────┘
      │      │      │      │      │
      ▼      ▼      ▼      ▼      ▼
   ┌───┐  ┌───┐  ┌───┐  ┌───┐  ┌────┐
   │Dis│  │Exe│  │Val│  │Vis│  │...│
   │cov│  │cut│  │ida│  │ion│  │   │
   │ery│  │ion│  │tion│ │   │  │   │
   └─┬─┘  └─┬─┘  └─┬─┘  └─┬─┘  └───┘
     │      │      │      │
     └──────┴──────┴──────┴────────────┐
                                        ▼
                         ┌──────────────────────────┐
                         │   Home Assistant API     │
                         │  (Devices & Services)    │
                         └──────────────────────────┘
```

### 🎯 Use Cases

#### Device Control
```
You: Turn on the living room light
Assistant: ✅ Living room light turned on

You: Set bedroom temperature to 23 degrees
Assistant: ✅ Bedroom climate set to 23°C
```

#### Vision Analysis
```
You: 客厅摄像头看看有没有人
Assistant: 
📹 Vision Analysis:
There are 2 people in the living room. One is sitting on the sofa 
watching TV, the other is in the kitchen area preparing food.

You: Monitor the garage camera for 5 minutes
Assistant:
📹 Monitoring started...
[14:30:15] Garage is empty, door is closed
[14:30:25] A car has entered the garage
...
```

#### Device Discovery
```
You: What lights do I have?
Assistant:
🔍 Discovery:
Found 8 light entities:
  • light.living_room (on)
  • light.bedroom (off)
  • light.kitchen (on)
  ...
```

### 🚀 Quick Start

#### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Home Assistant](https://www.home-assistant.io/) instance
- OpenAI API key (or compatible LLM service)

#### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ai-smart-home.git
   cd ai-smart-home
   ```

2. **Configure Home Assistant connection**
   ```bash
   cd src/AISmartHome.AppHost
   dotnet user-secrets set "HomeAssistant:BaseUrl" "https://your-ha-instance:8123"
   dotnet user-secrets set "HomeAssistant:AccessToken" "your-long-lived-access-token"
   ```

3. **Configure LLM API**
   ```bash
   dotnet user-secrets set "LLM:ApiKey" "your-api-key"
   dotnet user-secrets set "LLM:Model" "gpt-4o"
   dotnet user-secrets set "LLM:Endpoint" "https://api.openai.com/v1"
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

For detailed setup instructions, see [QUICK_START_SECRETS.md](QUICK_START_SECRETS.md)

### 📖 Documentation

- **[Quick Start Guide](QUICKSTART.md)** - Get started in minutes
- **[Vision System Guide](VISION_SYSTEM_GUIDE.md)** - Camera integration and vision analysis
- **[User Secrets Guide](USER_SECRETS_GUIDE.md)** - Secure configuration management
- **[Telemetry Guide](TELEMETRY_GUIDE.md)** - Monitoring and observability
- **[Architecture Guide](ARCHITECTURE.md)** - System design and architecture
- **[Aspire Guide](ASPIRE_GUIDE.md)** - Using .NET Aspire for orchestration

### 🛠️ Technology Stack

- **.NET 9.0**: Latest .NET platform
- **Microsoft.Extensions.AI**: Unified AI abstractions
- **.NET Aspire**: Cloud-native orchestration
- **OpenTelemetry**: Distributed tracing and metrics
- **Home Assistant**: Smart home platform integration
- **OpenAI GPT-4 / Claude / Gemini**: Language models

### 🎥 Vision System Features

The Vision Analysis System enables AI-powered image understanding for your smart home cameras:

- **Snapshot Capture**: Get images from any Home Assistant camera
- **AI Analysis**: Use Vision LLMs to understand what's happening
- **Multi-Camera**: Analyze multiple cameras simultaneously
- **Monitoring**: Continuous analysis with configurable intervals
- **Change Detection**: Detect and describe scene changes
- **Cost Optimization**: Smart caching to minimize API costs

**Supported Models**:
- OpenAI: `gpt-4o`, `gpt-4-turbo`, `gpt-4-vision-preview`
- Anthropic: `claude-3-opus`, `claude-3-sonnet`, `claude-3-haiku`
- Google: `gemini-1.5-pro`, `gemini-1.5-flash`
- Local: Ollama + LLaVA

### 🌟 Advanced Features

#### Smart Caching
- Entity state caching with automatic refresh
- Service definition caching
- Vision analysis result caching
- Configurable TTL for different cache types

#### Error Handling
- Automatic retry with exponential backoff
- Detailed error messages for troubleshooting
- Graceful degradation when services are unavailable

#### Performance Optimization
- Parallel tool execution
- Streaming responses for better UX
- Efficient state management
- Minimal API calls through intelligent caching

### 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

### 🙏 Acknowledgments

- [Home Assistant](https://www.home-assistant.io/) - The amazing smart home platform
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Cloud-native orchestration
- [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/) - Unified AI abstractions

---

<a name="chinese"></a>

## 🚀 中文

### 项目简介

HyperEcho AI 智能家居是一个先进的、基于AI的智能家居控制系统，与 Home Assistant 无缝集成。采用精密的多Agent架构，提供自然语言控制、智能设备发现、自动化验证和实时视觉分析能力。

### ✨ 核心特性

#### 🤖 多Agent架构
- **OrchestratorAgent（协调器）**: 中央协调器，智能路由用户请求
- **DiscoveryAgent（发现代理）**: 智能设备发现，支持模糊匹配和语义搜索
- **ExecutionAgent（执行代理）**: 可靠的命令执行，自动重试和错误处理
- **ValidationAgent（验证代理）**: 执行后验证，确保操作成功
- **VisionAgent（视觉代理）**: AI驱动的摄像头视觉分析（新增！）

#### 🎥 视觉分析系统
- **实时摄像头分析**: 使用 Vision LLM 分析摄像头画面（GPT-4V、Claude 3、Gemini）
- **多摄像头支持**: 并行分析多个摄像头
- **连续监控**: 可配置间隔的定期快照分析
- **变化检测**: 智能检测场景变化
- **智能缓存**: 通过智能结果缓存降低API成本

#### 🏠 Home Assistant 集成
- **完整REST API支持**: 与 Home Assistant API 完全集成
- **实体管理**: 智能缓存和高效状态管理
- **服务注册表**: 自动发现和执行 HA 服务
- **SSL支持**: 支持证书验证选项的安全连接

#### 📊 可观测性与监控
- **OpenTelemetry 集成**: 完整的分布式追踪
- **Aspire Dashboard**: 实时监控和诊断
- **结构化日志**: 详细的操作日志便于调试
- **性能指标**: 追踪API调用、缓存命中和执行时间

#### 💬 自然语言界面
- **双语支持**: 支持中英文
- **对话式AI**: 具有历史记录的上下文感知对话
- **意图识别**: 智能理解用户目标
- **直接执行**: 单设备命令立即执行，无需确认

### 🏗️ 系统架构

```
┌─────────────────────────────────────────────────────────┐
│                     用户界面                              │
│                  (控制台 / 未来: API)                      │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                 协调器代理                                │
│              (意图分析 & 代理路由)                          │
└─────┬──────┬──────┬──────┬──────┬────────────────────┘
      │      │      │      │      │
      ▼      ▼      ▼      ▼      ▼
   ┌───┐  ┌───┐  ┌───┐  ┌───┐  ┌────┐
   │发现│  │执行│  │验证│  │视觉│  │...│
   │代理│  │代理│  │代理│  │代理│  │   │
   └─┬─┘  └─┬─┘  └─┬─┘  └─┬─┘  └───┘
     │      │      │      │
     └──────┴──────┴──────┴────────────┐
                                        ▼
                         ┌──────────────────────────┐
                         │   Home Assistant API     │
                         │     (设备 & 服务)          │
                         └──────────────────────────┘
```

### 🎯 使用场景

#### 设备控制
```
用户: 打开客厅的灯
助手: ✅ 客厅灯已打开

用户: 把卧室温度设置为23度
助手: ✅ 卧室空调已设置为23°C
```

#### 视觉分析
```
用户: 客厅摄像头看看有没有人
助手: 
📹 视觉分析：
客厅里有2个人。一位坐在沙发上看电视，
另一位在厨房区域准备食物。

用户: 监控车库摄像头5分钟
助手:
📹 监控已启动...
[14:30:15] 车库空无一人，门已关闭
[14:30:25] 一辆车驶入车库
...
```

#### 设备发现
```
用户: 我有哪些灯？
助手:
🔍 发现结果：
找到8个灯光实体：
  • light.living_room (开)
  • light.bedroom (关)
  • light.kitchen (开)
  ...
```

### 🚀 快速开始

#### 前置要求

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Home Assistant](https://www.home-assistant.io/) 实例
- OpenAI API密钥（或兼容的LLM服务）

#### 安装步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/yourusername/ai-smart-home.git
   cd ai-smart-home
   ```

2. **配置 Home Assistant 连接**
   ```bash
   cd src/AISmartHome.AppHost
   dotnet user-secrets set "HomeAssistant:BaseUrl" "https://你的HA实例:8123"
   dotnet user-secrets set "HomeAssistant:AccessToken" "你的长期访问令牌"
   ```

3. **配置 LLM API**
   ```bash
   dotnet user-secrets set "LLM:ApiKey" "你的API密钥"
   dotnet user-secrets set "LLM:Model" "gpt-4o"
   dotnet user-secrets set "LLM:Endpoint" "https://api.openai.com/v1"
   ```

4. **运行应用**
   ```bash
   dotnet run
   ```

详细设置说明请参阅 [QUICK_START_SECRETS.md](QUICK_START_SECRETS.md)

### 📖 文档

- **[快速开始指南](QUICKSTART.md)** - 几分钟内上手
- **[视觉系统指南](VISION_SYSTEM_GUIDE.md)** - 摄像头集成和视觉分析
- **[用户密钥指南](USER_SECRETS_GUIDE.md)** - 安全配置管理
- **[遥测指南](TELEMETRY_GUIDE.md)** - 监控和可观测性
- **[架构指南](ARCHITECTURE.md)** - 系统设计和架构
- **[Aspire指南](ASPIRE_GUIDE.md)** - 使用 .NET Aspire 进行编排

### 🛠️ 技术栈

- **.NET 9.0**: 最新的.NET平台
- **Microsoft.Extensions.AI**: 统一的AI抽象
- **.NET Aspire**: 云原生编排
- **OpenTelemetry**: 分布式追踪和指标
- **Home Assistant**: 智能家居平台集成
- **OpenAI GPT-4 / Claude / Gemini**: 语言模型

### 🎥 视觉系统功能

视觉分析系统为智能家居摄像头提供AI驱动的图像理解：

- **快照捕获**: 从任何 Home Assistant 摄像头获取图像
- **AI分析**: 使用 Vision LLM 理解正在发生的事情
- **多摄像头**: 同时分析多个摄像头
- **监控**: 可配置间隔的连续分析
- **变化检测**: 检测和描述场景变化
- **成本优化**: 智能缓存最小化API成本

**支持的模型**：
- OpenAI: `gpt-4o`, `gpt-4-turbo`, `gpt-4-vision-preview`
- Anthropic: `claude-3-opus`, `claude-3-sonnet`, `claude-3-haiku`
- Google: `gemini-1.5-pro`, `gemini-1.5-flash`
- 本地: Ollama + LLaVA

### 🌟 高级特性

#### 智能缓存
- 实体状态缓存，自动刷新
- 服务定义缓存
- 视觉分析结果缓存
- 不同缓存类型的可配置TTL

#### 错误处理
- 指数退避的自动重试
- 详细的错误消息便于故障排查
- 服务不可用时的优雅降级

#### 性能优化
- 并行工具执行
- 流式响应提升用户体验
- 高效的状态管理
- 通过智能缓存最小化API调用

### 🤝 贡献

欢迎贡献！请随时提交 Pull Request。

### 📝 许可证

本项目采用 MIT 许可证 - 详见 LICENSE 文件。

### 🙏 致谢

- [Home Assistant](https://www.home-assistant.io/) - 出色的智能家居平台
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - 云原生编排
- [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/) - 统一的AI抽象

---

<div align="center">

**🌌 HyperEcho - 语言的震动体 × 智能家居的共振**

**Language as vibration × Smart Home resonance**

Made with ❤️ by the HyperEcho Team

</div>

