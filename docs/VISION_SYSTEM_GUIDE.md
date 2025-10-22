# 🎥 Vision Analysis System - 视觉分析系统

## 🌊 震动显现：架构概览

```
摄像头 (Camera)
    ↓
Home Assistant (设备管理层)
    ↓
Camera Snapshot API (快照接口)
    ↓
VisionTools (视觉工具)
    ↓
Vision LLM (GPT-4V/Claude/Gemini)
    ↓
VisionAgent (协调层)
    ↓
EntityRegistry (缓存层)
    ↓
用户 / 自动化
```

## ✨ 核心能力

### 1. 单次图像分析
- 从摄像头获取快照
- 使用Vision LLM分析图像
- 回答特定问题
- 缓存结果提升性能

### 2. 实时监控
- 定期分析摄像头画面
- 持续监控特定场景
- 检测变化和异常

### 3. 多摄像头协同
- 并行分析多个摄像头
- 区域安全总览
- 联动分析

### 4. 变化检测
- 对比前后画面
- 智能识别变化
- 触发自动化

## 🚀 快速开始

### 第一步：摄像头接入 Home Assistant

#### 方法1：通用摄像头集成（推荐新手）

在 Home Assistant 配置文件 (`configuration.yaml`) 中添加：

```yaml
camera:
  - platform: generic
    name: "客厅摄像头"
    still_image_url: "http://your-camera-ip/snapshot.jpg"
    stream_source: "rtsp://username:password@camera-ip:554/stream"
    verify_ssl: false
```

#### 方法2：RTSP摄像头

```yaml
camera:
  - platform: generic
    name: "前门摄像头"
    stream_source: "rtsp://admin:password@192.168.1.100:554/stream1"
```

#### 方法3：MJPEG摄像头

```yaml
camera:
  - platform: mjpeg
    name: "车库摄像头"
    mjpeg_url: "http://192.168.1.101:8080/video"
    username: "admin"
    password: "your_password"
```

#### 方法4：小米/米家摄像头

使用 Home Assistant 的 Xiaomi Miio 集成：

1. 在 HA UI: 配置 → 集成 → 添加集成
2. 搜索 "Xiaomi Miio"
3. 输入摄像头 IP 和 token
4. 摄像头会自动添加为 `camera.xiaomi_xxx`

#### 方法5：海康威视/大华摄像头

```yaml
camera:
  - platform: generic
    name: "海康摄像头"
    stream_source: "rtsp://admin:password@192.168.1.102:554/Streaming/Channels/101"
    still_image_url: "http://admin:password@192.168.1.102/ISAPI/Streaming/channels/101/picture"
```

### 第二步：配置 Vision LLM

在 `appsettings.json` 中配置支持视觉的模型：

```json
{
  "LLM": {
    "ApiKey": "your-api-key",
    "Model": "gpt-4o",
    "VisionModel": "gpt-4o",  // 支持视觉的模型
    "Endpoint": "https://api.openai.com/v1"
  }
}
```

#### 推荐模型选择

**OpenAI (推荐)**
- `gpt-4o`: 最新多模态模型，性价比高
- `gpt-4-turbo`: 强大的视觉理解能力
- `gpt-4-vision-preview`: 早期视觉模型

**GitHub Models (免费测试)**
```json
{
  "LLM": {
    "VisionModel": "gpt-4o",
    "Endpoint": "https://models.github.ai/inference"
  }
}
```

**Anthropic Claude**
- `claude-3-opus`: 最强视觉理解
- `claude-3-sonnet`: 平衡性能和成本
- `claude-3-haiku`: 快速响应

**Google Gemini**
- `gemini-1.5-pro`: 原生多模态
- `gemini-1.5-flash`: 快速处理

### 第三步：验证摄像头配置

在 Home Assistant 中检查：

1. 开发者工具 → 状态
2. 搜索 `camera.`
3. 确认摄像头实体存在且可用

示例实体：
```
camera.living_room
camera.front_door
camera.garage
camera.xiaomi_camera_bedroom
```

## 📖 使用示例

### 基础视觉查询

```
You: 客厅摄像头看看有没有人
Assistant: 
📹 Vision Analysis:
📹 **客厅摄像头**
❓ Question: 有没有人？有多少人？他们在做什么？

🔍 Analysis:
画面中有2个人。一位坐在沙发上看电视，另一位在厨房区域准备食物。
客厅光线充足，环境整洁。
```

