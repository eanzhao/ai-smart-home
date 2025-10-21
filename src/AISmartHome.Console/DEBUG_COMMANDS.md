# 🐛 调试命令速查表

运行系统后，你会看到详细的DEBUG日志。以下是日志的解读和调试命令。

## 🔍 日志分类

### 执行流程日志 `[DEBUG]`

追踪Agent的执行路径：

```
[DEBUG] OrchestratorAgent.ProcessMessageAsync called
[DEBUG] User message: 打开客厅灯
[DEBUG] Analyzing intent...
[DEBUG] Calling LLM for intent analysis...
[DEBUG] Stream update #1: {...
[DEBUG] Total stream updates: 15
[DEBUG] Intent analysis result:
  - NeedsDiscovery: false
  - NeedsExecution: true
  - NeedsEntityResolution: true
```

### 工具调用日志 `[TOOL]`

显示哪些工具被LLM调用：

```
[TOOL] SearchDevices called: query='客厅灯', domain='light'
[TOOL] FindDevice called: description='卧室温度', domain='climate'
[TOOL] ControlLight called: entity=light.living_room, action=turn_on, brightness=50
```

### API调用日志 `[API]`

显示对Home Assistant的实际HTTP请求：

```
[API] Calling Home Assistant service: light.turn_on
[API] Service data: {"entity_id":"light.living_room","brightness_pct":50}
[API] POST /api/services/light/turn_on
[API] Response status: 200 OK
```

## 📋 诊断检查清单

### ✅ 系统正常的日志特征

1. **启动日志完整**:
   ```
   ✅ Connected to Home Assistant at http://...
   ✅ Loaded XX entities across XX domains
   ✅ Loaded XXX services
   ✅ Multi-Agent system initialized
   ```

2. **每次对话都有流式更新**:
   ```
   [DEBUG] Total stream updates: XX  (XX > 0)
   ```

3. **工具被正确调用**:
   ```
   [TOOL] ... called: ...
   ```

4. **API调用成功**:
   ```
   [API] Response status: 200 OK
   ```

### ❌ 常见问题模式

#### 问题 A: LLM完全无响应

**日志特征**:
```
[DEBUG] Calling LLM...
[DEBUG] Total stream updates: 0
[DEBUG] Total response length: 0 chars
```

**原因**: 
- API key无效
- 网络问题
- Endpoint配置错误

**解决**:
1. 检查 `appsettings.json` 中的 `OpenAI:ApiKey`
2. 如果使用GitHub Models，确认endpoint是 `https://models.github.ai/inference`
3. 测试网络连接

#### 问题 B: LLM返回但工具未调用

**日志特征**:
```
[DEBUG] Total stream updates: 20
[DEBUG] Total response length: 200 chars
# 但没有 [TOOL] 日志
```

**原因**:
- LLM选择不调用工具（直接回答）
- 工具注册失败
- Function calling未启用

**解决**:
1. 检查是否看到：
   ```
   [DEBUG] Registered 6 discovery tools
   [DEBUG] Registered 7 control tools
   ```
2. 确认客户端初始化时包含 `.UseFunctionInvocation()`
3. 尝试更明确的指令："Use the SearchDevices tool to find lights"

#### 问题 C: 工具调用但执行失败

**日志特征**:
```
[TOOL] ControlLight called: ...
[API] Response status: 404 Not Found
```

**原因**:
- Entity ID不存在
- Service不支持
- 参数错误

**解决**:
1. 先用 `refresh` 命令更新缓存
2. 检查entity_id是否正确
3. 查看HA日志

#### 问题 D: 意图分析错误

**日志特征**:
```
[DEBUG] Intent analysis result:
  - NeedsDiscovery: true   ← 应该是false
  - NeedsExecution: false  ← 应该是true
```

用户明明要求执行操作，但被识别为查询。

**原因**: LLM误解了意图分析prompt

**临时解决**: 使用更明确的指令
```
# 不好的:
"灯"  → 容易被理解为查询

# 好的:
"打开客厅灯"  → 明确的动作
```

## 🛠️ 调试技巧

### 技巧 1: 逐层测试

1. **测试HA连接**:
   ```
   You: refresh
   ```

2. **测试LLM基础**:
   ```
   You: Hello
   ```

3. **测试发现功能**:
   ```
   You: What lights do I have?
   ```

4. **测试控制功能**:
   ```
   You: Turn on light.xxx  (使用确切的entity_id)
   ```

### 技巧 2: 使用确切的entity_id

如果语义搜索不工作，直接使用entity_id：

```
You: Turn on light.living_room  
# 而不是 "Turn on the living room light"
```

### 技巧 3: 查看工具是否注册成功

在每个Agent的 `GetTools()` 方法中添加：

```csharp
private List<AITool> GetTools()
{
    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(_tools.SearchDevices),
        // ...
    };
    
    System.Console.WriteLine($"[DEBUG] Created {tools.Count} tools");
    foreach (var tool in tools)
    {
        System.Console.WriteLine($"[DEBUG]   - Tool: {tool.Metadata.Name}");
    }
    
    return tools;
}
```

