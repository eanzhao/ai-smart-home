# Entity ID 验证机制

## 问题描述

在之前的实现中，AI可能会使用占位符或虚假的entity_id（如 `fan.xxx_air_purifier`）来调用Home Assistant API，导致操作失败或行为异常。

## 解决方案

我已经实现了一套完整的entity_id验证机制，确保所有控制操作都使用真实、有效的设备ID。

### 1. ControlTools 验证方法

在 `ControlTools` 类中添加了 `ValidateEntityIdAsync` 方法，对entity_id进行多层验证：

#### 验证内容：
- **非空检查**: entity_id不能为空或空白
- **占位符检测**: 拒绝包含 `xxx`、`placeholder`、`example` 等占位符的ID
- **格式验证**: 确保格式为 `domain.entity_name`
- **域名验证**: 验证域名是否符合预期（如light、fan、climate等）
- **存在性验证**: 检查entity_id是否真实存在于Home Assistant系统中

#### 验证结果：
如果验证失败，会返回详细的错误信息：
- `❌ Entity ID不能为空`
- `❌ 检测到占位符entity_id: fan.xxx_air_purifier。请使用真实的设备ID。`
- `❌ Entity ID格式错误: invalid_format。正确格式应为 'domain.entity_name'`
- `❌ Entity ID域名错误: 期望 'light'，实际为 'fan'`
- `❌ 设备 fan.nonexistent 不存在。请先使用发现工具查找正确的entity_id。`

### 2. 所有控制方法都已添加验证

以下方法都已添加entity_id验证：
- `ControlLight` - 验证light域
- `ControlClimate` - 验证climate域
- `ControlMediaPlayer` - 验证media_player域
- `ControlCover` - 验证cover域
- `ControlFan` - 验证fan域
- `GenericControl` - 验证任意域

### 3. Agent提示词增强

#### DiscoveryAgent
增强了提示词，强调：
- 必须使用搜索工具获取真实的entity_id
- 禁止编造、猜测或使用占位符entity_id
- 必须调用 SearchDevices 或 FindDevice 工具
- 提供了真实和虚假entity_id的示例对比

#### ExecutionAgent
增强了提示词，强调：
- 必须使用真实、完整的entity_id
- 禁止使用占位符如 xxx、placeholder、example
- 禁止编造或猜测entity_id
- 如果没有真实的entity_id，必须先使用发现工具
- 工具会自动验证并拒绝占位符

### 4. 验证流程

```
用户: "打开空气净化器"
    ↓
Discovery Agent: 调用 SearchDevices("空气净化器")
    ↓
EntityRegistry: 返回真实的entity_id (如 "fan.bedroom_air_purifier")
    ↓
Discovery Agent: 返回 "Found: fan.bedroom_air_purifier"
    ↓
Orchestrator: 提取 entity_id = "fan.bedroom_air_purifier"
    ↓
Execution Agent: 调用 ControlFan("fan.bedroom_air_purifier", "turn_on")
    ↓
ControlTools.ValidateEntityIdAsync: 
    ✓ 非空检查
    ✓ 占位符检测
    ✓ 格式验证
    ✓ 域名验证 (fan)
    ✓ 存在性验证
    ↓
ControlTools.ControlFan: 执行真实的API调用
    ↓
Home Assistant: 设备成功打开
```

### 5. 错误处理示例

如果AI尝试使用虚假的entity_id：

```
[TOOL] ControlFan called: entity=fan.xxx_air_purifier, action=turn_on
[TOOL] ControlFan validation failed: ❌ 检测到占位符entity_id: fan.xxx_air_purifier。请使用真实的设备ID。
```

AI会收到错误信息，并知道需要先使用发现工具获取真实的entity_id。

### 6. 日志输出

每个控制方法现在都会输出详细的日志：
- 工具调用日志: `[TOOL] ControlFan called: entity=..., action=...`
- 验证失败日志: `[TOOL] ControlFan validation failed: ...`
- API调用日志: `[API] Calling Home Assistant service: ...`

### 7. 测试建议

运行程序后，尝试以下场景：

1. **正常场景**（应该成功）:
   ```
   打开客厅灯
   关闭空调
   调节风扇速度到50%
   ```

2. **错误场景**（应该被拦截）:
   - AI如果尝试使用占位符entity_id，会被验证机制拦截
   - 错误信息会清晰地告知问题所在
   - AI会被引导使用发现工具获取真实的entity_id

### 8. 优势

✅ **安全性**: 防止无效的API调用
✅ **可靠性**: 确保只操作真实存在的设备
✅ **可调试性**: 详细的错误信息和日志
✅ **用户体验**: 清晰的错误提示，帮助AI自我纠正
✅ **自动化**: 验证是自动的，无需手动检查

现在你的智能家居控制系统具备了完整的entity_id验证能力，确保所有操作都是安全、可靠的！