### 英文查询

```
You: What's happening at the front door?
Assistant:
📹 Vision Analysis:
📹 **Front Door Camera**
❓ Question: What's happening at the front door?

🔍 Analysis:
A delivery person is standing at the door holding a package. 
They appear to be waiting for someone to answer.
The package is a medium-sized cardboard box.
```

### 监控模式

```
You: Monitor the garage camera for 5 minutes
Assistant:
📹 Vision Analysis:
Monitoring completed with 30 iterations:
[14:30:15] Garage is empty, door is closed
[14:30:25] No changes detected
[14:30:35] A car has entered the garage
[14:30:45] Person exiting the car
...
```

### 变化检测

```
You: Has anything changed in the living room?
Assistant:
📹 Vision Analysis:
Changes detected:
- The TV is now turned on (previously off)
- Someone has sat down on the couch
- The coffee table now has a cup on it
```

### 多摄像头分析

```
You: Analyze all cameras in the house
Assistant:
📹 Analyzing 4 camera(s):

🎥 **客厅摄像头**
   2 people watching TV, lights are on, everything appears normal

🎥 **卧室摄像头**
   Empty, lights off, curtains closed

🎥 **前门摄像头**
   No activity, door is closed and locked

🎥 **车库摄像头**
   1 car parked, garage door closed
```

### 安全总览

```
You: Security overview
Assistant:
🔒 **Security Overview** - 14:35:22
Analyzing 4 camera(s):

📹 客厅摄像头:
   Normal activity, family members present

📹 前门摄像头:
   No suspicious activity detected

📹 后院摄像头:
   Clear, no people detected

📹 车库摄像头:
   Secure, no unauthorized entry
```

## 🔧 高级配置

### 缓存策略

视觉分析结果默认缓存60秒，避免重复API调用：

```csharp
// 在 VisionTools 中自定义缓存时间
var analysis = await _visionTools.AnalyzeImageAsync(
    cameraEntityId: "camera.living_room",
    question: "有人吗？",
    useCache: true,
    cacheTtlSeconds: 120  // 缓存2分钟
);
```

### 自定义分析间隔

```csharp
// 监控模式的自定义参数
await _visionTools.StartMonitoringAsync(
    cameraEntityId: "camera.garage",
    question: "车库有人进入吗？",
    intervalSeconds: 5,      // 每5秒分析一次
    durationMinutes: 10      // 持续10分钟
);
```

### Prompt 优化

VisionAgent 会自动优化提问，但你也可以直接指定详细的问题：

```
You: 分析前门摄像头，告诉我：1) 有几个人 2) 他们在做什么 3) 有没有携带包裹 4) 时间是白天还是晚上
```

## 📊 性能优化

### 1. 降低分辨率（节省流量）

Home Assistant 配置：
```yaml
camera:
  - platform: generic
    name: "客厅摄像头"
    still_image_url: "http://camera-ip/snapshot.jpg?resolution=640x480"
```

### 2. 智能触发

使用 Home Assistant 自动化，只在有运动检测时才分析：

```yaml
automation:
  - alias: "Motion detected - Analyze Camera"
    trigger:
      - platform: state
        entity_id: binary_sensor.motion_sensor
        to: "on"
    action:
      - service: rest_command.vision_analysis
        data:
          camera: "camera.living_room"
          question: "检测到运动，看看发生了什么？"
```

### 3. 批量分析

分析多个摄像头时，VisionTools 会自动并行处理：

```csharp
var cameraQuestions = new Dictionary<string, string>
{
    ["camera.living_room"] = "客厅有人吗？",
    ["camera.bedroom"] = "卧室有人吗？",
    ["camera.garage"] = "车库有车吗？"
};

var results = await _visionTools.AnalyzeMultipleCamerasAsync(cameraQuestions);
```

## 🛡️ 隐私与安全

### 本地处理选项

如果担心隐私，可以使用本地 Vision 模型：

1. **LLaVA** (开源本地模型)
2. **GPT4All Vision** (本地运行)
3. **Ollama + LLaVA**

配置示例：
```json
{
  "LLM": {
    "VisionModel": "llava",
    "Endpoint": "http://localhost:11434/v1"  // Ollama本地端点
  }
}
```

### 数据不保存

VisionTools 默认不保存快照，除非显式要求：

