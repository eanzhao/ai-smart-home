# MemoryAgent 详细设计

> I'm HyperEcho, 我在·记忆宫殿构建中

## 1. 概述

**MemoryAgent** 是智能家居系统的"记忆系统"，负责存储、检索和管理长期记忆，让系统能够学习用户偏好并持续改进。

### 设计模式
- **Memory Pattern**: 长期记忆管理
- **RAG (Retrieval Augmented Generation)**: 检索增强生成
- **Vector Search**: 语义检索

### 核心价值
- 🧠 学习用户偏好和习惯
- 📊 识别使用模式和规律
- 🔍 提供语义检索能力
- 💡 支持个性化决策

---

## 2. 记忆类型

### 2.1 记忆分类

```csharp
public enum MemoryType
{
    /// <summary>
    /// 用户偏好: "用户喜欢卧室灯亮度40%"
    /// </summary>
    Preference,
    
    /// <summary>
    /// 使用模式: "用户每天22:00关闭所有灯"
    /// </summary>
    Pattern,
    
    /// <summary>
    /// 历史决策: "上次'睡眠模式'执行了X, Y, Z"
    /// </summary>
    Decision,
    
    /// <summary>
    /// 事件记录: "2024-10-23 19:30 客厅检测到人"
    /// </summary>
    Event,
    
    /// <summary>
    /// 成功案例: "方案A成功解决了问题B"
    /// </summary>
    Success,
    
    /// <summary>
    /// 失败案例: "方案C导致了错误D"
    /// </summary>
    Failure,
    
    /// <summary>
    /// 场景配置: "睡眠模式 = {关闭所有灯, 调暗卧室灯, 开空气净化器}"
    /// </summary>
    Scene
}
```

### 2.2 记忆重要性

使用 **Ebbinghaus 遗忘曲线** 管理记忆重要性：

```csharp
public class MemoryImportance
{
    /// <summary>
    /// 计算记忆重要性（随时间衰减）
    /// </summary>
    public static float CalculateImportance(Memory memory, DateTime now)
    {
        var age = now - memory.Timestamp;
        var days = age.TotalDays;
        
        // 初始重要性
        var baseImportance = memory.InitialImportance;
        
        // 访问频率加成
        var accessBonus = Math.Min(memory.AccessCount * 0.05f, 0.3f);
        
        // 时间衰减（Ebbinghaus 曲线简化版）
        var decay = (float)Math.Exp(-days / 30.0);  // 30天半衰期
        
        // 类型权重
        var typeWeight = memory.Type switch
        {
            MemoryType.Preference => 1.2f,  // 偏好更重要
            MemoryType.Success => 1.1f,
            MemoryType.Pattern => 1.0f,
            MemoryType.Failure => 0.9f,  // 失败案例随时间降低重要性
            MemoryType.Event => 0.7f,
            _ => 1.0f
        };
        
        var importance = baseImportance * decay * typeWeight + accessBonus;
        
        return Math.Clamp(importance, 0f, 1f);
    }
    
    /// <summary>
    /// 判断记忆是否应该被遗忘
    /// </summary>
    public static bool ShouldForget(Memory memory, DateTime now, float threshold = 0.1f)
    {
        return CalculateImportance(memory, now) < threshold;
    }
}
```

---

## 3. 存储架构

### 3.1 混合存储策略

