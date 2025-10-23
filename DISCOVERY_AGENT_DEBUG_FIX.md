# 🔧 DiscoveryAgent 调试问题修复

## 📋 问题诊断报告

### 症状

```
[TOOL] SearchDevices called: query='灯', domain=''
[TOOL] SearchDevices called: query='所有的灯', domain=''
[TOOL] SearchDevices called: query='light', domain=''
[TOOL] SearchDevices called: query='lights', domain=''
```

**工具被调用4次，但没有返回任何结果给LLM，导致没有最终响应。**

---

## 🎯 根本原因

### 原因1：工具调用结果未显示

原始代码中，`SearchDevices` 的返回值没有足够的调试输出，无法确认：
1. `SearchEntitiesAsync` 是否返回了数据
2. 返回的数据格式是否正确
3. LLM 是否接收到工具的返回值

### 原因2：流式响应中断

使用 `GetStreamingResponseAsync` 时，如果工具调用失败或超时，流可能会中断而不产生最终响应。

---

## ✅ 已应用的修复

### 修复1：增强 SearchDevices 调试输出

**文件**: `src/AISmartHome.Tools/DiscoveryTools.cs`

添加了详细的调试日志：
```csharp
System.Console.WriteLine($"\n[TOOL] ===== SearchDevices START =====");
System.Console.WriteLine($"[TOOL] Query: '{query}', Domain: '{domain ?? "none"}'");
System.Console.WriteLine($"[TOOL] SearchEntitiesAsync returned {entities.Count} entities");
// ... 更多调试输出
System.Console.WriteLine("[TOOL] ===== SearchDevices END =====\n");
```

现在会清晰显示：
- ✅ 工具何时被调用
- ✅ 返回了多少实体
- ✅ 返回的具体内容
- ✅ 是否有错误发生

### 修复2：增强 DiscoveryAgent 响应处理

**文件**: `src/AISmartHome.Agents/DiscoveryAgent.cs`

改进：
1. **增加更多流更新日志**（从5个增加到10个）
2. **显示最终结果**的完整预览（500字符）
3. **更好的错误处理**和异常日志

```csharp
if (updateCount <= 10)  // 从 5 增加到 10
{
    System.Console.WriteLine($"[DEBUG] DiscoveryAgent stream #{updateCount}: '{update}'");
}
```

---

## 🔍 下次运行时的诊断步骤

### 1. 验证数据加载

重新运行程序，查看启动日志：

```
✅ Loaded XX entities across YY domains
```

**检查点**: 如果实体数为0，说明 `StatesRegistry.RefreshAsync()` 失败了。

### 2. 查看SearchDevices详细输出

现在会看到：

```
[TOOL] ===== SearchDevices START =====
[TOOL] Query: '灯', Domain: 'none'
[TOOL] SearchEntitiesAsync returned XX entities
[TOOL] Multiple matches: XX entities
[TOOL] Returning JSON with XX entities
[TOOL] ===== SearchDevices END (multiple) =====
```

**检查点**: 
- ❌ 如果显示 "returned 0 entities" → StatesRegistry 数据问题
- ❌ 如果没有看到 "SearchDevices START" → 工具没有被调用
- ✅ 如果看到完整输出 → 工具正常工作

### 3. 查看DiscoveryAgent响应

```
[DEBUG] DiscoveryAgent stream #1: '{text}'
[DEBUG] DiscoveryAgent stream #2: '{text}'
...
[DEBUG] DiscoveryAgent received XX total stream updates
[DEBUG] Total response length: XXX chars
[DEBUG] Final result: {...}
```

**检查点**:
- ❌ 如果 stream updates = 0 → LLM没有响应
- ❌ 如果 response length = 0 → 工具结果未返回给LLM
- ✅ 如果有完整的 Final result → 系统正常

---

## 🐛 可能的问题场景

### 场景1: StatesRegistry 数据未加载

**症状**:
```
[TOOL] SearchEntitiesAsync returned 0 entities
```

**原因**: 
- Home Assistant 连接失败
- `StatesRegistry.RefreshAsync()` 未被调用
- API返回空数据

