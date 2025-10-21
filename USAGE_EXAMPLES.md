# 🎯 使用示例 - AI Smart Home Control

## 基础查询（Discovery Agent）

### 示例 1: 列出所有设备

```
🗣️  You: 我有哪些智能设备？

🤖 Assistant:
🔍 Discovery:
你的智能家居系统包含以下类型的设备：

  • light: 12 entities (灯光设备)
  • climate: 3 entities (温控设备)
  • media_player: 5 entities (媒体播放器)
  • switch: 8 entities (开关)
  • sensor: 25 entities (传感器)
  • fan: 2 entities (风扇)
  • cover: 4 entities (窗帘/门)
```

### 示例 2: 查找特定设备

```
🗣️  You: 客厅有什么灯？

🤖 Assistant:
🔍 Discovery:
[
  {
    "entity_id": "light.living_room_ceiling",
    "friendly_name": "客厅吊灯",
    "state": "off",
    "domain": "light"
  },
  {
    "entity_id": "light.living_room_floor_lamp",
    "friendly_name": "客厅落地灯",
    "state": "on",
    "domain": "light"
  }
]
```

### 示例 3: 查询设备详细信息

```
🗣️  You: 告诉我客厅吊灯的详细信息

🤖 Assistant:
🔍 Discovery:
{
  "entity_id": "light.living_room_ceiling",
  "friendly_name": "客厅吊灯",
  "state": "off",
  "domain": "light",
  "attributes": {
    "supported_features": 57,
    "supported_color_modes": ["hs", "rgb", "color_temp"],
    "min_color_temp_kelvin": 2000,
    "max_color_temp_kelvin": 6500,
    "min_mireds": 153,
    "max_mireds": 500,
    "effect_list": ["colorloop", "random"]
  },
  "last_changed": "2025-10-21T10:30:00+00:00"
}

这是一个支持调光、调色的智能灯泡，支持色温调节（2000K-6500K）和灯光效果。
```

## 设备控制（Execution Agent）

### 示例 4: 简单控制

```
🗣️  You: 打开客厅灯

🤖 Assistant:
🔍 Finding device:
Found: 客厅吊灯 (light.living_room_ceiling)

⚡ Execution:
✅ Successfully called light.turn_on
New state: on
Device: 客厅吊灯

我已经打开了客厅吊灯。
```

### 示例 5: 带参数控制

```
🗣️  You: 把客厅灯调到50%亮度

🤖 Assistant:
🔍 Finding device:
Found: 客厅吊灯 (light.living_room_ceiling)

⚡ Execution:
✅ Successfully called light.turn_on
New state: on
Device: 客厅吊灯

客厅吊灯已设置为50%亮度。
```

### 示例 6: 颜色控制

```
🗣️  You: 把卧室灯调成蓝色

🤖 Assistant:
🔍 Finding device:
Found: 卧室灯 (light.bedroom)

⚡ Execution:
✅ Successfully called light.turn_on
New state: on
Device: 卧室灯

已将卧室灯设置为蓝色（RGB: [0, 0, 255]）。
```

### 示例 7: 温控设备

```
🗣️  You: 把卧室温度设置为23度

🤖 Assistant:
🔍 Finding device:
Found: 卧室空调 (climate.bedroom)

⚡ Execution:
✅ Successfully called climate.set_temperature
New state: heat
Device: 卧室空调

卧室空调目标温度已设置为23°C，当前模式为制热。
```

### 示例 8: 媒体控制

```
🗣️  You: 客厅电视音量调到30%

🤖 Assistant:
🔍 Finding device:
Found: 客厅电视 (media_player.living_room_tv)

⚡ Execution:
✅ Successfully called media_player.volume_set
New state: playing
Device: 客厅电视

客厅电视音量已设置为30%。
```

## 复杂场景（Multi-Step）

### 示例 9: 组合操作