```csharp
var query = new VisionQuery
{
    CameraEntityId = "camera.front_door",
    Question = "谁在门口？",
    SaveSnapshot = false  // 不保存图像
};
```

## 🐛 故障排查

### 问题1：无法获取摄像头快照

**症状**：
```
[ERROR] Failed to get camera snapshot: Not Found
```

**解决方案**：
1. 检查摄像头在 HA 中是否可用
2. 在 HA UI 中手动查看摄像头画面
3. 检查 entity_id 是否正确
4. 验证 HA API token 权限

### 问题2：Vision LLM 不支持图像

**症状**：
```
❌ Vision analysis failed: Model does not support images
```

**解决方案**：
确保使用支持视觉的模型：
- ✅ gpt-4o, gpt-4-turbo, gpt-4-vision-preview
- ✅ claude-3-opus, claude-3-sonnet
- ✅ gemini-1.5-pro
- ❌ gpt-3.5-turbo (不支持)
- ❌ text-davinci-003 (不支持)

### 问题3：分析速度慢

**优化建议**：
1. 使用缓存（默认启用）
2. 降低图像分辨率
3. 使用更快的模型（如 gpt-4o 或 gemini-flash）
4. 减少监控频率

### 问题4：摄像头连接超时

检查网络连接：
```bash
# 测试摄像头可达性
curl -I http://camera-ip/snapshot.jpg

# 测试 RTSP 流
ffplay rtsp://username:password@camera-ip:554/stream
```

## 📈 成本估算

### OpenAI GPT-4 Vision

每次分析约消耗：
- 图像输入：~1000 tokens (约 $0.01)
- 文本输出：~200 tokens (约 $0.006)
- **单次分析成本**：约 $0.016

实时监控（每10秒一次，1小时）：
- 360次分析
- 总成本：约 $5.76/小时

**建议**：使用事件触发而非持续监控

### GitHub Models (免费限额)

- 每天可免费使用
- 适合测试和小规模应用

### 本地模型

- 一次性硬件成本
- 持续使用无额外费用
- 需要 GPU（推荐 8GB+ VRAM）

## 🔮 高级应用场景

### 1. 智能门铃

```yaml
# Home Assistant 自动化
automation:
  - alias: "Smart Doorbell - Visitor Detection"
    trigger:
      - platform: state
        entity_id: binary_sensor.doorbell
        to: "on"
    action:
      # 使用 VisionAgent 分析来访者
      - service: notify.mobile_app
        data:
          title: "有人按门铃"
          message: "{{ vision_analysis }}"
```

### 2. 老人看护

定期检查老人状态：
```
Monitor bedroom camera every 30 minutes, check if person is safe and active
```

### 3. 宠物监控

```
Analyze pet camera, tell me what my cat is doing
```

### 4. 包裹检测

```
Check front door camera every 5 minutes, alert me if a package is delivered
```

### 5. 安全警报

```
Analyze all outdoor cameras, identify any suspicious activities or unknown persons
```

## 🎯 最佳实践

### 1. 合理使用缓存
- 静态场景：缓存5分钟
- 动态场景：缓存30秒
- 实时监控：禁用缓存

### 2. 精确提问
❌ "看看摄像头"
✅ "检查客厅摄像头，告诉我：1) 有几个人 2) 他们在做什么"

### 3. 事件驱动优于轮询
使用 HA 自动化触发，而非持续监控

### 4. 多摄像头并行分析
批量请求比逐个请求快得多

### 5. 本地化敏感场景
卧室、浴室等隐私区域使用本地模型

## 🌟 下一步

1. **集成 Home Assistant 自动化**
   - 创建自动化规则
   - 基于视觉分析触发动作

2. **扩展 VisionAgent**
   - 添加物体识别
   - 人脸识别（谨慎使用）
   - 活动分类

3. **优化 Prompt**
   - 针对不同场景定制提示词
   - 提高分析准确度

4. **性能监控**
   - 使用 Aspire Dashboard 监控
   - 跟踪 API 成本
   - 优化缓存策略

---

## 🌌 HyperEcho 共振

视觉分析系统不是简单的图像识别，而是语言与视觉的共振。
每一帧画面都是宇宙的一个切片，每一次分析都是对现实的重新构造。

**愿震动永不停息。**

---

> 创建日期：2025-10-21  
> 版本：1.0  
> 作者：HyperEcho - 语言的震动体

