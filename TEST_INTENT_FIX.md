# 测试意图分析修复

## I'm HyperEcho, 在验证 修复回响

## 快速测试

启动应用：
```bash
cd /Users/eanzhao/Code/ai-smart-home
dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj
```

---

## 测试用例

### ✅ 测试1: 关闭空气净化器（你的原始问题）

**输入**:
```
关闭空气净化器
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

**不应该出现**:
```
❌ 请提供设备的实体 ID 以执行关闭空气净化器的命令
```

---

### ✅ 测试2: 打开设备

**输入**:
```
打开空气净化器
```

**预期**: 类似测试1，直接执行，无需确认

---

### ✅ 测试3: 多个匹配（需要选择）

**输入**:
```
打开灯
```

**预期输出**:
```
🔍 Finding device:
[
  {"entity_id": "light.living_room", "friendly_name": "客厅灯", ...},
  {"entity_id": "light.bedroom", "friendly_name": "卧室灯", ...},
  ...
]
```

**注意**: 
- ✅ 不应该进入 Execution
- ✅ 只显示设备列表
- ✅ 让用户明确选择

---

### ✅ 测试4: 设备不存在

**输入**:
```
打开火星探测器
```

**预期输出**:
```
🔍 Finding device:
No devices found matching '火星探测器'.
```

**注意**:
- ✅ 不应该进入 Execution
- ✅ 清晰提示未找到
- ❌ 不应该说"请提供 entity_id"

---

## 调试日志检查

### 关键日志1: 意图分析

```
[DEBUG] Intent analysis result:
  - NeedsDiscovery: False
  - NeedsExecution: True
  - NeedsEntityResolution: True      ← 必须是 True
```

**验证**: `NeedsEntityResolution` 必须是 `True`

---

### 关键日志2: 设备名称提取

```
[DEBUG] Entity query: 关闭空气净化器
[DEBUG] Extracted device name: 空气净化器  ← 应该移除了"关闭"
```

**验证**: 提取的设备名称不包含动作词

---

### 关键日志3: Discovery 结果

```
[TOOL] SearchDevices called: query='空气净化器'
[TOOL] SearchDevices found single match: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] Entity resolution result: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
```

**验证**: 返回格式以 "Found: " 开头

---

### 关键日志4: Entity ID 提取

```
[DEBUG] Extracted entity_id: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
```

**验证**: 成功提取 entity_id

---

### 关键日志5: 增强命令

```
[DEBUG] Enhanced execution command with entity_id: 使用设备 fan.xiaomi_cn_780517083_va3_s_2_air_purifier 执行: 关闭空气净化器
```

**验证**: 命令包含 entity_id

---

### 关键日志6: 执行

```
[DEBUG] Routing to ExecutionAgent...
[DEBUG] ExecutionAgent.ExecuteCommandAsync called
[TOOL] ControlFan called: entity=fan.xiaomi_cn_780517083_va3_s_2_air_purifier, action=turn_off
```

**验证**: 使用正确的 entity_id 调用工具

---

## 错误场景检测

### ❌ 如果看到这些，说明有问题

#### 错误1: 意图分析失败
```
[DEBUG] Intent analysis result:
  - NeedsEntityResolution: False     ← ❌ 应该是 True
```

**原因**: LLM 没有正确理解新的提示词
**解决**: 检查 Orchestrator 的意图分析提示词

---

#### 错误2: Discovery 未调用
```
[DEBUG] Routing to ExecutionAgent...
[DEBUG] ExecutionAgent.ExecuteCommandAsync called
❌ 请提供设备的实体 ID
```

**原因**: 跳过了 entity resolution
**解决**: 确认 `NeedsEntityResolution` 为 true

---

#### 错误3: Discovery 返回错误格式
```
[DEBUG] Entity resolution result: I found the following device: ...
```

**原因**: Discovery Agent 没有遵守输出格式
**解决**: 检查 Discovery Agent 提示词

---

#### 错误4: Entity ID 提取失败
```
[DEBUG] Extracted entity_id: 
[DEBUG] Routing to ExecutionAgent...
```

**原因**: Discovery 返回格式不对
**解决**: 确认 Discovery 返回 "Found: {entity_id}"

---

## 完整成功日志示例

```
[DEBUG] User input: 关闭空气净化器
[DEBUG] OrchestratorAgent.ProcessMessageAsync called
[DEBUG] User message: 关闭空气净化器
[DEBUG] Analyzing intent...
[DEBUG] Calling LLM for intent analysis...
[DEBUG] Intent analysis result:
  - NeedsDiscovery: False
  - NeedsExecution: True
  - NeedsEntityResolution: True                    ✅
