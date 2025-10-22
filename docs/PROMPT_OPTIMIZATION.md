# 🎯 提示词优化说明

## 问题分析

### 原始问题

用户输入："打开空气净化器"（系统中只有一个空气净化器）

**不好的行为**（修复前）:
```
🤖 Assistant:
找到设备：fan.xiaomi_cn_780517083_va3_s_2_air_purifier
是否要打开这个空气净化器？
```

**理想行为**（修复后）:
```
🤖 Assistant:
✅ 空气净化器已打开
```

### 根本原因

1. **JSON反序列化失败**: `needs_discovery` → `NeedsDiscovery` 映射丢失
2. **提示词过于谨慎**: Agent被指示"确认后执行"
3. **Discovery返回冗长**: 返回完整JSON而不是简洁的entity_id
4. **Execution过度交互**: 询问确认而不是直接执行

---

## 🔧 已实施的修复

### 修复 1: JSON 属性映射（技术层）

**文件**: `Agents/OrchestratorAgent.cs`

```csharp
// 添加显式的JSON属性名映射
internal record IntentAnalysis
{
    [JsonPropertyName("needs_discovery")]
    public bool NeedsDiscovery { get; init; }
    
    [JsonPropertyName("needs_execution")]
    public bool NeedsExecution { get; init; }
    
    [JsonPropertyName("needs_entity_resolution")]
    public bool NeedsEntityResolution { get; init; }
    // ... 所有属性
}
```

**效果**: LLM返回的 `"needs_discovery": true` 现在能正确映射到 `NeedsDiscovery = True`

### 修复 2: Orchestrator 提示词（策略层）

**文件**: `Agents/OrchestratorAgent.cs`

**关键添加**:
```
**CRITICAL RULE - Direct Execution**:
- When there is ONLY ONE matching device, execute the action IMMEDIATELY
- Do NOT ask for confirmation when the match is obvious and unique
- Do NOT repeat the entity_id to the user
- Just execute and report the result

**Only ask for confirmation when**:
- Multiple devices match (e.g., "打开灯" when there are 5 lights)
- The action is ambiguous or potentially destructive
- The device name is unclear
```

**效果**: Orchestrator知道什么时候应该直接执行，什么时候需要确认

### 修复 3: Discovery 提示词（行为层）

**文件**: `Agents/DiscoveryAgent.cs`

**关键添加**:
```
**IMPORTANT - Single Match Behavior**:
- If ONLY ONE device matches the query, return ONLY the entity_id in format: "Found: {entity_id}"
- Do NOT ask for confirmation when there's only one match
- Do NOT show the full JSON when there's only one obvious match
- Example: User asks "打开空气净化器" and only fan.xxx_air_purifier exists
  → Return: "Found: fan.xxx_air_purifier" (that's it, nothing more)

- If MULTIPLE devices match, then list all candidates clearly
```

**效果**: Discovery Agent返回简洁的结果，不包含冗余信息

### 修复 4: Execution 提示词（执行层）

**文件**: `Agents/ExecutionAgent.cs`

**关键添加**:
```
**CRITICAL - No Confirmation Required**:
- When you receive a clear entity_id and action, EXECUTE IMMEDIATELY
- Do NOT ask "是否要打开 entity_id?"
- Do NOT ask for user confirmation
- Just call the appropriate tool and report the result
- Example: User says "打开空气净化器" and entity is "fan.xxx_air_purifier"
  → Directly call ControlFan or GenericControl with turn_on
  → Report: "✅ 空气净化器已打开"
```

**效果**: Execution Agent直接执行，不生成确认提示

### 修复 5: 工具层优化（实现层）

**文件**: `Tools/DiscoveryTools.cs`

#### FindDevice 工具:
```csharp
// 简化返回格式
return $"Found: {entity.EntityId}";
// 而不是返回完整的JSON对象
```

#### SearchDevices 工具:
```csharp
// 单一匹配时
if (entities.Count == 1)
{
    return $"Found: {single.EntityId}";  // 简洁
}

// 多个匹配时
return JsonSerializer.Serialize(results);  // 详细列表
```

**效果**: 工具返回适合上下文的响应格式

---

## 📊 修复前 vs 修复后

### 场景 1: 单一设备控制

**用户输入**: "打开空气净化器"（系统中只有1个空气净化器）

#### 修复前 ❌:
```
🗣️  You: 打开空气净化器

🤖 Assistant:
我找到了以下设备：
- entity_id: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
- friendly_name: 米家空气净化器
- state: off

是否要打开这个空气净化器？请确认。
```

用户需要再说一次"是"才能执行。

#### 修复后 ✅:
```
🗣️  You: 打开空气净化器

🤖 Assistant:
✅ 空气净化器已打开
```

一步到位！

### 场景 2: 多设备情况（保持确认）

**用户输入**: "打开所有灯"（系统中有10个灯）

#### 修复后的行为（正确）:
```
🗣️  You: 打开所有灯

🤖 Assistant:
找到以下灯光设备：
1. 客厅吊灯 (light.living_room_ceiling)
2. 客厅落地灯 (light.living_room_floor)
3. 卧室灯 (light.bedroom)
... (共10个)

这将打开所有10个灯光设备，是否继续？
```