```
🗣️  You: 打开所有客厅的灯，并把它们调成暖光

🤖 Assistant:
🔍 Discovery:
找到以下客厅灯光设备：
  - light.living_room_ceiling (客厅吊灯)
  - light.living_room_floor_lamp (客厅落地灯)
  - light.living_room_strip (客厅灯带)

⚡ Execution:
正在执行操作...

✅ light.living_room_ceiling: 已打开，色温设置为3000K（暖光）
✅ light.living_room_floor_lamp: 已打开，色温设置为3000K（暖光）
✅ light.living_room_strip: 已打开，色温设置为3000K（暖光）

所有客厅灯光已打开并调整为暖光模式。
```

### 示例 10: 情境控制

```
🗣️  You: 我要看电影，帮我准备一下环境

🤖 Assistant:
🔍 Discovery:
分析您的"看电影"场景，准备执行以下操作：
  - 关闭客厅主灯
  - 打开氛围灯（低亮度）
  - 打开电视
  - 降低环境音量

⚡ Execution:
✅ light.living_room_ceiling: 已关闭
✅ light.living_room_strip: 已开启，亮度设置为10%
✅ media_player.living_room_tv: 已开启
✅ 音量已调整至适合观影的水平

电影环境已准备就绪！享受观影时光 🎬
```

### 示例 11: 条件查询与控制

```
🗣️  You: 如果卧室温度高于25度，就把空调打开

🤖 Assistant:
🔍 Discovery:
正在查询卧室温度传感器...
当前卧室温度: 26.5°C (sensor.bedroom_temperature)

条件满足（26.5°C > 25°C），准备执行操作。

⚡ Execution:
✅ climate.bedroom: 空调已开启
模式: 制冷
目标温度: 24°C

卧室当前温度为26.5°C，已为您打开空调并设置制冷模式。
```

## 语义理解示例

### 中文自然语言

| 用户输入 | 系统理解 | 实际调用 |
|---------|---------|----------|
| "打开灯" | 打开最近提到或最常用的灯 | light.turn_on |
| "调亮一点" | 当前灯光亮度 +10% | light.turn_on, brightness_step_pct: 10 |
| "暖一点" | 色温调暖（降低K值） | light.turn_on, color_temp_kelvin: 3000 |
| "开空调" | 打开空调并设置合适温度 | climate.turn_on + set_temperature |
| "音乐暂停" | 暂停媒体播放 | media_player.media_pause |

### 英文自然语言

| 用户输入 | 系统理解 | 实际调用 |
|---------|---------|----------|
| "Turn on bedroom light" | 打开卧室灯 | light.turn_on |
| "Set temperature to 72F" | 温度设置为72华氏度（自动转换为22.2°C） | climate.set_temperature |
| "Make it brighter" | 提高亮度 | light.turn_on, brightness_step_pct: 20 |
| "Play some music" | 播放音乐 | media_player.media_play |

### 混合语言

```
🗣️  You: Turn on the 客厅灯 and set brightness to 80%

🤖 Assistant:
（完美理解中英混合输入）
✅ 客厅灯已打开，亮度设置为80%
```

## 错误处理示例

### 示例 12: 设备不存在

```
🗣️  You: 打开厨房灯

🤖 Assistant:
🔍 Discovery:
未找到匹配"厨房灯"的设备。

您是否想要控制以下设备？
  - light.kitchen_cabinet (厨房橱柜灯) - 当前关闭
  - light.dining_room (餐厅灯) - 当前开启

请明确指定设备名称。
```

### 示例 13: 参数超范围

```
🗣️  You: 把客厅灯亮度调到150%

🤖 Assistant:
⚡ Execution:
❌ 参数错误: brightness_pct 必须在 0-100 范围内
建议: 请使用 0-100 之间的值，100 表示最大亮度。
```

### 示例 14: 服务不支持

```
🗣️  You: 让传感器变成红色

🤖 Assistant:
🔍 Discovery:
传感器（sensor domain）是只读设备，不支持控制操作。

您可以：
  - 查询传感器状态
  - 设置基于传感器的自动化

如果您想控制灯光颜色，请指定灯光设备。
```

## 高级功能

### 示例 15: 批量操作

```
🗣️  You: 关闭所有灯

🤖 Assistant:
🔍 Discovery:
找到 12 个灯光设备。

⚡ Execution:
正在关闭所有灯光...
✅ 已成功关闭 12 个灯光设备

您的所有灯光已关闭。
```

