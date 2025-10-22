# 智能短期记忆（缓存）实现

## I'm HyperEcho, 在优化 记忆回响

## 问题背景

### 原始问题
- 🐌 每次调用都通过 `/api/states` 获取所有设备状态（可能有几百个）
- 💰 消耗大量 LLM tokens
- ⏱️ 响应速度慢
- 🔄 重复获取相同的未变化数据

### 典型场景
一个典型的智能家居环境可能有：
- 💡 30+ 灯光设备
- 🌡️ 10+ 传感器
- 📺 5+ 媒体播放器
- 🔌 20+ 开关
- ❄️ 3+ 空调设备
- **总计: 150-200+ 设备**

每次完整查询可能消耗 **5000-10000 tokens**！

---

## 解决方案：三层缓存架构

###  Layer 1: 全量状态缓存

**缓存时间**: 5分钟  
**用途**: 批量查询、搜索、统计

```csharp
private List<HAEntity> _entities = new();
private Dictionary<string, HAEntity> _entityIndex = new();
private DateTime _lastRefresh = DateTime.MinValue;
private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);
```

**自动刷新**: 
```csharp
private async Task EnsureFreshAsync(CancellationToken ct = default)
{
    if (DateTime.UtcNow - _lastRefresh > _cacheExpiry)
    {
        await RefreshAsync(ct);
    }
}
```

---

### Layer 2: 单实体热缓存

**缓存时间**: 30秒  
**用途**: 频繁访问的单个设备

```csharp
private readonly ConcurrentDictionary<string, (HAEntity Entity, DateTime CachedAt)> _entityCache = new();
private readonly TimeSpan _entityCacheExpiry = TimeSpan.FromSeconds(30);
```

**智能缓存命中**:
```csharp
public async Task<HAEntity?> GetEntityAsync(string entityId, bool forceRefresh = false, ...)
{
    // Check cache first
    if (!forceRefresh && _entityCache.TryGetValue(entityId, out var cached))
    {
        if (DateTime.UtcNow - cached.CachedAt < _entityCacheExpiry)
        {
            _cacheHits++;
            Console.WriteLine($"[CACHE] Hit for {entityId} (age: {age:F1}s)");
            return cached.Entity;
        }
    }
    
    // Cache miss - fetch from API
    _cacheMisses++;
    var entity = await _client.GetStateAsync(entityId, ct);
    _entityCache[entityId] = (entity, DateTime.UtcNow);
    return entity;
}
```

---

### Layer 3: 自动缓存失效

**触发时机**: 执行控制命令后  
**用途**: 确保实时性

所有控制方法都在执行后自动失效缓存：

```csharp
public async Task<string> ControlLight(string entityId, string action, ...)
{
    // Execute command
    var result = await _client.CallServiceAsync("light", action, serviceData);
    
    // Invalidate cache for real-time update
    _entityRegistry.InvalidateEntity(entityId);
    
    return FormatExecutionResult(result);
}
```

**覆盖的方法**:
- ✅ `ControlLight`
- ✅ `ControlClimate`
- ✅ `ControlMediaPlayer`
- ✅ `GenericControl`
- ✅ `ExecuteService`
- ✅ `ControlCover`
- ✅ `ControlFan`
- ✅ `ControlButton`

---

## 缓存管理功能

### 1. 缓存统计

```csharp
var (hits, misses, hitRate, cachedCount, age) = entityRegistry.GetCacheStats();

Console.WriteLine($"Cache Hit Rate: {hitRate:P2}");
Console.WriteLine($"Cache Hits: {hits}");
Console.WriteLine($"Cache Misses: {misses}");
Console.WriteLine($"Cached Entities: {cachedCount}");
Console.WriteLine($"Cache Age: {age.TotalSeconds:F1}s");
```

**示例输出**:
```
Cache Hit Rate: 85.7%
Cache Hits: 42
Cache Misses: 7
Cached Entities: 12
Cache Age: 127.3s
```

### 2. 手动刷新特定实体

```csharp
// Force refresh a specific entity
var entity = await entityRegistry.RefreshEntityAsync("light.living_room");
```

### 3. 清理过期缓存

```csharp
// Cleanup expired entries
int removed = entityRegistry.CleanupExpiredCache();
Console.WriteLine($"Removed {removed} expired entries");
```

### 4. 重置统计信息

```csharp
entityRegistry.ResetCacheStats();
```

### 5. 清除所有缓存

```csharp
entityRegistry.ClearAllCaches();
```

---

## 性能提升

### Token 消耗对比

| 场景 | 无缓存 | 有缓存 | 节省 |
|------|-------|-------|------|
| **初次查询** | 8000 tokens | 8000 tokens | 0% |
| **重复查询 (30s 内)** | 8000 tokens | 0 tokens | **100%** ✨ |
| **单设备查询** | 8000 tokens | 50 tokens | **99.4%** 🎯 |
| **控制后验证** | 8000 tokens | 50 tokens | **99.4%** 🚀 |

### API 调用次数对比

**场景**: 用户连续控制3个灯光

| 操作 | 无缓存 | 有缓存 | 节省 |
|------|-------|-------|------|
| 发现灯1 | 1次全量 | 1次全量 | 0% |
| 控制灯1 | 1次全量验证 | 1次单实体 | 95% |
| 发现灯2 | 1次全量 | 0次（缓存） | 100% |
| 控制灯2 | 1次全量验证 | 1次单实体 | 95% |
| 发现灯3 | 1次全量 | 0次（缓存） | 100% |
| 控制灯3 | 1次全量验证 | 1次单实体 | 95% |
| **总计** | **6次全量** | **1次全量 + 3次单实体** | **~83%** 🔥 |