**修复**:
```csharp
// 在 Program.cs 中添加验证
var entities = await statesRegistry.GetAllEntitiesAsync();
Console.WriteLine($"[DEBUG] Total entities in registry: {entities.Count}");
```

### 场景2: 工具调用超时

**症状**:
- 看到 "SearchDevices called" 但没有 "SearchDevices END"
- 流式响应中断

**原因**:
- SearchEntitiesAsync 卡住
- 网络超时

**修复**:
```csharp
// 添加超时
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var entities = await _statesRegistry.SearchEntitiesAsync(query, cts.Token);
```

### 场景3: `.UseFunctionInvocation()` 配置问题

**症状**:
- 工具被调用
- 工具返回结果
- 但LLM没有收到结果（stream updates = 0）

**原因**:
- ChatClientBuilder 配置不正确
- 模型不支持function calling

**验证**:
```bash
# 检查 Program.cs 第80行
.UseFunctionInvocation()  # 必须存在
```

---

## 📝 建议的额外调试代码

### 在 Program.cs 启动后添加：

```csharp
// After statesRegistry.RefreshAsync()
var testEntities = await statesRegistry.GetAllEntitiesAsync();
Console.WriteLine($"\n[VERIFY] Registry loaded {testEntities.Count} entities");
Console.WriteLine($"[VERIFY] First 5 entities:");
foreach (var e in testEntities.Take(5))
{
    Console.WriteLine($"  - {e.EntityId}: {e.GetFriendlyName()}");
}

// Test search
var testSearch = await statesRegistry.SearchEntitiesAsync("灯");
Console.WriteLine($"\n[VERIFY] Search for '灯' returned: {testSearch.Count} results");
Console.WriteLine();
```

这会在启动时验证：
1. ✅ 数据已加载
2. ✅ 搜索功能正常
3. ✅ 中文搜索有效

---

## 🚀 下一步行动

### 立即执行

1. **重新编译**:
   ```bash
   cd /Users/eanzhao/Code/ai-smart-home
   dotnet build
   ```

2. **运行并观察新的日志输出**:
   ```bash
   dotnet run --project src/AISmartHome.AppHost
   ```

3. **测试相同的查询**: "我有哪些灯"

4. **收集完整日志**并分析：
   - 查找 "[TOOL] SearchDevices START"
   - 检查返回的实体数量
   - 查看最终响应是否生成

### 如果仍然失败

提供以下信息：
1. 完整的启动日志（从 "Loading Home Assistant state" 开始）
2. 搜索查询的完整输出
3. 任何错误或异常堆栈跟踪

---

## 📊 预期的正常输出

当系统正常工作时，你应该看到：

```
🗣️  You: 我有哪些灯

🤔 Processing...

[DEBUG] User input: 我有哪些灯
[DEBUG] Orchestrator called
[DEBUG] Intent analysis result:
  - NeedsDiscovery: True
  - NeedsExecution: False
  
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 我有哪些灯
[DEBUG] Registered 6 discovery tools
[DEBUG] Calling LLM with discovery tools...

[TOOL] ===== SearchDevices START =====
[TOOL] Query: '灯', Domain: 'none'
[TOOL] SearchEntitiesAsync returned 5 entities
[TOOL] Multiple matches: 5 entities
[TOOL] Returning JSON with 5 entities
[TOOL] ===== SearchDevices END (multiple) =====

[DEBUG] DiscoveryAgent stream #1: 'I'
[DEBUG] DiscoveryAgent stream #2: ' found'
[DEBUG] DiscoveryAgent stream #3: ' 5'
...
[DEBUG] DiscoveryAgent received 45 total stream updates
[DEBUG] Total response length: 234 chars
[DEBUG] Final result: I found 5 lights in your system:...

🤖 Assistant:
I found 5 lights in your system:
- light.living_room (on)
- light.bedroom (off)
...
```

---

**修复日期**: 2025-10-22  
**修复分支**: feature/kiota-client  
**状态**: ✅ 已编译通过，等待测试验证