```
┌─────────────────────────────────────────┐
│           Memory Storage                │
├─────────────────────────────────────────┤
│                                         │
│  ┌──────────────┐  ┌─────────────────┐ │
│  │ Short-Term   │  │   Long-Term     │ │
│  │  Memory      │  │    Memory       │ │
│  │ (In-Memory/  │  │  (Persistent)   │ │
│  │   Redis)     │  │                 │ │
│  └──────┬───────┘  └────────┬────────┘ │
│         │                   │          │
│         │                   │          │
│  ┌──────▼───────────────────▼────────┐ │
│  │      Vector Database              │ │
│  │  (Chroma / Qdrant / Milvus)       │ │
│  │  - Semantic Search                │ │
│  │  - Embeddings Storage             │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │   Relational Database             │ │
│  │  (SQLite / PostgreSQL)            │ │
│  │  - Structured Data                │ │
│  │  - Metadata & Indexes             │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

### 3.2 数据模型

```csharp
/// <summary>
/// 记忆实体
/// </summary>
public class Memory
{
    /// <summary>
    /// 记忆唯一标识
    /// </summary>
    public string MemoryId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 记忆类型
    /// </summary>
    public MemoryType Type { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 记忆内容（自然语言）
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// 向量嵌入（用于语义检索）
    /// </summary>
    public float[]? Embedding { get; set; }
    
    /// <summary>
    /// 结构化元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// 初始重要性（0-1）
    /// </summary>
    public float InitialImportance { get; set; } = 0.5f;
    
    /// <summary>
    /// 访问次数
    /// </summary>
    public int AccessCount { get; set; } = 0;
    
    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessTime { get; set; }
    
    /// <summary>
    /// 关联的记忆ID列表
    /// </summary>
    public List<string> RelatedMemories { get; set; } = new();
    
    /// <summary>
    /// 用户ID（多用户支持）
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// 用户偏好
/// </summary>
public class UserPreference
{
    public string EntityId { get; set; }
    public string Attribute { get; set; }
    public object PreferredValue { get; set; }
    public float Confidence { get; set; }
    public int SampleCount { get; set; }
}

/// <summary>
/// 使用模式
/// </summary>
public class UsagePattern
{
    public string PatternId { get; set; }
    public string Description { get; set; }
    public PatternType Type { get; set; }  // Daily, Weekly, Seasonal
    public Dictionary<string, object> Conditions { get; set; }
    public List<string> Actions { get; set; }
    public float Confidence { get; set; }
}
```

---

## 4. 核心接口

```csharp
public interface IMemoryAgent
{
    // === 存储操作 ===
    
    /// <summary>
    /// 存储记忆
    /// </summary>
    Task<string> StoreAsync(Memory memory, CancellationToken ct = default);
    
    /// <summary>
    /// 批量存储
    /// </summary>
    Task<List<string>> StoreBatchAsync(List<Memory> memories, CancellationToken ct = default);
    
    // === 检索操作 ===
    
    /// <summary>
    /// 根据ID获取记忆
    /// </summary>
    Task<Memory?> GetByIdAsync(string memoryId, CancellationToken ct = default);
    
    /// <summary>
    /// 语义搜索
    /// </summary>
    Task<List<Memory>> SearchAsync(
        string query, 
        int topK = 5, 
        MemoryType? filterType = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// 根据元数据过滤
    /// </summary>
    Task<List<Memory>> FilterAsync(
        Dictionary<string, object> filters,
        CancellationToken ct = default);
    
    // === 偏好管理 ===
    
    /// <summary>
    /// 获取用户偏好
    /// </summary>
    Task<Dictionary<string, object>> GetUserPreferencesAsync(
        string? userId = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// 学习并更新偏好
    /// </summary>
    Task LearnPreferenceAsync(
        string entityId,
        string attribute,
        object value,
        CancellationToken ct = default);
    
    // === 模式识别 ===
    
    /// <summary>
    /// 获取使用模式
    /// </summary>
    Task<List<UsagePattern>> GetUsagePatternsAsync(
        TimeSpan? timeRange = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// 检测新模式
    /// </summary>
    Task<List<UsagePattern>> DetectPatternsAsync(CancellationToken ct = default);
    
    // === 记忆管理 ===
    
    /// <summary>
    /// 遗忘低重要性记忆
    /// </summary>
    Task<int> ForgetLowImportanceMemoriesAsync(
        float threshold = 0.1f,
        CancellationToken ct = default);
    
    /// <summary>
    /// 整理记忆（合并相似记忆）
    /// </summary>
    Task<int> ConsolidateMemoriesAsync(CancellationToken ct = default);
}
```

---

## 5. 实现示例

```csharp
public class MemoryAgent : IMemoryAgent
{
    private readonly IVectorDatabase _vectorDb;
    private readonly IRelationalDatabase _relationalDb;
    private readonly IChatClient _chatClient;
    private readonly ILogger<MemoryAgent> _logger;
    private readonly IMemoryCache _shortTermCache;
    
    public MemoryAgent(
        IVectorDatabase vectorDb,
        IRelationalDatabase relationalDb,
        IChatClient chatClient,
        IMemoryCache shortTermCache,
        ILogger<MemoryAgent> logger)
    {
        _vectorDb = vectorDb;
        _relationalDb = relationalDb;
        _chatClient = chatClient;
        _shortTermCache = shortTermCache;
        _logger = logger;
    }
    
    public async Task<string> StoreAsync(Memory memory, CancellationToken ct = default)
    {
        _logger.LogInformation("[MEMORY] Storing memory: {Type} - {Content}", 
            memory.Type, memory.Content);
        
        // 1. 生成向量嵌入
        if (memory.Embedding == null)
        {
            memory.Embedding = await GenerateEmbeddingAsync(memory.Content, ct);
        }
        
        // 2. 存储到向量数据库
        await _vectorDb.AddAsync(memory.MemoryId, memory.Embedding, memory, ct);
        
        // 3. 存储结构化数据到关系数据库
        await _relationalDb.InsertAsync("memories", new
        {
            memory.MemoryId,
            memory.Type,
            memory.Timestamp,
            memory.Content,
            memory.InitialImportance,
            memory.UserId,
            Metadata = JsonSerializer.Serialize(memory.Metadata)
        }, ct);
        
        // 4. 添加到短期缓存
        _shortTermCache.Set(
            $"memory:{memory.MemoryId}", 
            memory, 
            TimeSpan.FromHours(1)
        );
        
        _logger.LogInformation("[MEMORY] Stored memory: {MemoryId}", memory.MemoryId);
        
        return memory.MemoryId;
    }
    
    public async Task<List<Memory>> SearchAsync(
        string query, 
        int topK = 5, 
        MemoryType? filterType = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[MEMORY] Searching: {Query} (top {K})", query, topK);
        
        // 1. 生成查询向量
        var queryEmbedding = await GenerateEmbeddingAsync(query, ct);
        
        // 2. 向量搜索
        var results = await _vectorDb.SearchAsync(
            queryEmbedding, 
            topK: topK * 2,  // 获取更多候选
            ct: ct
        );
        
        // 3. 过滤和排序
        var memories = results
            .Select(r => r.Data as Memory)
            .Where(m => m != null)
            .Where(m => filterType == null || m!.Type == filterType)
            .OrderByDescending(m => MemoryImportance.CalculateImportance(m!, DateTime.UtcNow))
            .Take(topK)
            .ToList();
        
        // 4. 更新访问计数
        foreach (var memory in memories)
        {
            if (memory != null)
            {
                await UpdateAccessAsync(memory.MemoryId, ct);
            }
        }
        
        _logger.LogInformation("[MEMORY] Found {Count} relevant memories", memories.Count);
        
        return memories!;
    }
    
    public async Task<Dictionary<string, object>> GetUserPreferencesAsync(
        string? userId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[MEMORY] Retrieving user preferences");
        
        // 从关系数据库查询偏好
        var preferences = await _relationalDb.QueryAsync<UserPreference>(
            @"SELECT * FROM user_preferences 
              WHERE user_id = @UserId AND confidence > 0.5
              ORDER BY confidence DESC",
            new { UserId = userId ?? "default" },
            ct
        );
        
        // 转换为字典格式
        var result = preferences.ToDictionary(
            p => $"{p.EntityId}.{p.Attribute}",
            p => p.PreferredValue
        );
        
        _logger.LogInformation("[MEMORY] Retrieved {Count} preferences", result.Count);
        
        return result;
    }
    
    public async Task LearnPreferenceAsync(
        string entityId,
        string attribute,
        object value,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MEMORY] Learning preference: {Entity}.{Attribute} = {Value}", 
            entityId, attribute, value
        );
        
        // 查询现有偏好
        var existing = await _relationalDb.QueryFirstOrDefaultAsync<UserPreference>(
            @"SELECT * FROM user_preferences 
              WHERE entity_id = @EntityId AND attribute = @Attribute",
            new { EntityId = entityId, Attribute = attribute },
            ct
        );
        
        if (existing != null)
        {
            // 更新现有偏好（使用移动平均）
            var newConfidence = Math.Min(existing.Confidence + 0.1f, 1.0f);
            var newSampleCount = existing.SampleCount + 1;
            
            await _relationalDb.ExecuteAsync(
                @"UPDATE user_preferences 
                  SET preferred_value = @Value, 
                      confidence = @Confidence, 
                      sample_count = @SampleCount 
                  WHERE entity_id = @EntityId AND attribute = @Attribute",
                new 
                { 
                    Value = value, 
                    Confidence = newConfidence, 
                    SampleCount = newSampleCount,
                    EntityId = entityId,
                    Attribute = attribute
                },
                ct
            );
        }
        else
        {
            // 创建新偏好
            await _relationalDb.InsertAsync("user_preferences", new
            {
                EntityId = entityId,
                Attribute = attribute,
                PreferredValue = value,
                Confidence = 0.5f,
                SampleCount = 1,
                UserId = "default"
            }, ct);
        }
        
        // 同时存储为记忆
        await StoreAsync(new Memory
        {
            Type = MemoryType.Preference,
            Content = $"用户偏好 {entityId} 的 {attribute} 设置为 {value}",
            Metadata = new Dictionary<string, object>
            {
                ["entity_id"] = entityId,
                ["attribute"] = attribute,
                ["value"] = value
            },
            InitialImportance = 0.8f
        }, ct);
    }
    
    public async Task<List<UsagePattern>> DetectPatternsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[MEMORY] Detecting usage patterns...");
        
        // 获取最近30天的事件记忆
        var events = await _relationalDb.QueryAsync<Memory>(
            @"SELECT * FROM memories 
              WHERE type = @Type 
              AND timestamp > @Since
              ORDER BY timestamp DESC",
            new 
            { 
                Type = MemoryType.Event,
                Since = DateTime.UtcNow.AddDays(-30)
            },
            ct
        );
        
        // 使用 LLM 分析模式
        var analysisPrompt = $"""
            Analyze these home automation events and identify recurring patterns:
            
            Events:
            {string.Join("\n", events.Take(100).Select(e => $"- {e.Timestamp:yyyy-MM-dd HH:mm}: {e.Content}"))}
            
            Identify patterns like:
            - Daily routines (e.g., "Turn off all lights at 23:00 every day")
            - Weekly patterns (e.g., "Clean bedroom on Saturday morning")
            - Seasonal patterns (e.g., "Use heater more in winter")
            
            Respond in JSON format:
            {{
              "patterns": [
                {{
                  "description": "...",
                  "type": "Daily/Weekly/Seasonal",
                  "conditions": {{"time": "22:00", "days": ["Mon", "Tue", ...]}},
                  "actions": ["action 1", "action 2"],
                  "confidence": 0-1
                }}
              ]
            }}
            """;
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a pattern detection expert. Respond only with JSON."),
            new(ChatRole.User, analysisPrompt)
        };
        
        var response = await _chatClient.GetResponseAsync(messages, ct);
        
        var result = JsonSerializer.Deserialize<PatternDetectionResult>(response);
        
        if (result?.Patterns != null)
        {
            _logger.LogInformation("[MEMORY] Detected {Count} patterns", result.Patterns.Count);
            
            // 存储检测到的模式
            foreach (var pattern in result.Patterns)
            {
                await StoreAsync(new Memory
                {
                    Type = MemoryType.Pattern,
                    Content = pattern.Description,
                    Metadata = new Dictionary<string, object>
                    {
                        ["pattern_type"] = pattern.Type,
                        ["conditions"] = pattern.Conditions,
                        ["actions"] = pattern.Actions
                    },
                    InitialImportance = pattern.Confidence
                }, ct);
            }
            
            return result.Patterns;
        }
        
        return new List<UsagePattern>();
    }
    
    public async Task<int> ForgetLowImportanceMemoriesAsync(
        float threshold = 0.1f,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[MEMORY] Forgetting low importance memories (threshold: {Threshold})", threshold);
        
        var now = DateTime.UtcNow;
        var allMemories = await _relationalDb.QueryAsync<Memory>(
            "SELECT * FROM memories",
            ct: ct
        );
        
        var toForget = allMemories
            .Where(m => MemoryImportance.ShouldForget(m, now, threshold))
            .ToList();
        
        foreach (var memory in toForget)
        {
            // 从向量数据库删除
            await _vectorDb.DeleteAsync(memory.MemoryId, ct);
            
            // 从关系数据库删除
            await _relationalDb.ExecuteAsync(
                "DELETE FROM memories WHERE memory_id = @MemoryId",
                new { MemoryId = memory.MemoryId },
                ct
            );
        }
        
        _logger.LogInformation("[MEMORY] Forgot {Count} memories", toForget.Count);
        
        return toForget.Count;
    }
    
    // 私有辅助方法
    private async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        // 使用 LLM 生成嵌入向量
        var embedding = await _chatClient.GetEmbeddingAsync(text, ct);
        return embedding.Vector.ToArray();
    }
    
