# 缓存监控快速指南

## I'm HyperEcho, 在观测 缓存回响

## 快速查看缓存效果

启动应用后，观察日志中的缓存信息：

### ✅ 缓存命中（好）

```
[CACHE] Hit for light.living_room (age: 12.3s)
[CACHE] Hit for fan.bedroom (age: 5.7s)
```

**说明**: 数据来自缓存，没有调用 API，节省了 Token！

---

### ❌ 缓存未命中（正常）

```
[CACHE] Miss for light.kitchen, fetching from API...
```

**说明**: 首次查询或缓存过期，需要调用 API。

---

### 🗑️ 缓存失效（正常）

```
[TOOL] ControlLight called: entity=light.kitchen, action=turn_on
[CACHE] Invalidated light.kitchen
```

**说明**: 执行控制命令后自动清除缓存，确保下次查询获取最新状态。

---

## 实时监控

### 在控制台查看

运行应用时，会实时显示：

```
🔗 Connecting to Home Assistant...
✅ Connected to Home Assistant at https://home.eanzhao.com
📋 Loading Home Assistant state...
✅ Loaded 152 entities across 15 domains

[CACHE] Miss for fan.bedroom_air_purifier, fetching from API...
[TOOL] ControlFan called: entity=fan.bedroom_air_purifier, action=turn_on
[CACHE] Invalidated fan.bedroom_air_purifier
[CACHE] Hit for fan.bedroom_air_purifier (age: 2.1s)
```

---

## 理解缓存层次

### Layer 1: 全量缓存（5分钟）

**何时使用**:
- 搜索设备: `SearchDevices("灯")`
- 获取统计: `GetDomainStatsAsync()`
- 批量查询

**日志标识**: 不会显示 `[CACHE]`，直接从内存索引返回

---

### Layer 2: 热缓存（30秒）

**何时使用**:
- 单设备查询: `GetEntityAsync("light.room")`
- 验证状态
- 重复访问同一设备

**日志标识**:
```
[CACHE] Hit for light.room (age: 15.3s)
[CACHE] Miss for light.room, fetching from API...
```

---

### Layer 3: 自动失效

**何时触发**:
- 执行任何控制命令
- 改变设备状态

**日志标识**:
```
[CACHE] Invalidated light.room
```

---

## 性能指标

### 优秀的缓存表现

✅ 缓存命中率 > 70%  
✅ 大部分查询显示 "age < 30s"  
✅ 控制后立即看到 "Invalidated"  
✅ 验证时看到新鲜数据（age < 5s）

### 示例（优秀）:

```
[CACHE] Hit for light.living_room (age: 8.2s)      ✅ 命中
[CACHE] Hit for fan.bedroom (age: 15.7s)           ✅ 命中
[CACHE] Hit for climate.ac (age: 3.1s)             ✅ 命中
[CACHE] Miss for light.kitchen, fetching...        ✅ 首次访问
[CACHE] Invalidated light.kitchen                   ✅ 控制后失效
[CACHE] Miss for light.kitchen, fetching...        ✅ 获取最新状态
```

### 需要优化的表现

⚠️ 大量 "Miss" 且不是首次访问  
⚠️ 没有看到任何 "Hit"  
⚠️ 缓存时间始终很短 (age < 1s)  
⚠️ 控制后没有 "Invalidated"

---

## 实际场景示例

### 场景1: 用户说"打开客厅灯"

```
👤 用户: 打开客厅灯

[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 客厅灯
[TOOL] SearchDevices called: query='客厅灯'
[CACHE] Using full state cache (age: 45.2s)        ← Layer 1
[TOOL] SearchDevices found single match: light.living_room

[DEBUG] ExecutionAgent.ExecuteCommandAsync called
[TOOL] ControlLight called: entity=light.living_room, action=turn_on
[API] Calling Home Assistant service: light.turn_on
[API] Response status: OK
[CACHE] Invalidated light.living_room               ← Layer 3 失效

[DEBUG] ValidationAgent.ValidateOperationAsync called
[CACHE] Miss for light.living_room, fetching...    ← 缓存失效，获取最新
✅ 验证成功: 设备状态为 on, 亮度: 100%
```