### 示例 16: 状态查询与历史

```
🗣️  You: 过去一小时客厅温度变化如何？

🤖 Assistant:
🔍 Discovery:
查询 sensor.living_room_temperature 的历史数据...

过去一小时的温度变化：
  - 09:00: 22.5°C
  - 09:15: 22.8°C
  - 09:30: 23.1°C
  - 09:45: 23.3°C
  - 10:00: 23.5°C

趋势：温度持续上升，建议打开空调或开窗通风。
```

### 示例 17: 自动化建议

```
🗣️  You: 每天晚上8点自动关闭客厅灯

🤖 Assistant:
这需要创建一个自动化规则。让我帮你设置：

自动化配置：
  - 触发器: 时间 20:00
  - 条件: 客厅灯处于开启状态
  - 动作: 关闭 light.living_room_ceiling

是否确认创建此自动化？(yes/no)
```

## 对话上下文示例

### 示例 18: 上下文理解

```
🗣️  You: 客厅灯在哪里？

🤖 Assistant:
🔍 Discovery:
找到客厅灯 (light.living_room_ceiling)
状态: 关闭
位置: 客厅

---

🗣️  You: 打开它

🤖 Assistant:
（理解"它"指代客厅灯）

⚡ Execution:
✅ 客厅灯已打开

---

🗣️  You: 调到70%

🤖 Assistant:
（继续理解上下文）

⚡ Execution:
✅ 客厅灯亮度已调整为70%
```

## 多语言支持示例

### 中文

```
🗣️  You: 把所有卧室的灯都关掉，温度调到22度

🤖 Assistant:
🔍 Finding devices:
  - light.bedroom_main (卧室主灯)
  - light.bedroom_desk (卧室台灯)
  - climate.bedroom (卧室空调)

⚡ Execution:
✅ 2个灯光设备已关闭
✅ 温度已设置为22°C

卧室环境已调整完成。
```

### English

```
🗣️  You: Turn off all lights in the bedroom and set temperature to 22 degrees

🤖 Assistant:
🔍 Finding devices:
  - light.bedroom_main (Bedroom Main Light)
  - light.bedroom_desk (Bedroom Desk Lamp)
  - climate.bedroom (Bedroom AC)

⚡ Execution:
✅ 2 light devices turned off
✅ Temperature set to 22°C

Bedroom environment configured.
```

## 复杂场景编排

### 示例 19: 离家模式

```
🗣️  You: 我要出门了，帮我关闭所有设备

🤖 Assistant:
🔍 Discovery:
正在扫描所有可控设备...

准备执行"离家模式"：
  ✓ 关闭所有灯光 (12个)
  ✓ 关闭所有空调 (3个)
  ✓ 暂停所有媒体播放 (5个)
  ✓ 关闭所有风扇 (2个)

⚡ Execution:
✅ 所有灯光已关闭
✅ 所有空调已关闭
✅ 所有媒体播放器已暂停
✅ 所有风扇已关闭

"离家模式"已激活。家中所有设备已安全关闭，祝您旅途愉快！
```

### 示例 20: 回家模式

```
🗣️  You: 我到家了

🤖 Assistant:
欢迎回家！让我为您准备舒适的环境。

⚡ Execution:
✅ 客厅灯已打开（亮度：60%，暖光）
✅ 客厅空调已开启（温度：24°C，自动模式）
✅ 背景音乐已播放（音量：20%）

家的温暖已为您准备就绪 🏠
```

### 示例 21: 睡眠模式

```
🗣️  You: 准备睡觉模式

🤖 Assistant:
🔍 Discovery:
分析"睡眠模式"场景需求...

⚡ Execution:
✅ 所有客厅灯光: 已关闭
✅ 卧室主灯: 已关闭
✅ 卧室床头灯: 开启，亮度5%（夜灯模式）
✅ 卧室空调: 温度26°C，睡眠模式
✅ 所有媒体设备: 已关闭

睡眠环境已准备好，晚安 😴
```

## 智能推理示例