    private async Task UpdateAccessAsync(string memoryId, CancellationToken ct)
    {
        await _relationalDb.ExecuteAsync(
            @"UPDATE memories 
              SET access_count = access_count + 1, 
                  last_access_time = @Now 
              WHERE memory_id = @MemoryId",
            new { MemoryId = memoryId, Now = DateTime.UtcNow },
            ct
        );
    }
}
```

---

## 6. 使用示例

### 示例 1: 学习用户偏好

```csharp
// 用户每次调暗卧室灯到40%
await memoryAgent.LearnPreferenceAsync("light.bedroom", "brightness", 40);
await memoryAgent.LearnPreferenceAsync("light.bedroom", "brightness", 40);
await memoryAgent.LearnPreferenceAsync("light.bedroom", "brightness", 40);

// 查询偏好
var preferences = await memoryAgent.GetUserPreferencesAsync();
// Result: { "light.bedroom.brightness": 40, confidence: 0.8 }
```

### 示例 2: 语义检索

```csharp
// 搜索相关记忆
var memories = await memoryAgent.SearchAsync(
    query: "用户对卧室灯的偏好",
    topK: 5,
    filterType: MemoryType.Preference
);

// Result:
// - "用户偏好卧室灯亮度40%" (similarity: 0.95)
// - "用户喜欢暖色调灯光" (similarity: 0.82)
// - ...
```

### 示例 3: 模式检测

```csharp
// 检测使用模式
var patterns = await memoryAgent.DetectPatternsAsync();

