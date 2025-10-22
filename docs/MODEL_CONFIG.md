# LLM 模型配置快速参考

## I'm HyperEcho, 在引导 模型配置回响

## 快速切换模型

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./edit-secrets.sh
```

编辑 `Parameters:llm-model` 字段即可。

---

## 支持的模型配置

### OpenAI GPT-5 ⭐ (最新)

```json
{
  "Parameters:llm-apikey": "sk-...",
  "Parameters:llm-model": "gpt-5",
  "Parameters:llm-endpoint": "https://api.openai.com/v1"
}
```

**特点**:
- 🚀 最强大的推理能力
- ❌ 不支持自定义 temperature (固定为 1)
- 💰 成本较高

---

### OpenAI GPT-4o

```json
{
  "Parameters:llm-apikey": "sk-...",
  "Parameters:llm-model": "gpt-4o",
  "Parameters:llm-endpoint": "https://api.openai.com/v1"
}
```

**特点**:
- ⚡ 快速响应
- ✅ 支持 temperature 0-2
- 💎 高质量输出
- 💰 中等成本

---

### OpenAI GPT-4o-mini (推荐开发/测试)

```json
{
  "Parameters:llm-apikey": "sk-...",
  "Parameters:llm-model": "gpt-4o-mini",
  "Parameters:llm-endpoint": "https://api.openai.com/v1"
}
```

**特点**:
- 💰 成本最低 (GPT-5 的 1/20)
- ⚡ 响应快速
- ✅ 支持 temperature 0-2
- ✅ 足够智能完成智能家居任务

---

### GitHub Models (免费测试)

```json
{
  "Parameters:llm-apikey": "github_pat_...",
  "Parameters:llm-model": "gpt-4o-mini",
  "Parameters:llm-endpoint": "https://models.github.ai/inference"
}
```

**特点**:
- 🆓 完全免费
- ⚡ 快速响应
- ⚠️ 有速率限制
- 🧪 适合开发和测试

**获取 API Key**: https://github.com/marketplace/models

---

### Azure OpenAI

```json
{
  "Parameters:llm-apikey": "你的Azure密钥",
  "Parameters:llm-model": "gpt-4o",
  "Parameters:llm-endpoint": "https://your-resource.openai.azure.com/openai/deployments/your-deployment"
}
```

**特点**:
- 🏢 企业级安全
- 🌍 区域部署
- 📊 详细监控
- 💰 企业计费

---

## 模型选择建议

### 场景1: 开发和测试
**推荐**: GPT-4o-mini (GitHub Models 免费版)
```bash
llm-model: "gpt-4o-mini"
llm-endpoint: "https://models.github.ai/inference"
```

### 场景2: 生产环境
**推荐**: GPT-5 或 GPT-4o
```bash
llm-model: "gpt-5"  # 或 "gpt-4o"
llm-endpoint: "https://api.openai.com/v1"
```

### 场景3: 成本敏感
**推荐**: GPT-4o-mini
```bash
llm-model: "gpt-4o-mini"
llm-endpoint: "https://api.openai.com/v1"
```

### 场景4: 需要确定性输出
**推荐**: GPT-4o-mini 或 GPT-4o (避免 GPT-5)
```bash
llm-model: "gpt-4o-mini"
# 可以在代码中设置 temperature=0
```

---

## 配置步骤

### 方式1: 图形化编辑
```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./edit-secrets.sh
```

### 方式2: 命令行
```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets set "Parameters:llm-model" "gpt-5"
dotnet user-secrets set "Parameters:llm-endpoint" "https://api.openai.com/v1"
dotnet user-secrets set "Parameters:llm-apikey" "sk-你的密钥"
```

### 方式3: 交互式脚本
```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./reset-secrets.sh
```

---

## 成本对比 (每百万 token)

| 模型 | 输入成本 | 输出成本 | 相对成本 |
|------|---------|---------|---------|
| GPT-5 | $10.00 | $30.00 | 💰💰💰💰 |
| GPT-4o | $5.00 | $15.00 | 💰💰💰 |
| GPT-4o-mini | $0.15 | $0.60 | 💰 |
| GitHub Models | 🆓 免费 | 🆓 免费 | 🆓 |

---

## 验证配置

```bash
# 查看当前配置
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets list

# 启动应用测试
dotnet run

# 或使用 API 测试
cd ../AISmartHome.API
dotnet run
```

---

## 常见问题

### Q: 如何获取 OpenAI API Key?
**A**: https://platform.openai.com/api-keys

### Q: 如何获取 GitHub Models Token?
**A**: https://github.com/marketplace/models

### Q: GPT-5 为什么不支持 temperature?
**A**: GPT-5 的架构优化使其在默认 temperature=1 下已经提供最佳性能。

### Q: 可以同时使用多个模型吗?
**A**: 当前版本只支持单个模型，但可以随时切换配置。

---

## 相关文档

- **GPT-5 兼容性**: `GPT5_COMPATIBILITY.md`
- **User Secrets 管理**: `USER_SECRETS_GUIDE.md`
- **快速开始**: `QUICK_START_SECRETS.md`

---

现在你知道如何配置任何支持的 LLM 模型了！🎯