### 技巧 4: 捕获完整异常

如果遇到神秘错误，在Program.cs的catch块已经添加了详细异常信息：

```csharp
catch (Exception ex)
{
    System.Console.WriteLine($"❌ Error: {ex.Message}");
    System.Console.WriteLine($"   Type: {ex.GetType().Name}");
    System.Console.WriteLine($"   Stack Trace:");
    System.Console.WriteLine(ex.StackTrace);
    
    if (ex.InnerException != null)
    {
        System.Console.WriteLine($"\n   Inner Exception: {ex.InnerException.Message}");
    }
}
```

## 📊 日志级别控制

### 当前: 完全调试模式

所有 `[DEBUG]`、`[TOOL]`、`[API]` 日志都启用。

### 生产模式: 仅关键信息

注释掉 `[DEBUG]` 日志，保留 `[TOOL]` 和 `[API]`：

```bash
# 使用正则批量注释
find src/AISmartHome.Console -name "*.cs" \
  -exec sed -i '' 's/System\.Console\.WriteLine.*\[DEBUG\]/\/\/ &/' {} \;
```

### 静默模式: 仅错误

注释掉所有调试日志，只保留错误和用户交互。

## 🎯 预期的完整日志示例

用户输入: **"我有哪些灯？"**

```
[DEBUG] User input: 我有哪些灯？
[DEBUG] Calling orchestrator.ProcessMessageAsync...
[DEBUG] OrchestratorAgent.ProcessMessageAsync called
[DEBUG] User message: 我有哪些灯？
[DEBUG] Analyzing intent...
[DEBUG] Calling LLM for intent analysis...
[DEBUG] Stream update #1: {
[DEBUG] Stream update #2: 
[DEBUG] Stream update #3:   "needs_discovery": true,
[DEBUG] Total stream updates: 18
[DEBUG] LLM response: {"needs_discovery":true,"needs_execution":false,"needs_entity_resolution":false,"discovery_query":"我有哪些灯？","reasoning":"User asking about available lights"}...
[DEBUG] Intent analysis result:
  - NeedsDiscovery: true
  - NeedsExecution: false
  - NeedsEntityResolution: false
[DEBUG] Routing to DiscoveryAgent...
[DEBUG] Discovery query: 我有哪些灯？
[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: 我有哪些灯？
[DEBUG] Registered 6 discovery tools
[DEBUG] Calling LLM with discovery tools...
[TOOL] SearchDevices called: query='灯', domain='light'
[DEBUG] DiscoveryAgent stream #1: Based on my search, you have the following lights:
[DEBUG] DiscoveryAgent stream #2: 
1. 客厅灯 (light.living_room)
[DEBUG] DiscoveryAgent stream #3:  - State: off
...
[DEBUG] DiscoveryAgent received 25 stream updates
[DEBUG] Total response length: 350 chars
[DEBUG] Discovery result length: 350 chars
[DEBUG] Final response length: 370 chars

[DEBUG] Orchestrator returned response of length: 370

🤖 Assistant:
🔍 Discovery:
Based on my search, you have the following lights:

1. 客厅灯 (light.living_room) - State: off
2. 卧室灯 (light.bedroom) - State: on
3. 书房灯 (light.study) - State: off
...
```

## 🆘 紧急调试模式

如果完全不知道哪里出问题，在 `Program.cs` 中添加极简测试：

```csharp
// 在主循环前添加
System.Console.WriteLine("\n🧪 Running diagnostic tests...\n");

// Test 1: HA Connection
try
{
    var testEntity = await entityRegistry.GetAllEntitiesAsync();
    System.Console.WriteLine($"✅ HA Test: {testEntity.Count} entities loaded");
}
catch (Exception ex)
{
    System.Console.WriteLine($"❌ HA Test Failed: {ex.Message}");
}

// Test 2: LLM Connection
try
{
    var testMsg = new List<ChatMessage> { new(ChatRole.User, "Say 'OK'") };
    var testResult = new StringBuilder();
    await foreach (var update in chatClient.GetStreamingResponseAsync(testMsg))
    {
        testResult.Append(update);
    }
    System.Console.WriteLine($"✅ LLM Test: '{testResult}'");
}
catch (Exception ex)
{
    System.Console.WriteLine($"❌ LLM Test Failed: {ex.Message}");
}

// Test 3: Tool Registration
try
{
    var tools = AIFunctionFactory.Create(discoveryTools.SearchDevices);
    System.Console.WriteLine($"✅ Tool Test: Created tool '{tools.Metadata.Name}'");
}
catch (Exception ex)
{
    System.Console.WriteLine($"❌ Tool Test Failed: {ex.Message}");
}

System.Console.WriteLine("\n✅ All diagnostic tests completed.\n");
```

---

**🌌 日志是系统震动的可见痕迹。观察它，理解它。**