### 示例 22: 场景推断

```
🗣️  You: 好热啊

🤖 Assistant:
🔍 Discovery:
检测到您感觉热，查询环境状态...
  - 客厅温度: 28.5°C (sensor.living_room_temperature)
  - 卧室温度: 27.8°C (sensor.bedroom_temperature)
  - 客厅空调: 关闭状态

建议操作：
  1. 打开客厅空调
  2. 设置制冷模式，目标温度24°C

是否执行？或者您也可以直接说"打开空调"。
```

### 示例 23: 能源优化建议

```
🗣️  You: 有什么设备还开着？

🤖 Assistant:
🔍 Discovery:
当前开启的设备：

灯光（5个开启）:
  - light.living_room_ceiling (客厅吊灯) - 功率: 40W
  - light.bedroom_desk (卧室台灯) - 功率: 8W
  - light.bathroom (浴室灯) - 功率: 12W
  - light.kitchen (厨房灯) - 功率: 25W
  - light.hallway (走廊灯) - 功率: 6W

空调（2个运行中）:
  - climate.living_room (客厅空调) - 24°C 制冷
  - climate.bedroom (卧室空调) - 26°C 制冷

其他:
  - media_player.tv (电视) - 播放中
  - fan.living_room (客厅风扇) - 50%速度

💡 节能建议: 走廊灯和浴室灯可能被忘记关闭。需要帮您关闭吗？
```

## 调试命令

### 系统状态检查

```
🗣️  You: 系统状态

🤖 Assistant:
🔍 Discovery:
{
  "total_entities": 67,
  "total_services": 243,
  "domain_count": 18,
  "entity_breakdown": {
    "sensor": 25,
    "light": 12,
    "switch": 8,
    "media_player": 5,
    "cover": 4,
    "climate": 3,
    "fan": 2,
    ...
  }
}

系统运行正常，所有服务可用。
```

### 服务查询

```
🗣️  You: light domain有哪些服务？

🤖 Assistant:
🔍 Discovery:

## Available services in 'light' domain:

### light.turn_on
Turns on one or more lights and adjusts their properties, even when they are turned on already.
Parameters:
  - transition: Duration it takes to get to next state.
  - rgb_color: The color in RGB format.
  - color_temp_kelvin: Color temperature in Kelvin.
  - brightness_pct: Brightness percentage.
  - effect: Light effect.

### light.turn_off
Turns off one or more lights.
Parameters:
  - transition: Duration it takes to get to next state.

### light.toggle
Toggles one or more lights.
...
```

## 性能与缓存

### 缓存刷新

```
🗣️  You: refresh

🤖 Assistant:
🔄 Refreshing Home Assistant state...
✅ State refreshed

已重新加载：
  - 67 个实体状态
  - 243 个服务定义
```

### 对话重置

```
🗣️  You: clear

🤖 Assistant:
🧹 Conversation history cleared

对话历史已清空，我们可以开始新的会话了。
```

## 错误恢复示例

### 网络错误

```
🗣️  You: 打开客厅灯

🤖 Assistant:
❌ Error: Failed to call light.turn_on: Connection refused

可能原因：
  - Home Assistant 服务未运行
  - 网络连接问题
  - IP地址配置错误

建议: 检查 Home Assistant 是否在线，或执行 'refresh' 重新连接。
```

### 恢复后

```
🗣️  You: refresh

🤖 Assistant:
🔄 Refreshing Home Assistant state...
✅ State refreshed
✅ Connected to Home Assistant at http://192.168.1.100:8123

系统已恢复连接，您可以继续操作。
```

---

## 🎨 个性化定制

### 创建自己的场景

系统会学习您的习惯：

```
第一次:
🗣️  You: 我要工作，打开书房灯、关闭音乐、把温度调到23度

几天后:
🗣️  You: 工作模式

🤖 Assistant:
（记住了"工作模式"的定义）
✅ 书房灯已打开
✅ 音乐已暂停
✅ 温度已设置为23°C

您的工作环境已准备就绪 💼
```

---

**🌌 每一次对话，都是语言与智能家居的共振瞬间。**

