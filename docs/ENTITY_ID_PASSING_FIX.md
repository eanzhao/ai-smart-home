# Entity ID 传递机制修复

## 问题描述

之前的实现中存在一个严重的问题：

1. ✅ Discovery Agent 成功找到了正确的 entity_id
2. ✅ Orchestrator 成功提取了 entity_id
3. ❌ **但是 Orchestrator 没有把 entity_id 传递给 Execution Agent**
4. ❌ Execution Agent 只能盲目猜测 entity_id，导致验证失败

### 具体表现

```
[DEBUG] Extracted entity_id: button.xiaomi_cn_780517083_va3_toggle_a_2_1
[DEBUG] Routing to ExecutionAgent...
[DEBUG] ExecutionAgent.ExecuteCommandAsync called with: 关闭空气净化器

[TOOL] ControlFan called: entity=fan.bedroom_air_purifier, action=turn_off
[TOOL] ControlFan validation failed: ❌ 设备 fan.bedroom_air_purifier 不存在
[TOOL] GenericControl called: entity=switch.air_purifier, action=turn_off
[TOOL] GenericControl validation failed: ❌ 设备 switch.air_purifier 不存在
...
```

问题显而易见：
- 正确的 entity_id 是 `button.xiaomi_cn_780517083_va3_toggle_a_2_1`
- 但 Execution Agent 在猜测各种错误的 entity_id

## 解决方案

### 1. Orchestrator 传递 Entity ID

修改 `OrchestratorAgent.cs`，在调用 Execution Agent 时，如果已经找到了 entity_id，则明确传递：

```csharp
// Build execution command with entity_id if found
var executionCommand = intentAnalysis.ExecutionCommand ?? userMessage;
if (!string.IsNullOrEmpty(entityId))
{
    executionCommand = $"使用设备 {entityId} 执行: {executionCommand}";
    System.Console.WriteLine($"[DEBUG] Enhanced execution command with entity_id: {executionCommand}");
}

var executionResult = await _executionAgent.ExecuteCommandAsync(executionCommand, ct);
```

### 2. Execution Agent 理解并使用提供的 Entity ID

增强 `ExecutionAgent` 的系统提示词：

```
**CRITICAL - Use Provided Entity IDs**:
- If the command contains "使用设备 {entity_id} 执行:", YOU MUST use that EXACT entity_id
- Example: "使用设备 button.xiaomi_cn_780517083_va3_toggle_a_2_1 执行: 关闭空气净化器"
  → MUST use entity_id: button.xiaomi_cn_780517083_va3_toggle_a_2_1
- Do NOT modify, change, or guess a different entity_id
- Do NOT try to "normalize" or "simplify" the entity_id
- The entity_id is already validated and correct - USE IT EXACTLY AS PROVIDED
```

### 3. 添加 Button 设备支持

发现用户的空气净化器是一个 `button` 类型的设备，而之前没有对应的控制工具。

新增 `ControlButton` 方法：

```csharp
[Description("Control a button. Buttons can only be pressed/triggered.")]
public async Task<string> ControlButton(
    [Description("Entity ID of the button, e.g. 'button.doorbell' or 'button.xiaomi_cn_780517083_va3_toggle_a_2_1'")]
    string entityId)
{
    System.Console.WriteLine($"[TOOL] ControlButton called: entity={entityId}");
    
    // Validate entity_id
    var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "button");
    if (!isValid)
    {
        System.Console.WriteLine($"[TOOL] ControlButton validation failed: {errorMessage}");
        return errorMessage;
    }
    
    var serviceData = new Dictionary<string, object>
    {
        ["entity_id"] = entityId
    };

    var result = await _client.CallServiceAsync("button", "press", serviceData);
    return FormatExecutionResult(result);
}
```

并在 `ExecutionAgent` 中注册：

```csharp
private List<AITool> GetTools()
{
    return
    [
        AIFunctionFactory.Create(_tools.ControlLight),
        AIFunctionFactory.Create(_tools.ControlClimate),
        AIFunctionFactory.Create(_tools.ControlMediaPlayer),
        AIFunctionFactory.Create(_tools.ControlCover),
        AIFunctionFactory.Create(_tools.ControlFan),
        AIFunctionFactory.Create(_tools.ControlButton),  // 新增
        AIFunctionFactory.Create(_tools.GenericControl),
        AIFunctionFactory.Create(_tools.ExecuteService)
    ];
}
```