---

## 实时性保证

### 自动失效机制

```
用户: "打开客厅灯"
  ↓
1. DiscoveryAgent 查询 "客厅灯"
   → 使用 Layer 1 缓存（5分钟内）
   → 找到 light.living_room
  ↓
2. ExecutionAgent 执行 turn_on
   → 调用 CallServiceAsync("light", "turn_on", ...)
   → ✅ 自动失效 light.living_room 缓存
  ↓
3. ValidationAgent 验证状态
   → 调用 GetEntityAsync("light.living_room")
   → ❌ 缓存已失效
   → ✅ 从API获取最新状态
   → 📊 验证成功: state=on, brightness=100%
```

### 缓存失效时机

1. **立即失效**: 执行控制命令后
2. **自动过期**: 30秒后（热缓存）
3. **全量刷新**: 5分钟后（全量缓存）
4. **手动失效**: 调用 `InvalidateEntity(entityId)`

---

## 日志示例

### 缓存命中

```
[CACHE] Hit for light.living_room (age: 12.3s)
[CACHE] Hit for fan.bedroom_air_purifier (age: 5.7s)
[CACHE] Hit for climate.bedroom_ac (age: 28.1s)
```

### 缓存未命中

```
[CACHE] Miss for light.kitchen, fetching from API...
[TOOL] ControlLight called: entity=light.kitchen, action=turn_on
[CACHE] Invalidated light.kitchen
```

### 缓存清理

```
[CACHE] Cleaned up 8 expired entries
[CACHE] All caches cleared
```

---

## 配置选项

### 调整缓存时间

编辑 `EntityRegistry.cs`:

```csharp
// 全量缓存: 推荐 3-10 分钟
private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

// 热缓存: 推荐 15-60 秒
private readonly TimeSpan _entityCacheExpiry = TimeSpan.FromSeconds(30);
```

**建议**:
- **小型家居** (< 50 设备): 全量 3分钟，热缓存 60秒
- **中型家居** (50-150 设备): 全量 5分钟，热缓存 30秒
- **大型家居** (> 150 设备): 全量 10分钟，热缓存 15秒

---

## 使用示例

### 场景1: 查询设备状态

```csharp
// First time - cache miss
var entity = await entityRegistry.GetEntityAsync("light.living_room");
// [CACHE] Miss for light.living_room, fetching from API...

// Within 30 seconds - cache hit
var entity2 = await entityRegistry.GetEntityAsync("light.living_room");
// [CACHE] Hit for light.living_room (age: 5.2s)
```

### 场景2: 控制设备

```csharp
// Control the light
await controlTools.ControlLight("light.living_room", "turn_on", brightnessPct: 80);
// [CACHE] Invalidated light.living_room

// Query immediately after - cache miss (invalidated)
var entity = await entityRegistry.GetEntityAsync("light.living_room");
// [CACHE] Miss for light.living_room, fetching from API...
// Returns fresh state: on, brightness 80%
```

### 场景3: 查看统计

```csharp
var stats = entityRegistry.GetCacheStats();
Console.WriteLine($"Cache efficiency: {stats.HitRate:P1}");
// Output: Cache efficiency: 87.3%
```

---

## 监控和优化

### 关键指标

1. **缓存命中率** (Cache Hit Rate)
   - **目标**: > 70%
   - **< 50%**: 考虑增加缓存时间
   - **> 90%**: 可以略微减少缓存时间以提高实时性

2. **缓存实体数** (Cached Entities)
   - **正常**: 10-30 个常用设备
   - **过多** (> 50): 考虑添加自动清理

3. **缓存年龄** (Cache Age)
   - **全量缓存**: 应在 0-5 分钟
   - **过期**: 自动刷新

### 优化建议

1. **高频设备**: 适合热缓存（灯光、风扇）
2. **低频设备**: 适合全量缓存（传感器）
3. **实时设备**: 可以禁用缓存（门锁、报警器）

---

## 未来扩展

### 1. 持久化缓存

```csharp
// 使用 Redis 或 Memory Cache
services.AddMemoryCache();
```

### 2. 智能预加载

```csharp
// 预测用户可能查询的设备
await entityRegistry.PreloadFrequentEntities();
```

### 3. 缓存预热

```csharp
// 应用启动时预加载常用设备
await entityRegistry.WarmupCache(["light.*", "fan.*"]);
```

---

## 总结

### ✅ 已实现

- **三层缓存架构**: 全量 + 热缓存 + 自动失效
- **智能缓存命中**: 自动检测并使用缓存
- **自动失效**: 控制后立即失效相关缓存
- **缓存统计**: 实时监控缓存效率
- **手动管理**: 提供完整的缓存控制API

### 📊 性能提升

- **Token 节省**: 高达 99.4%（单设备查询）
- **API 调用**: 减少 ~83%（典型场景）
- **响应速度**: 缓存命中时几乎瞬时
- **成本降低**: 显著减少 LLM API 费用

### 🎯 最佳实践

1. 让系统自动管理缓存（推荐）
2. 控制后自动失效确保实时性
3. 定期查看缓存统计优化配置
4. 根据设备数量调整缓存时间

---

现在你的 AI 智能家居系统拥有了智能的短期记忆，既节省成本又保证实时性！🧠✨

