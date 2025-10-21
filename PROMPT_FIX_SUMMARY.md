# Discovery Agent 输出格式修复

## 问题回顾

用户的空气净化器设备：
- Entity ID: `fan.xiaomi_cn_780517083_va3_s_2_air_purifier`
- Friendly Name: "小米家空气净化器 5S 空气净化器"
- State: On

当用户说"关闭空气净化器"时，Discovery Agent虽然找到了设备，但是返回了错误的格式：

```
❌ 实际返回：
I found the following air purifier currently being used:

- **Entity ID**: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
- **Friendly Name**: 小米家空气净化器 5S 空气净化器
- **State**: On

If you need more details about this device, just let me know!
```

```
✅ 应该返回：
Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
```

导致的结果：
- Orchestrator 无法提取 entity_id（因为没有"Found: "开头）
- Execution Agent 收不到 entity_id
- 操作失败

## 核心修复

### 1. 超强化的输出格式要求

```
**CRITICAL - Single Match Output Format**:
- When the tool returns "Found: {entity_id}", you MUST return it EXACTLY AS IS
- Do NOT reformat, expand, or add any explanation
- Do NOT convert it to markdown or add device details
- Do NOT say anything before or after the "Found: {entity_id}" line
- ABSOLUTELY NO additional text, formatting, or explanations
```

### 2. 提供清晰的步骤指导

```
**Step-by-step for single match**:
1. Call SearchDevices or FindDevice
2. Tool returns: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
3. You return: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
4. DONE. Nothing more!
```

### 3. 提供正确和错误的示例

```
**Examples of CORRECT responses**:
✅ "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
✅ "Found: light.living_room"
✅ "Found: button.xiaomi_cn_780517083_va3_toggle_a_2_1"

**Examples of WRONG responses** (these will BREAK the system):
❌ "I found the following air purifier currently being used:\n\n- **Entity ID**: fan...." 
❌ "The air purifier is: fan...."
❌ Any response that is NOT exactly "Found: {entity_id}"
```

### 4. Few-Shot Learning 示例

添加了具体的对话示例，让AI通过模仿学习：

```
**EXAMPLE CONVERSATIONS (Learn from these)**:

Example 1 - Single Match:
User: "空气净化器"
You: [Call SearchDevices("空气净化器")]
Tool: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
You: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
```

## 执行流程

### 修复后的完整流程

```
用户: "关闭空气净化器"
  ↓
OrchestratorAgent: 分析意图
  needs_execution: true
  needs_entity_resolution: true
  entity_query: "关闭空气净化器"
  ↓
OrchestratorAgent: ExtractDeviceName("关闭空气净化器")
  → "空气净化器" ✅
  ↓
DiscoveryAgent: ProcessQueryAsync("空气净化器")
  ↓
DiscoveryAgent: 调用 SearchDevices("空气净化器") ✅
  ↓
SearchDevices: 找到 1 个匹配
  → 返回 "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier" ✅
  ↓
DiscoveryAgent: 收到工具返回 "Found: fan...."
  → 直接返回 "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier" ✅
  （不添加任何额外文本）
  ↓
OrchestratorAgent: 提取 entity_id
  discoveryResult.StartsWith("Found: ") → true ✅
  entityId = "fan.xiaomi_cn_780517083_va3_s_2_air_purifier" ✅
  ↓
OrchestratorAgent: 增强命令
  → "使用设备 fan.xiaomi_cn_780517083_va3_s_2_air_purifier 执行: 关闭空气净化器" ✅
  ↓
ExecutionAgent: 识别命令中的 entity_id
  → "fan.xiaomi_cn_780517083_va3_s_2_air_purifier" ✅
  ↓
ExecutionAgent: 调用 ControlFan(entity_id, "turn_off") ✅
  ↓
ControlFan: 验证 entity_id
  ValidateEntityIdAsync → ✅ 通过
  ↓
HomeAssistantClient: CallServiceAsync("fan", "turn_off", {...})
  ↓
Home Assistant API: 执行成功 ✅
  ↓
ValidationAgent: 验证状态
  fan.xiaomi_cn_780517083_va3_s_2_air_purifier state: "off" ✅
  ↓
用户收到: "✅ 空气净化器已关闭"
```

## 关键点

### 1. Discovery Agent 的输出必须简洁
- **单个匹配**: 只返回 `Found: {entity_id}`
- **多个匹配**: 返回详细列表

### 2. Orchestrator 的提取逻辑
- 查找 `discoveryResult.StartsWith("Found: ")`
- 提取 `entity_id = discoveryResult.Substring(7).Trim()`

### 3. 这要求严格的格式一致性
- 任何额外的文本都会导致提取失败
- 任何格式变化都会破坏流程

## 预期日志输出

```
[DEBUG] Entity query: 关闭空气净化器
[DEBUG] Extracted device name: 空气净化器
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 空气净化器
[DEBUG] Registered 5 discovery tools
[TOOL] SearchDevices called: query='空气净化器', domain='null'
[TOOL] SearchDevices found single match: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] DiscoveryAgent response: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] Entity resolution result: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] Extracted entity_id: fan.xiaomi_cn_780517083_va3_s_2_air_purifier ✅
[DEBUG] Enhanced execution command with entity_id: 使用设备 fan.xiaomi_cn_780517083_va3_s_2_air_purifier 执行: 关闭空气净化器
[DEBUG] Routing to ExecutionAgent...
[TOOL] ControlFan called: entity=fan.xiaomi_cn_780517083_va3_s_2_air_purifier, action=turn_off
[API] Calling Home Assistant service: fan.turn_off
[API] Response status: OK
✅ 空气净化器已关闭
```

## 提示词工程技巧

### 1. 使用强烈的语言
- "MUST", "NEVER", "ALWAYS"
- "CRITICAL", "ABSOLUTELY"
- 使用全大写强调

### 2. 提供具体示例
- 正确示例（✅）
- 错误示例（❌）
- 实际对话示例

### 3. 解释后果
- "these will BREAK the system"
- 让AI理解为什么规则重要

### 4. Few-Shot Learning
- 提供具体的对话示例
- 让AI通过模仿学习
- 比抽象规则更有效

### 5. 重复关键信息
- 在不同部分重复重要规则
- 使用不同的表达方式
- 增加记忆强度

## 测试建议

现在测试以下场景：

### 测试1: 关闭空气净化器
```
输入: 关闭空气净化器
预期输出: 
  🔍 Finding device:
  Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
  
  ⚡ Execution:
  ✅ 空气净化器已关闭
  
  ✅ Verification:
  验证成功 - 设备状态: off
```

### 测试2: 打开客厅灯
```
输入: 打开客厅灯
预期: Discovery返回 "Found: light.living_room"
```

### 测试3: 查询所有灯
```
输入: 我有哪些灯？
预期: Discovery返回详细的JSON列表（因为是多个匹配）
```

## 成功标准

✅ Discovery Agent 返回格式严格为 `Found: {entity_id}`
✅ Orchestrator 成功提取 entity_id
✅ Execution Agent 收到正确的 entity_id
✅ 设备操作成功执行
✅ Validation Agent 验证成功

---

现在 Discovery Agent 应该会严格遵守输出格式了！如果还有问题，可能需要调整 LLM 的 temperature 参数或使用更强的模型。🎯