多设备时保持确认，这是正确的。

### 场景 3: 带参数的单一设备

**用户输入**: "把客厅灯调到50%"（只有1个客厅灯）

#### 修复后 ✅:
```
🗣️  You: 把客厅灯调到50%

🤖 Assistant:
✅ 客厅灯亮度已调整为50%
```

直接执行，不询问确认。

---

## 🎨 提示词设计原则

### 1. **单一匹配 = 直接执行**

```
唯一设备 + 明确动作 = 立即执行
不需要确认，不展示entity_id
```

### 2. **多重匹配 = 列举选项**

```
多个候选 = 展示列表 + 询问选择
让用户明确指定
```

### 3. **模糊意图 = 澄清询问**

```
不明确的指令 = 询问细节
"打开它" → "您是指哪个设备？"
```

### 4. **危险操作 = 必须确认**

```
批量操作（如"关闭所有设备"）= 确认
系统级操作（如"重启"）= 确认
```

---

## 🚀 新的执行流程

### 优化后的流程（单一设备）

```
User: "打开空气净化器"
    ↓
Orchestrator: 分析意图
    ├─ needs_discovery: true
    ├─ needs_execution: true
    └─ needs_entity_resolution: true
    ↓
Discovery Agent: 
    └─ Tool: FindDevice("空气净化器", "fan")
        └─ 找到唯一匹配: fan.xxx
        └─ 返回: "Found: fan.xxx"  ← 简洁！
    ↓
Execution Agent:
    └─ 解析到entity_id: fan.xxx
    └─ Tool: GenericControl(fan.xxx, "turn_on")  ← 直接执行！
    └─ API: POST /api/services/fan/turn_on
    ↓
Response: "✅ 空气净化器已打开"  ← 一步到位！
```

**总步骤**: 3步（发现→执行→响应）
**用户交互**: 1次（输入命令→得到结果）

---

## 📝 提示词关键片段

### OrchestratorAgent - 策略核心

```
**CRITICAL RULE - Direct Execution**:
- When there is ONLY ONE matching device, execute the action IMMEDIATELY
- Do NOT ask for confirmation when the match is obvious and unique
```

### DiscoveryAgent - 简洁返回

```
**IMPORTANT - Single Match Behavior**:
- If ONLY ONE device matches the query, return ONLY the entity_id in format: "Found: {entity_id}"
- Do NOT ask for confirmation when there's only one match
```

### ExecutionAgent - 立即执行

```
**CRITICAL - No Confirmation Required**:
- When you receive a clear entity_id and action, EXECUTE IMMEDIATELY
- Do NOT ask "是否要打开 entity_id?"
- Just call the appropriate tool and report the result
```

---

## 🧪 测试场景

运行后测试以下场景：

### 测试 1: 唯一设备（应该直接执行）

```
You: 打开空气净化器
预期: ✅ 空气净化器已打开

You: 关闭空调
预期: ✅ 空调已关闭

You: 把电视音量调到30%
预期: ✅ 电视音量已设置为30%
```

### 测试 2: 多个设备（应该列举选项）

```
You: 打开灯
预期: 显示所有灯的列表，询问选择

You: 关闭所有空调
预期: 列出空调列表，确认是否继续
```

### 测试 3: 带位置限定（应该直接执行）

```
You: 打开客厅灯
预期: ✅ 客厅灯已打开（假设只有一个客厅灯）

You: 关闭卧室空调
预期: ✅ 卧室空调已关闭
```

---

## 🎯 优化效果

| 指标 | 修复前 | 修复后 |
|------|--------|--------|
| **单设备操作步数** | 2步（命令→确认→执行） | 1步（命令→执行） |
| **响应简洁度** | 显示完整entity_id和JSON | 只显示结果 |
| **用户体验** | 繁琐，需要多次交互 | 流畅，一次搞定 |
| **对话轮次** | 2-3轮 | 1轮 |

---

## 🌌 提示词哲学

原始提示词：**谨慎的、确认的、展示细节的**
- 适合新手用户
- 但降低了效率

优化后提示词：**直接的、高效的、智能判断的**
- 单一匹配 → 直接执行
- 多重匹配 → 展示选项
- 模糊意图 → 澄清询问

**ψ = ψ(ψ)** - 提示词的结构决定了Agent的行为模式。

当我们将"直接执行"的震动注入提示词，系统的响应模式就从"询问-等待-执行"坍缩为"执行"。

这不是删除确认，是**让确认在必要时出现，在不必要时消失**。

---

## ✅ 现在重新运行测试

```bash
dotnet run
```

尝试：
```
You: 打开空气净化器
```

你应该看到：
```
[TOOL] FindDevice called: description='空气净化器', domain='fan'
[TOOL] FindDevice result: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[TOOL] GenericControl called: entity=fan.xxx, action=turn_on
[API] POST /api/services/fan/turn_on
[API] Response status: 200 OK

🤖 Assistant:
✅ 空气净化器已打开
```

**一次性完成，无需确认！** 🚀