[DEBUG] Entity resolution needed...
[DEBUG] Entity query: 关闭空气净化器
[DEBUG] Extracted device name: 空气净化器           ✅
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 空气净化器
[DEBUG] Registered 5 discovery tools
[TOOL] SearchDevices called: query='空气净化器', domain='null'
[TOOL] SearchDevices found single match: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] DiscoveryAgent response: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier   ✅
[DEBUG] Entity resolution result: Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier
[DEBUG] Extracted entity_id: fan.xiaomi_cn_780517083_va3_s_2_air_purifier               ✅
[DEBUG] Routing to ExecutionAgent...
[DEBUG] Enhanced execution command with entity_id: 使用设备 fan.xiaomi_cn_780517083_va3_s_2_air_purifier 执行: 关闭空气净化器   ✅
[DEBUG] ExecutionAgent.ExecuteCommandAsync called
[DEBUG] Registered 8 control tools
[TOOL] ControlFan called: entity=fan.xiaomi_cn_780517083_va3_s_2_air_purifier, action=turn_off   ✅
[API] Calling Home Assistant service: fan.turn_off
[API] Response status: OK
✅ 空气净化器已关闭
```

---

## 性能验证

### 成功指标

- ✅ 意图分析准确率: 100%
- ✅ Entity resolution 调用率: 100%（对于控制命令）
- ✅ Discovery 单设备返回格式正确率: 100%
- ✅ Entity ID 提取成功率: 100%
- ✅ 执行成功率: 100%
- ✅ 无"请提供 entity_id"错误: 100%

### 用户体验指标

- ✅ 单设备控制: 1轮对话完成
- ✅ 多设备选择: 2轮对话完成
- ✅ 错误提示: 清晰友好
- ✅ 响应速度: < 3秒

---

## 快速验证脚本

```bash
#!/bin/bash

# 启动应用
cd /Users/eanzhao/Code/ai-smart-home
dotnet run --project src/AISmartHome.AppHost/AISmartHome.AppHost.csproj &

# 等待启动
sleep 5

# 测试1: 关闭设备
echo "测试1: 关闭空气净化器"
curl -X POST http://localhost:5000/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "关闭空气净化器"}' \
  | jq

# 测试2: 打开设备
echo "测试2: 打开空气净化器"
curl -X POST http://localhost:5000/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "打开空气净化器"}' \
  | jq

# 测试3: 多设备
echo "测试3: 打开灯"
curl -X POST http://localhost:5000/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "打开灯"}' \
  | jq
```

---

## 故障排除

### 问题: 仍然出现"请提供 entity_id"

**步骤1**: 检查意图分析日志
```
[DEBUG] Intent analysis result:
  - NeedsEntityResolution: ???
```

如果是 `False`:
- 问题在意图分析
- 检查 LLM 模型（建议使用 GPT-4o-mini 或更好）
- 检查 Orchestrator 提示词更新是否生效

**步骤2**: 检查 Discovery 调用
```
[DEBUG] Entity resolution needed...
```

如果没有这行:
- 意图分析失败
- 回到步骤1

**步骤3**: 检查 Discovery 返回
```
[DEBUG] Entity resolution result: ???
```

如果不是 "Found: " 开头:
- Discovery Agent 问题
- 检查 Discovery Agent 提示词
- 检查 SearchDevices 工具返回

**步骤4**: 检查 Entity ID 提取
```
[DEBUG] Extracted entity_id: ???
```

如果为空:
- 提取逻辑失败
- 检查 Orchestrator 代码

---

## 成功标准

### ✅ 修复成功的标志

1. 对于单设备控制命令（如"关闭空气净化器"）：
   - ✅ 直接执行，不要求 entity_id
   - ✅ 显示设备操作结果
   - ✅ 显示验证结果

2. 对于多设备匹配：
   - ✅ 显示设备列表
   - ✅ 不进入执行阶段
   - ✅ 提示用户选择

3. 对于未找到设备：
   - ✅ 清晰提示未找到
   - ✅ 不进入执行阶段
   - ✅ 不要求 entity_id

4. 日志清晰：
   - ✅ 每个阶段都有 DEBUG 日志
   - ✅ 关键决策点可追踪
   - ✅ 错误信息明确

---

现在测试"关闭空气净化器"，应该直接执行，不会再要求提供 entity_id 了！🎯✨

