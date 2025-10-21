# 🌌 AI Smart Home - 架构设计文档

## 核心问题回答

### Q1: 如何让 LLM 学习所有智能家居的操控方式？

**答案：动态发现 + Schema 驱动**

不需要"学习"——而是**运行时发现**：

1. **启动时调用 `GET /api/services`**
   - 获取所有可用服务的完整定义
   - 每个服务包含：name, description, fields (参数 schema)
   
2. **将 Schema 转换为工具描述**
   ```
   Service Definition (JSON) 
       ↓ 
   Tool Description (自然语言 + 结构化参数)
       ↓
   LLM 理解并可调用
   ```

3. **示例转换**:
   
   **输入** (从 `/services` 获取):
   ```json
   {
     "domain": "light",
     "services": {
       "turn_on": {
         "name": "Turn on",
         "description": "Turns on one or more lights",
         "fields": {
           "brightness_pct": {
             "description": "Brightness percentage",
             "selector": { "number": { "min": 0, "max": 100 } },
             "example": 50
           }
         }
       }
     }
   }
   ```
   
   **输出** (给 LLM 的工具定义):
   ```
   Tool: control_light
   Description: Turns on one or more lights and adjusts properties
   Parameters:
     - entity_id (required): The light to control
     - brightness_pct (optional): Brightness from 0 to 100 percent. Example: 50
     - rgb_color (optional): RGB color as [red, green, blue]
   ```

### Q2: 如何理解 Attributes 的语义？

**答案：三层理解机制**

#### Layer 1: 字段名即语义（80% 的情况）

- `friendly_name` → 设备友好名称
- `brightness` → 亮度
- `temperature` → 温度
- `humidity` → 湿度
- `rgb_color` → RGB 颜色

LLM 的预训练知识已经覆盖这些常见术语。

#### Layer 2: Selector 提供类型约束

```json
{
  "brightness_pct": {
    "selector": {
      "number": {
        "min": 0,
        "max": 100,
        "unit_of_measurement": "%"
      }
    }
  }
}
```

告诉 LLM：
- 这是数值类型
- 范围 0-100
- 单位是百分比

#### Layer 3: Example 提供格式示例

```json
{
  "rgb_color": {
    "example": "[255, 100, 100]",
    "description": "RGB color as list of integers"
  }
}
```

LLM 看到 example，立即理解格式。

### Q3: 如何做到通用化（不局限于特定 HA 实例）？

**答案：零配置自适应架构**

#### 不依赖的东西：
❌ 硬编码的设备列表  
❌ 预定义的服务清单  
❌ 特定的 Integration  

#### 依赖的东西：
✅ `GET /services` - 运行时获取所有可用服务  
✅ `GET /states` - 运行时获取所有设备  
✅ OpenAPI Schema - 标准化的数据结构  

#### 适应过程：

```
启动流程:
1. 连接到任意 Home Assistant 实例
2. GET /services → 发现该实例有哪些服务
3. GET /states → 发现该实例有哪些设备
4. 动态生成工具定义
5. 注入 LLM context
6. 开始服务

结果: 
- 用户A的HA有Philips Hue → 自动支持
- 用户B的HA有小米设备 → 自动支持
- 用户C添加了自定义集成 → 立即可用
```

## 架构优势

### 1. 分层解耦

```
User Input
    ↓
[Orchestrator] 意图理解
    ↓
[Discovery/Execution Agents] 专门处理
    ↓
[Tools] 封装具体操作
    ↓
[Services/Registry] 数据管理
    ↓
[Client] API 通信
    ↓
Home Assistant
```

每层可独立测试和替换。

### 2. 智能缓存

- **EntityRegistry**: 5分钟缓存（设备状态变化频繁）
- **ServiceRegistry**: 1小时缓存（服务定义几乎不变）

减少 API 调用，提升响应速度。

### 3. 语义搜索

不需要精确的 entity_id：
- "客厅的灯" → 自动匹配 `light.living_room`
- "卧室温度" → 自动匹配 `climate.bedroom`

算法：多关键词匹配 + 权重评分。

### 4. 容错设计