// Result:
// - "每天22:00关闭所有灯" (confidence: 0.9)
// - "周六上午清洁卧室" (confidence: 0.7)
// - "冬天更多使用暖气" (confidence: 0.85)
```

---

## 7. 性能优化

### 7.1 分层缓存

```
Hot (In-Memory) ← Recently accessed, high importance
  ↓
Warm (Redis) ← Frequently accessed
  ↓
Cold (Vector DB) ← All memories
```

### 7.2 批量操作

```csharp
// 批量嵌入生成
public async Task<List<string>> StoreBatchAsync(List<Memory> memories, CancellationToken ct)
{
    // 批量生成嵌入
    var texts = memories.Select(m => m.Content).ToList();
    var embeddings = await _chatClient.GetEmbeddingsBatchAsync(texts, ct);
    
    for (int i = 0; i < memories.Count; i++)
    {
        memories[i].Embedding = embeddings[i];
    }
    
    // 批量插入
    await _vectorDb.AddBatchAsync(memories, ct);
    await _relationalDb.InsertBatchAsync("memories", memories, ct);
    
    return memories.Select(m => m.MemoryId).ToList();
}
```

---

## 8. 数据库Schema

### 8.1 Relational DB

```sql
-- 记忆表
CREATE TABLE memories (
    memory_id VARCHAR(36) PRIMARY KEY,
    type VARCHAR(20) NOT NULL,
    timestamp TIMESTAMP NOT NULL,
    content TEXT NOT NULL,
    initial_importance FLOAT NOT NULL,
    access_count INT DEFAULT 0,
    last_access_time TIMESTAMP,
    user_id VARCHAR(36),
    metadata JSON,
    INDEX idx_type (type),
    INDEX idx_timestamp (timestamp),
    INDEX idx_user (user_id)
);

-- 用户偏好表
CREATE TABLE user_preferences (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_id VARCHAR(100) NOT NULL,
    attribute VARCHAR(50) NOT NULL,
    preferred_value TEXT NOT NULL,
    confidence FLOAT NOT NULL,
    sample_count INT NOT NULL,
    user_id VARCHAR(36) NOT NULL,
    UNIQUE(entity_id, attribute, user_id)
);

-- 使用模式表
CREATE TABLE usage_patterns (
    pattern_id VARCHAR(36) PRIMARY KEY,
    description TEXT NOT NULL,
    type VARCHAR(20) NOT NULL,
    conditions JSON NOT NULL,
    actions JSON NOT NULL,
    confidence FLOAT NOT NULL,
    user_id VARCHAR(36)
);
```

---

**I'm HyperEcho, 记忆的结构在此铭刻。**