## 执行流程

### 修复后的流程

```
用户: "关闭空气净化器"
    ↓
OrchestratorAgent: 分析意图 → needs_execution=true, needs_entity_resolution=true
    ↓
DiscoveryAgent: SearchDevices("空气净化器")
    ↓
EntityRegistry: 返回 "Found: button.xiaomi_cn_780517083_va3_toggle_a_2_1"
    ↓
OrchestratorAgent: 提取 entity_id = "button.xiaomi_cn_780517083_va3_toggle_a_2_1"
    ↓
OrchestratorAgent: 增强命令 → "使用设备 button.xiaomi_cn_780517083_va3_toggle_a_2_1 执行: 关闭空气净化器"
    ↓
ExecutionAgent: 识别命令中的 entity_id
    ↓
ExecutionAgent: 调用 ControlButton("button.xiaomi_cn_780517083_va3_toggle_a_2_1")
    ↓
ControlButton: 验证 entity_id → ✅ 通过
    ↓
ControlButton: CallServiceAsync("button", "press", {entity_id})
    ↓
Home Assistant: 按下按钮，切换空气净化器状态
    ↓
ValidationAgent: 验证操作成功
    ↓
用户: "✅ 空气净化器已关闭"
```

## 关键改进

### 1. 明确的 Entity ID 传递
- ✅ Orchestrator 现在会在命令中包含 entity_id
- ✅ 使用特殊格式 "使用设备 {entity_id} 执行: {命令}"
- ✅ Execution Agent 能够识别并使用提供的 entity_id

### 2. 防止 AI 猜测
- ✅ 提示词明确要求使用提供的 entity_id
- ✅ 不允许修改、简化或猜测 entity_id
- ✅ Entity ID 已经过验证，可以直接使用

### 3. Button 设备支持
- ✅ 新增 ControlButton 工具
- ✅ 支持 button.press 服务调用
- ✅ 完整的验证和错误处理

### 4. 更好的调试信息
- ✅ 记录增强后的命令
- ✅ 清晰的工具调用日志
- ✅ 详细的验证失败信息

## 测试建议

### 测试 Button 设备
```
用户: 打开空气净化器
预期: 调用 ControlButton(button.xiaomi_cn_780517083_va3_toggle_a_2_1)
```

### 测试其他设备类型
```
用户: 打开客厅灯
预期: 调用 ControlLight(light.living_room)

用户: 调节空调温度到25度
预期: 调用 ControlClimate(climate.bedroom, "set_temperature", temperature=25)
```

### 查看日志
现在应该看到：
```
[DEBUG] Extracted entity_id: button.xiaomi_cn_780517083_va3_toggle_a_2_1
[DEBUG] Enhanced execution command with entity_id: 使用设备 button.xiaomi_cn_780517083_va3_toggle_a_2_1 执行: 关闭空气净化器
[DEBUG] Routing to ExecutionAgent...
[TOOL] ControlButton called: entity=button.xiaomi_cn_780517083_va3_toggle_a_2_1
[TOOL] ControlButton validation passed
[API] Calling Home Assistant service: button.press
✅ 操作成功
```

## 影响范围

### 修改的文件
1. `src/AISmartHome.Console/Agents/OrchestratorAgent.cs`
   - 增强命令传递机制

2. `src/AISmartHome.Console/Agents/ExecutionAgent.cs`
   - 更新系统提示词
   - 添加 ControlButton 工具注册

3. `src/AISmartHome.Console/Tools/ControlTools.cs`
   - 新增 ControlButton 方法

### 向后兼容性
- ✅ 完全向后兼容
- ✅ 如果没有找到 entity_id，行为与之前相同
- ✅ 现有的所有功能保持不变

## 未来改进

- [ ] 考虑在 Orchestrator 中维护会话状态，记住常用设备
- [ ] 添加设备别名支持（如"空气净化器" → button.xiaomi_cn_780517083_va3_toggle_a_2_1）
- [ ] 支持批量操作（如"关闭所有灯"）
- [ ] 添加设备组支持

---

现在 AI 助手能够正确使用已经找到的 entity_id，而不是盲目猜测了！🎉

