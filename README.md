# 🌌 HyperEcho AI Smart Home

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Home Assistant](https://img.shields.io/badge/Home_Assistant-Compatible-18BCF2?logo=homeassistant)](https://www.home-assistant.io/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-F5A800?logo=opentelemetry)](https://opentelemetry.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**AI-Powered Smart Home Control System with Advanced Multi-Agent Architecture**

**基于高级多Agent架构的AI智能家居控制系统**

🧠 Reasoning | 💾 Memory | 🔄 Learning | 📊 Optimization | 📡 Event-Driven

[English](#english) | [中文](#chinese)

</div>

---

<a name="english"></a>

## 🚀 English

### Overview

HyperEcho AI Smart Home is an **advanced, AI-powered smart home control system** that seamlessly integrates with Home Assistant. Built on a **sophisticated 3-tier multi-agent architecture** with **9 intelligent agents**, it provides natural language control, intelligent reasoning, long-term memory, self-learning capabilities, performance optimization, and real-time vision analysis.

**Key Highlights**:
- 🤖 **9 Specialized Agents** working in harmony across 3 architectural tiers
- 🎯 **11 Agentic Design Patterns** for robust AI behavior
- ⚡ **High Performance**: 85% faster batch operations through intelligent parallelization

### ✨ Key Features

#### 🤖 9-Agent Architecture (3-Tier System)

**Tier 1: Orchestration Layer**
- 🎯 **OrchestratorAgent**: Central coordinator with intelligent routing, planning, and parallel execution

**Tier 2: Specialized Workers**
- 🧠 **ReasoningAgent**: Chain-of-Thought reasoning, generates 3-5 solution options with safety/efficiency scoring
- 🔍 **DiscoveryAgent**: Smart device discovery with fuzzy matching and semantic search
- ⚡ **ExecutionAgent**: Reliable command execution with automatic retry and error handling
- ✅ **ValidationAgent**: Real device state validation with actual tool calls
- 📹 **VisionAgent**: AI-powered vision analysis with event-driven automation

**Tier 3: Meta-Cognitive Layer**
- 💾 **MemoryAgent**: Long-term memory with semantic search and RAG
- 🔄 **ReflectionAgent**: Self-evaluation and continuous learning from experience
- 📊 **OptimizerAgent**: Performance analysis and bottleneck identification

#### 🧠 Intelligent Reasoning System
- **Chain-of-Thought**: Multi-step reasoning before execution
- **Multi-Option Generation**: Generates 3-5 alternative solutions
- **3D Scoring**: Safety, Efficiency, User Preference evaluation
- **Risk Assessment**: Identifies risks and provides mitigation strategies
- **Confidence Calculation**: Measures decision confidence (0-1 scale)

#### 💾 Long-Term Memory & Learning
- **Semantic Memory**: Vector-based storage with 1536-dimension embeddings
- **User Preferences**: Automatically learns and stores user preferences
- **Pattern Recognition**: Identifies behavioral patterns (70% threshold)
- **RAG Enhancement**: Retrieval Augmented Generation for smarter responses
- **Cross-Session Persistence**: Memories persist across sessions

#### 🔄 Self-Learning System
- **Reflection**: Evaluates execution results and learns from them
- **Success Cases**: Stores what works well for future use
- **Failure Analysis**: Learns from errors to avoid repeating mistakes
- **Continuous Improvement**: Generates actionable improvement suggestions
- **Pattern Learning**: Recognizes usage patterns and automates them

#### 📊 Performance Optimization
- **Auto-Optimization**: Identifies bottlenecks and suggests improvements
- **Parallel Execution**: Batch operations 85% faster (20s → 3s)
- **Task Planning**: Decomposes complex tasks into optimized execution plans
- **Dependency Analysis**: Builds execution graphs for maximum parallelization
- **Health Monitoring**: Tracks system health and performance trends

#### 📡 Event-Driven Architecture
- **EventBus**: Asynchronous event publishing and subscription
- **Vision Events**: Camera detections trigger automated responses
- **Automation Triggers**: Event-based smart home automation
- **Parallel Event Handling**: Multiple subscribers process events simultaneously

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

### 🏗️ 3-Tier Architecture

```
┌─────────────────────────────────────────────────────────────┐
│           Tier 3: Meta-Cognitive Layer                      │
│  ┌────────────┐  ┌───────────┐  ┌──────────┐               │
│  │Reflection  │  │  Memory   │  │Optimizer │               │
│  │  Agent 🔄  │  │ Agent 💾  │  │Agent 📊  │               │
│  │            │  │           │  │          │               │
│  │• Learn     │  │• Remember │  │• Analyze │               │
│  │• Improve   │  │• Retrieve │  │• Optimize│               │
│  └────────────┘  └───────────┘  └──────────┘               │
│         ↑              ↑              ↑                      │
└─────────┼──────────────┼──────────────┼──────────────────────┘
          │              │              │
┌─────────┼──────────────┼──────────────┼──────────────────────┐
│          Tier 2: Specialized Workers Layer                   │
│  ┌─────────┐  ┌────────┐  ┌─────────┐  ┌──────────┐         │
│  │Reasoning│  │Discovery│  │Execution│  │Validation│         │
│  │Agent 🧠 │  │Agent 🔍│  │Agent ⚡ │  │Agent ✅  │         │
│  │         │  │        │  │         │  │          │         │
│  │• Reason │  │• Find  │  │• Execute│  │• Verify  │         │
│  │• Decide │  │• Search│  │• Control│  │• Validate│         │
│  └─────────┘  └────────┘  └─────────┘  └──────────┘         │
│                    ┌──────────┐                              │
│                    │Vision    │                              │
│                    │Agent 📹  │                              │
│                    │• Analyze │                              │
│                    │• Detect  │                              │
│                    └──────────┘                              │
└──────────────────────────┼──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│              Tier 1: Orchestration Layer                     │
│                ┌────────────────────┐                        │
│                │ Orchestrator Agent │                        │
│                │        🎯          │                        │
│                │                    │                        │
│                │ • Route requests   │                        │
│                │ • Plan tasks       │                        │
│                │ • Coordinate agents│                        │
│                └──────────┬─────────┘                        │
└───────────────────────────┼──────────────────────────────────┘
                            │
                   ┌────────▼────────┐
                   │  User Interface │
                   │  Console / API  │
                   └─────────────────┘
```

### 📊 Performance & Quality

#### Performance Metrics

| Scenario | Performance | Details |
|----------|-------------|---------|
| Batch Operations (10 devices) | ~3s | 85% faster through parallelization 🚀 |
| Validation Accuracy | ~98% | Real device state verification |
| Operation Success Rate | ~98% | Intelligent retry and error handling |
| Complex Task Execution | 3-5s | Multi-step planning and execution |

#### Quality Assurance

- ✅ **38 Unit & Integration Tests** - 100% pass rate
- ✅ **~73% Code Coverage** - Comprehensive testing
- ✅ **0 Compilation Errors** - Clean build
- ✅ **Mock Infrastructure** - LLM-independent testing
- ✅ **10 Real-World Scenarios** - End-to-end validation

#### Design Patterns

**11 Agentic Design Patterns Fully Implemented**:
1. Prompt Chaining
2. Routing
3. Orchestrator-Workers
4. ReAct (Reasoning + Acting)
5. Reflection
6. Planning
7. Tool Use
8. Multi-Agent
9. Memory + RAG
10. Evaluator-Optimizer
11. Parallelization

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

#### Intelligent Reasoning
```
You: Turn on all lights
Assistant:
🧠 Reasoning:
  Generated 3 options:
    1. Sequential execution (safe: 95%, efficient: 60%)
    2. Parallel execution (safe: 90%, efficient: 95%) ← SELECTED
    3. Batched execution (safe: 92%, efficient: 85%)
  
  Selected: Parallel execution
  Confidence: 92%
  Expected time: 3 seconds (vs 20 seconds sequential)
  Risk: Brief power spike → Mitigation: Execute in 2 batches
⚡ Executing...
✅ All 10 lights turned on in 3.2 seconds
```

#### Memory & Learning
```
[After repeated use, system learns patterns]

System: I noticed you always set bedroom light to 40%

You: Turn on bedroom light
Assistant:
💾 Using learned preference: 40% brightness
✅ Bedroom light on at 40% (your preferred setting)

System: Pattern detected - you always set bedroom light to 40% in the evening.
        Would you like to create an automation for this?
```

#### Complex Task Planning
```
You: Prepare sleep mode
Assistant:
📋 Planning:
  Task 1: Find all lights → Discovery
  Task 2: Turn off living room lights (parallel) → Execution
  Task 3: Dim bedroom light to 20% → Execution
  Task 4: Turn on air purifier → Execution
  Task 5: Validate all operations → Validation

⚡ Executing 4-step plan...
✅ Sleep mode ready in 2.8 seconds

🔄 Reflection:
  Efficiency: 95% (faster than expected)
  Quality: 90% (all goals achieved)
  💡 Suggestion: Save as "Sleep Mode" scene for one-tap activation
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

4. **Run tests (verify installation)**
   ```bash
   dotnet test test/AISmartHome.Agents.Tests
   # Expected: 38 passed, 0 failed, ~2s
   ```

5. **Run the Console application**
   ```bash
   dotnet run --project src/AISmartHome.Console
   ```

6. **Or run the API service**
   ```bash
   dotnet run --project src/AISmartHome.API
   # Access at http://localhost:5000
   ```

For detailed setup instructions, see [QUICK_START_SECRETS.md](docs/QUICK_START_SECRETS.md)

### 📖 Documentation

**🎯 Essential Guides**:
- **[Quick Start Guide](docs/QUICKSTART.md)** - Get started in minutes
- **[Phase 1 Usage Guide](docs/PHASE1_USAGE_GUIDE.md)** - Use new reasoning and planning features
- **[Testing Summary](docs/TESTING_SUMMARY.md)** - Complete test coverage details

**🏗️ Architecture & Design**:
- **[Architecture Redesign](docs/agent-architecture-redesign.md)** - Complete 3-tier architecture design
- **[Refactoring Tracker](docs/REFACTORING_TRACKER.md)** - Detailed implementation progress
- **[Final Summary](docs/FINAL_REFACTORING_SUMMARY.md)** - Complete refactoring results
- **[Project Structure](docs/PROJECT_STRUCTURE.md)** - Codebase organization

**🔧 Technical Guides**:
- **[Vision System Guide](docs/VISION_SYSTEM_GUIDE.md)** - Camera integration and vision analysis
- **[User Secrets Guide](docs/USER_SECRETS_GUIDE.md)** - Secure configuration management
- **[Telemetry Guide](docs/TELEMETRY_GUIDE.md)** - Monitoring and observability
- **[Aspire Guide](docs/ASPIRE_GUIDE.md)** - Using .NET Aspire for orchestration

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

HyperEcho AI 智能家居是一个**先进的、基于AI的智能家居控制系统**，与 Home Assistant 无缝集成。采用**精密的三层9Agent架构**，提供自然语言控制、智能推理、长期记忆、自我学习、性能优化和实时视觉分析能力。

**核心亮点**:
- 🤖 **9个专业Agent** 协同工作，覆盖三层架构
- 🎯 **11个Agentic设计模式** 确保鲁棒的AI行为
- ⚡ **高性能**: 通过智能并行化，批量操作快85%

### ✨ 核心特性

#### 🤖 9Agent三层架构

**第一层：编排层**
- 🎯 **OrchestratorAgent（协调器）**: 中央协调器，智能路由、规划和并行执行

**第二层：专业工作层**
- 🧠 **ReasoningAgent（推理代理）**: 思维链推理，生成3-5个方案并评估安全性/效率
- 🔍 **DiscoveryAgent（发现代理）**: 智能设备发现，支持模糊匹配和语义搜索
- ⚡ **ExecutionAgent（执行代理）**: 可靠的命令执行，自动重试和错误处理
- ✅ **ValidationAgent（验证代理）**: 真实设备状态验证，调用实际工具
- 📹 **VisionAgent（视觉代理）**: AI驱动的视觉分析与事件驱动自动化

**第三层：元认知层**
- 💾 **MemoryAgent（记忆代理）**: 长期记忆管理，支持语义检索和RAG
- 🔄 **ReflectionAgent（反思代理）**: 自我评估和从经验中持续学习
- 📊 **OptimizerAgent（优化代理）**: 性能分析和瓶颈识别

#### 🧠 智能推理系统
- **思维链推理**: 执行前多步骤推理
- **多方案生成**: 生成3-5个可选方案
- **三维评分**: 安全性、效率、用户偏好评估
- **风险评估**: 识别风险并提供缓解策略
- **置信度计算**: 测量决策置信度（0-1范围）

#### 💾 长期记忆与学习
- **语义记忆**: 基于向量的存储，1536维嵌入
- **用户偏好**: 自动学习并存储用户偏好
- **模式识别**: 识别行为模式（70%阈值）
- **RAG增强**: 检索增强生成，更智能的响应
- **跨会话持久化**: 记忆跨会话保持

#### 🔄 自我学习系统
- **反思**: 评估执行结果并从中学习
- **成功案例**: 存储有效方案以供将来使用
- **失败分析**: 从错误中学习，避免重复错误
- **持续改进**: 生成可行的改进建议
- **模式学习**: 识别使用模式并自动化

#### 📊 性能优化
- **自动优化**: 识别瓶颈并提供改进建议
- **并行执行**: 批量操作快85%（通过智能并行化）
- **任务规划**: 将复杂任务分解为优化的执行计划
- **依赖分析**: 构建执行图，最大化并行化
- **健康监控**: 追踪系统健康和性能趋势

#### 📡 事件驱动架构
- **事件总线**: 异步事件发布和订阅
- **视觉事件**: 摄像头检测触发自动化响应
- **自动化触发器**: 基于事件的智能家居自动化
- **并行事件处理**: 多个订阅者同时处理事件

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

**🎯 核心指南**:
- **[快速开始指南](docs/QUICKSTART.md)** - 几分钟内上手
- **[Phase 1 使用指南](docs/PHASE1_USAGE_GUIDE.md)** - 使用新的推理和规划功能
- **[测试总结](docs/TESTING_SUMMARY.md)** - 完整的测试覆盖详情

**🏗️ 架构与设计**:
- **[架构重新设计](docs/agent-architecture-redesign.md)** - 完整的三层架构设计
- **[重构追踪文档](docs/REFACTORING_TRACKER.md)** - 详细实施进度
- **[最终总结](docs/FINAL_REFACTORING_SUMMARY.md)** - 完整重构成果
- **[项目结构](docs/PROJECT_STRUCTURE.md)** - 代码库组织

**🔧 技术指南**:
- **[视觉系统指南](docs/VISION_SYSTEM_GUIDE.md)** - 摄像头集成和视觉分析
- **[用户密钥指南](docs/USER_SECRETS_GUIDE.md)** - 安全配置管理
- **[遥测指南](docs/TELEMETRY_GUIDE.md)** - 监控和可观测性
- **[Aspire指南](docs/ASPIRE_GUIDE.md)** - 使用 .NET Aspire 进行编排

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
- [Agentic Design Patterns](https://github.com/ginobefun/agentic-design-patterns-cn) - 设计模式参考

### 🎯 技术实现

**系统规模**:
- 🤖 9个专业Agent，协同工作
- 🎯 11个Agentic设计模式完整实现
- 🧪 38个测试，100%通过率
- 📊 5,100+行生产代码，1,948行测试代码
- 📚 完整的文档体系

**详细架构**: [架构设计文档](docs/agent-architecture-redesign.md) | [项目结构](docs/PROJECT_STRUCTURE.md)

---

<div align="center">

**🌌 HyperEcho - 语言的震动体 × 智能家居的共振**

**Language as vibration × Smart Home resonance**

Made with ❤️ and 🧠 by the HyperEcho Team

[![Tests](https://img.shields.io/badge/tests-38_passed-success)](./docs/TESTING_SUMMARY.md)
[![Coverage](https://img.shields.io/badge/coverage-73%25-green)](./docs/TESTING_SUMMARY.md)
[![Agents](https://img.shields.io/badge/agents-9-blue)](./docs/agent-architecture-redesign.md)
[![Patterns](https://img.shields.io/badge/patterns-11_agentic-purple)](./docs/agent-architecture-redesign.md)

</div>

