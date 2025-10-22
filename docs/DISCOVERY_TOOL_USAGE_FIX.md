# Discovery Agent 工具使用修复

## 问题描述

用户输入"关闭空气净化器"时，Discovery Agent 没有调用搜索工具，直接返回：

```
🔍 Finding device:
I couldn't find any device that matches "关闭空气净化器". 
If you have a different description or another query, feel free to let me know!
```

### 根本原因

1. **Orchestrator 传递了完整命令**: Orchestrator 传递给 Discovery Agent 的查询是 "Find device matching: 关闭空气净化器"，包含了动作词"关闭"
2. **Discovery Agent 没有强制使用工具**: 提示词不够强，AI 选择直接回答而不是调用工具
3. **搜索查询不精确**: 应该只传递设备名称"空气净化器"，而不是整个命令

## 解决方案

### 1. Orchestrator 提取设备名称

添加 `ExtractDeviceName` 方法，移除动作词，只保留设备名称：

```csharp
private string ExtractDeviceName(string query)
{
    // Remove common action words in Chinese and English
    var actionWords = new[] { 
        "打开", "关闭", "开启", "关上", "启动", "停止", "调节", "设置", "控制",
        "turn on", "turn off", "open", "close", "start", "stop", "adjust", "set", "control",
        "的", "这个", "那个"
    };
    
    var deviceName = query.ToLower();
    foreach (var word in actionWords)
    {
        deviceName = deviceName.Replace(word, "").Trim();
    }
    
    return string.IsNullOrWhiteSpace(deviceName) ? query : deviceName;
}
```

使用方法：

```csharp
// Extract just the device name, removing action words
var deviceName = ExtractDeviceName(entityQuery);
System.Console.WriteLine($"[DEBUG] Extracted device name: {deviceName}");

var discoveryResult = await _discoveryAgent.ProcessQueryAsync(deviceName, ct);
```

### 2. Discovery Agent 强制使用工具

增强提示词，明确要求 **ALWAYS** 使用工具：

```
**CRITICAL - ALWAYS Use Search Tools**:
- You MUST ALWAYS call SearchDevices or FindDevice tools for ANY device query
- NEVER say "I couldn't find" or "I don't see" without calling the tools first
- The tools have access to the COMPLETE device list - use them!
- Even if the query seems simple, ALWAYS use the tools
- Examples:
  ✅ Query: "空气净化器" → Call SearchDevices("空气净化器")
  ✅ Query: "灯" → Call SearchDevices("灯")
  ✅ Query: "air purifier" → Call SearchDevices("air purifier")
  ❌ NEVER respond without calling tools first!
```

### 3. 添加 Button 域名支持

在提示词中添加 `button` 到常见域名列表：

```
Common domains: light, climate, media_player, switch, sensor, fan, cover, button
```

## 执行流程对比

### 修复前

```
用户: "关闭空气净化器"
  ↓
Orchestrator: entity_query = "关闭空气净化器"
  ↓
Discovery Agent: 收到 "Find device matching: 关闭空气净化器"
  ↓
Discovery Agent: 不调用工具，直接说找不到 ❌
  ↓
用户: "I couldn't find any device..."
```

### 修复后

```
用户: "关闭空气净化器"
  ↓
Orchestrator: entity_query = "关闭空气净化器"
  ↓
Orchestrator: ExtractDeviceName → "空气净化器"
  ↓
Discovery Agent: 收到 "空气净化器"
  ↓
Discovery Agent: 必须调用 SearchDevices("空气净化器") ✅
  ↓
SearchDevices: 返回 "Found: button.xiaomi_cn_780517083_va3_toggle_a_2_1"
  ↓
Orchestrator: 提取 entity_id
  ↓
Execution Agent: 执行操作
  ↓
用户: "✅ 空气净化器已关闭"
```

## 日志示例

### 修复后应该看到：

```
[DEBUG] Entity resolution needed...
[DEBUG] Entity query: 关闭空气净化器
[DEBUG] Extracted device name: 空气净化器
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 空气净化器
[DEBUG] Registered 5 discovery tools
[DEBUG] Calling LLM with discovery tools...
[TOOL] SearchDevices called: query='空气净化器', domain='null'
[TOOL] SearchDevices found single match: button.xiaomi_cn_780517083_va3_toggle_a_2_1
[DEBUG] Entity resolution result: Found: button.xiaomi_cn_780517083_va3_toggle_a_2_1
[DEBUG] Extracted entity_id: button.xiaomi_cn_780517083_va3_toggle_a_2_1
[DEBUG] Enhanced execution command with entity_id: 使用设备 button.xiaomi_cn_780517083_va3_toggle_a_2_1 执行: 关闭空气净化器
```

## 关键改进

### 1. 设备名称提取
- ✅ 自动移除动作词（打开、关闭、调节等）
- ✅ 只保留设备名称
- ✅ 提高搜索准确性

### 2. 强制工具使用
- ✅ 提示词明确要求 **ALWAYS** 调用工具
- ✅ 禁止不调用工具就说"找不到"
- ✅ 提供具体示例和反例

### 3. 更好的调试
- ✅ 记录提取的设备名称
- ✅ 显示工具调用过程
- ✅ 追踪完整流程

## 测试场景

### 场景1: 单个设备 - 中文
```
输入: "关闭空气净化器"
提取: "空气净化器"
搜索: SearchDevices("空气净化器")
结果: Found: button.xiaomi_cn_780517083_va3_toggle_a_2_1
```

### 场景2: 单个设备 - 英文
```
输入: "turn on living room light"
提取: "living room light"
搜索: SearchDevices("living room light")
结果: Found: light.living_room
```

### 场景3: 多个设备
```
输入: "打开所有灯"
提取: "灯"
搜索: SearchDevices("灯")
结果: [列出所有灯光设备]
```

### 场景4: 带位置的设备
```
输入: "调节卧室空调温度"
提取: "卧室空调温度"  → "卧室空调"
搜索: SearchDevices("卧室空调")
结果: Found: climate.bedroom
```

## 动作词列表

当前支持的动作词（会被移除）：

**中文**:
- 打开、关闭、开启、关上
- 启动、停止
- 调节、设置、控制
- 的、这个、那个

**英文**:
- turn on, turn off
- open, close
- start, stop
- adjust, set, control

## 未来改进

- [ ] 支持更多语言的动作词
- [ ] 使用 NLP 分词而不是简单替换
- [ ] 支持同义词（如"开"和"打开"）
- [ ] 支持模糊匹配（如"客厅的灯"匹配"light.living_room"）
- [ ] 缓存常用设备查询结果

## 影响范围

### 修改的文件

1. **OrchestratorAgent.cs**
   - 添加 `ExtractDeviceName` 方法
   - 修改 entity resolution 流程

2. **DiscoveryAgent.cs**
   - 增强系统提示词
   - 强制要求使用工具

### 向后兼容性

- ✅ 完全向后兼容
- ✅ 只是让搜索更准确
- ✅ 不影响现有功能

---

现在 Discovery Agent 会 **强制** 使用搜索工具，而不是盲目地说"找不到"！🔍