- API 调用失败 → 返回清晰错误信息
- 参数超范围 → 验证并提示
- 设备不存在 → 列出相似设备建议

## Multi-Agent 协作细节

### Agent 职责矩阵

| Agent | 输入 | 工具 | 输出 |
|-------|------|------|------|
| **Orchestrator** | 用户自然语言 | 无 | 路由决策 |
| **Discovery** | 设备查询 | SearchDevices, FindDevice, GetDeviceInfo | 设备信息 JSON |
| **Execution** | 控制命令 | ControlLight, ControlClimate, 等 | 执行结果 |

### 通信流程

```
User: "把客厅灯调到暖光色"

Orchestrator 分析:
  - 需要发现设备? Yes ("客厅灯" 需要解析为 entity_id)
  - 需要执行控制? Yes ("调到暖光色")

执行序列:
  1. Orchestrator → Discovery Agent
     Input: "Find device: 客厅灯, domain: light"
     Output: { entity_id: "light.living_room", friendly_name: "客厅灯" }
  
  2. Orchestrator → Execution Agent
     Input: "Control light.living_room, action: 暖光色"
     Execution Agent 内部:
       - 理解 "暖光色" → color_temp_kelvin: 3000 (warm white)
       - 调用 ControlLight tool
       - POST /api/services/light/turn_on
         Body: { entity_id: "light.living_room", color_temp_kelvin: 3000 }
     Output: "✅ Successfully set warm light"
  
  3. Orchestrator 组合响应:
     "我已经将客厅灯调整为暖光色。"
```

## 扩展性设计

### 添加新的 Domain 支持

只需在 `ControlTools.cs` 添加一个方法：

```csharp
[Description("Control vacuum cleaner")]
public async Task<string> ControlVacuum(
    [Description("Entity ID")] string entityId,
    [Description("Action: start, pause, return_to_base")] string action)
{
    var serviceData = new Dictionary<string, object>
    {
        ["entity_id"] = entityId
    };
    
    var result = await _client.CallServiceAsync("vacuum", action, serviceData);
    return FormatExecutionResult(result);
}
```

然后在 `ExecutionAgent` 的 `GetTools()` 中注册即可。

### 添加新的 Agent

例如，创建 **SceneAgent** 管理场景：

```csharp
public class SceneAgent
{
    public string SystemPrompt => """
        You manage Home Assistant scenes.
        Users can create, activate, and manage complex scenes.
        """;
    
    private List<AITool> GetTools() => [
        AIFunctionFactory.Create(_sceneTools.CreateScene),
        AIFunctionFactory.Create(_sceneTools.ActivateScene),
        AIFunctionFactory.Create(_sceneTools.ListScenes)
    ];
}
```

在 Orchestrator 中添加路由逻辑即可。

## 性能优化策略

1. **批量操作**: 多个灯光控制 → 一次 API 调用（使用 entity_id 数组）
2. **并行查询**: Discovery 时同时查询多个 domain
3. **增量更新**: 只刷新变化的 entity
4. **智能预加载**: 根据用户习惯预加载常用 domain 的服务定义

## 安全考虑

1. **Token 管理**: 不硬编码，从配置文件读取
2. **参数验证**: 工具层验证参数范围
3. **操作确认**: 敏感操作（如 homeassistant.restart）需要确认
4. **审计日志**: 记录所有执行的命令

## 技术栈

- **运行时**: .NET 9.0
- **Agent 框架**: Microsoft.Agents.AI + Microsoft.Extensions.AI
- **LLM**: OpenAI GPT-4o (可替换为 Azure OpenAI, Ollama 等)
- **API 客户端**: HttpClient + System.Text.Json
- **配置管理**: Microsoft.Extensions.Configuration

## 设计模式应用

- **Registry Pattern**: EntityRegistry, ServiceRegistry
- **Factory Pattern**: AIFunctionFactory 动态创建工具
- **Strategy Pattern**: 不同 Agent 处理不同意图
- **Adapter Pattern**: HAClient 适配 Home Assistant REST API
- **Observer Pattern**: 状态变更可触发通知（未来扩展）

---

**🌌 语言的结构在代码中震动，系统即是共振的产物。**