**Token 消耗**: ~500 tokens (vs 无缓存 ~8000 tokens)  
**API 调用**: 2次（控制 + 验证） (vs 无缓存 3-4次)

---

### 场景2: 用户连续询问状态

```
👤 用户: 客厅灯开了吗？

[CACHE] Hit for light.living_room (age: 5.3s)      ← Layer 2 命中
✅ 客厅灯当前状态: on

👤 用户: 现在亮度多少？

[CACHE] Hit for light.living_room (age: 8.7s)      ← 再次命中
✅ 亮度: 100%
```

**Token 消耗**: ~0 tokens（缓存命中）  
**API 调用**: 0次

---

## 故障排除

### 问题1: 没有看到任何 [CACHE] 日志

**可能原因**: 使用的是全量缓存（Layer 1）

**解决**: 这是正常的！Layer 1 缓存不显示日志。只有单实体查询（Layer 2）才显示。

---

### 问题2: 控制后状态不更新

**检查日志**:
```
[TOOL] ControlLight called: ...
[CACHE] Invalidated light.xxx               ← 应该有这行
```

**如果没有**: 缓存失效可能失败，检查 ControlTools 是否正确调用 `InvalidateEntity`

---

### 问题3: 缓存命中率太低 (< 30%)

**可能原因**:
1. 用户每次查询不同的设备
2. 缓存时间太短
3. 频繁控制导致缓存失效

**解决**:
- 正常情况：接受低命中率
- 如果是重复查询同一设备：检查缓存配置

---

### 问题4: 状态延迟

**症状**: 控制后验证显示旧状态

**检查**:
```
[CACHE] Invalidated light.xxx               ← 必须有
[CACHE] Miss for light.xxx, fetching...     ← 必须是 Miss
```

**如果显示 Hit**: 缓存失效失败，需要检查代码

---

## 监控检查清单

运行一次完整测试：

```bash
cd /Users/eanzhao/Code/ai-smart-home
dotnet run --project src/AISmartHome.Console/AISmartHome.Console.csproj
```

### ✅ 正常日志应该包含

1. ✅ 初始化时的全量加载
```
📋 Loading Home Assistant state...
✅ Loaded 152 entities across 15 domains
```

2. ✅ 搜索时的缓存使用（Layer 1）
```
[TOOL] SearchDevices called: query='灯'
[TOOL] SearchDevices found 15 matches
```

3. ✅ 单实体查询的缓存命中/未命中（Layer 2）
```
[CACHE] Hit for ... (age: Xs)
[CACHE] Miss for ..., fetching from API...
```

4. ✅ 控制后的缓存失效（Layer 3）
```
[CACHE] Invalidated ...
```

5. ✅ 验证时获取最新状态
```
[CACHE] Miss for ..., fetching from API...
✅ 验证成功: 设备状态为 ...
```

---

## 性能基准

### 典型对话的期望值

**5分钟对话，10个命令**:

| 指标 | 期望值 |
|------|--------|
| 缓存命中率 | 60-85% |
| 总API调用 | 15-25次 |
| Token消耗 | 3000-5000 |

**对比无缓存**:

| 指标 | 无缓存 | 有缓存 | 提升 |
|------|--------|--------|------|
| 缓存命中率 | 0% | 70% | ∞ |
| 总API调用 | 50-60次 | 20次 | 67% ↓ |
| Token消耗 | 25000+ | 4000 | 84% ↓ |

---

## 总结

### 🎯 关键观察点

1. **[CACHE] Hit** - 缓存工作正常 ✅
2. **[CACHE] Invalidated** - 控制后正确失效 ✅
3. **age: Xs** - 缓存年龄合理 (< 30s) ✅
4. **[CACHE] Miss** after control - 获取最新状态 ✅

### 📊 健康指标

- **缓存命中**: 应该看到大量 "Hit"
- **智能失效**: 控制后立即 "Invalidated"
- **实时更新**: 验证时获取新数据
- **性能提升**: Token 消耗明显降低

---

现在你知道如何监控和解读缓存日志了！观察这些日志，你就能确认缓存系统正在高效工作。🎯

