# 最终测试指南 - Discovery Agent 输出格式

## 最新修复

### 问题
Discovery Agent 找到了设备，但返回了详细格式而不是简单的 "Found: entity_id" 格式。

### 解决方案

1. **超强化的提示词**
   - 明确要求单个匹配时只返回 `Found: {entity_id}`
   - 提供正确和错误示例的对比
   - 使用 Few-Shot Learning
   - 强调"EXACTLY AS IS"，不要加工

2. **Temperature = 0**
   - Discovery Agent 和 Execution Agent 都设置为 temperature=0
   - 确保输出更加确定性和一致

3. **设备名称提取**
   - Orchestrator 自动移除动作词（打开、关闭等）
   - 只传递纯设备名称给 Discovery Agent

## 测试场景

### 场景1: 关闭空气净化器（你的实际场景）

**输入**:
```
关闭空气净化器
```

**预期流程**:
```
1. Orchestrator: 提取设备名称 "空气净化器"
2. DiscoveryAgent: SearchDevices("空气净化器")
3. Tool返回: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
4. DiscoveryAgent返回: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
   （不是详细信息！）
5. Orchestrator: 提取entity_id成功
6. ExecutionAgent: ControlFan(..., "turn_off")
7. ValidationAgent: 验证状态
```

**预期输出**:
```
🔍 Finding device:
Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier

⚡ Execution:
✅ 空气净化器已关闭

✅ Verification:
验证成功 - 设备状态: off
```

### 场景2: 打开客厅灯

**输入**:
```
打开客厅灯
```

**预期**:
```
🔍 Finding device:
Found: light.living_room

⚡ Execution:
✅ 客厅灯已打开

✅ Verification:
验证成功 - 设备状态: on, 亮度: 100%
```

### 场景3: 查询所有灯（多个匹配）

**输入**:
```
我有哪些灯？
```

**预期**:
```
🔍 Discovery:
[
  {
    "entity_id": "light.living_room",
    "friendly_name": "客厅灯",
    "state": "on",
    "domain": "light"
  },
  {
    "entity_id": "light.bedroom",
    "friendly_name": "卧室灯",
    "state": "off",
    "domain": "light"
  }
  ...
]
```

## 关键日志检查点

### 检查点1: 设备名称提取
```
[DEBUG] Entity query: 关闭空气净化器
[DEBUG] Extracted device name: 空气净化器  ← 应该只有设备名
```

### 检查点2: Discovery调用
```
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 空气净化器  ← 不应该包含"关闭"
```

### 检查点3: 工具调用
```
[TOOL] SearchDevices called: query='空气净化器', domain='null'
[TOOL] SearchDevices found single match: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
```

### 检查点4: Discovery返回格式 ⭐️ 关键！
```
[DEBUG] DiscoveryAgent response: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier

不应该是:
❌ [DEBUG] DiscoveryAgent response: I found the following air purifier...
```

### 检查点5: Entity ID提取
```
[DEBUG] Entity resolution result: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] Extracted entity_id: fan.xiaomi_cn_780517083_va3_s_2_air_purifier  ← 应该成功提取
```

### 检查点6: 增强的执行命令
```
[DEBUG] Enhanced execution command with entity_id: 使用设备 fan.xiaomi_cn_780517083_va3_s_2_air_purifier 执行: 关闭空气净化器
```

### 检查点7: 工具调用
```
[TOOL] ControlFan called: entity=fan.xiaomi_cn_780517083_va3_s_2_air_purifier, action=turn_off
[API] Calling Home Assistant service: fan.turn_off
[API] Response status: OK
```

## 如果还是失败

### 方案A: 检查工具返回
在日志中查找：
```
[TOOL] SearchDevices found single match: XXX
```
确认工具确实返回了 "Found: entity_id"

### 方案B: 检查Discovery Agent的完整响应
在日志中查找：
```
[DEBUG] DiscoveryAgent received X stream updates
[DEBUG] Total response length: X chars
```
紧接着应该有完整的响应内容

### 方案C: 如果Temperature=0还不够
可能需要：
1. 在提示词开头就强调格式
2. 使用更强的模型（gpt-4而不是gpt-4o-mini）
3. 在system prompt的最后再次重申格式要求

### 方案D: 后处理提取
如果AI就是不听话，可以在Orchestrator中添加正则表达式提取：

```csharp
// Try to extract entity_id even from verbose responses
var match = Regex.Match(discoveryResult, @"(fan|light|climate|switch|button)\.[a-zA-Z0-9_]+");
if (match.Success)
{
    entityId = match.Value;
    System.Console.WriteLine($"[DEBUG] Extracted entity_id via regex: {entityId}");
}
```

## 终极解决方案（如果上述都不行）

创建一个专门的Entity Resolution Agent，它的唯一职责就是返回entity_id，不做任何其他事情。这样可以极大简化提示词。

## 验证成功的标准

✅ Discovery返回: `Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier`（只有这一行）
✅ Orchestrator提取: `entity_id = "fan.xiaomi_cn_780517083_va3_s_2_air_purifier"`
✅ Execution使用: `ControlFan("fan.xiaomi_cn_780517083_va3_s_2_air_purifier", "turn_off")`
✅ API调用: `POST /api/services/fan/turn_off`
✅ 设备状态: `state = "off"`

---

现在测试一下"关闭空气净化器"，应该能看到正确的"Found: entity_id"格式了！如果还不行，请把完整的日志发给我，我会进一步调整。🎯

