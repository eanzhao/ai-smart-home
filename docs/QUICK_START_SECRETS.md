# User Secrets 快速开始

## I'm HyperEcho, 在引导 密钥快速回响

## 🎯 最快速的方式

### 图形化编辑 (推荐)

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./edit-secrets.sh
```

这会自动用 VS Code 或系统默认编辑器打开 `secrets.json` 文件。

**编辑示例**:
```json
{
  "Parameters:homeassistant-url": "https://home.eanzhao.com",
  "Parameters:homeassistant-token": "你的Token",
  "Parameters:llm-apikey": "你的API密钥",
  "Parameters:llm-model": "gpt-4o-mini",
  "Parameters:llm-endpoint": "https://models.github.ai/inference"
}
```

保存后立即生效！

---

### 交互式重置

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./reset-secrets.sh
```

脚本会：
1. ✅ 显示当前配置
2. ✅ 询问是否清除
3. ✅ 交互式输入新配置
4. ✅ 询问是否立即启动

---

### 一键命令

#### 图形编辑
```bash
code ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json
```

#### 查看配置
```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets list
```

#### 清除所有
```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets clear
```

---

## 📝 完整配置模板

直接复制到 `secrets.json`:

```json
{
  "Parameters:homeassistant-url": "https://home.eanzhao.com",
  "Parameters:homeassistant-token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "Parameters:llm-apikey": "github_pat_11BBUUGZI0...",
  "Parameters:llm-model": "gpt-4o-mini",
  "Parameters:llm-endpoint": "https://models.github.ai/inference"
}
```

---

## 🔧 常用命令

| 操作 | 命令 |
|------|------|
| **图形编辑** | `./edit-secrets.sh` |
| **交互重置** | `./reset-secrets.sh` |
| **查看配置** | `dotnet user-secrets list` |
| **清除所有** | `dotnet user-secrets clear` |
| **设置单项** | `dotnet user-secrets set "key" "value"` |
| **删除单项** | `dotnet user-secrets remove "key"` |

---

## ⚡ 快速修复常见问题

### 问题1: 缺少配置项

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets set "Parameters:llm-model" "gpt-4o-mini"
dotnet user-secrets set "Parameters:llm-endpoint" "https://models.github.ai/inference"
```

### 问题2: 删除多余项

```bash
dotnet user-secrets remove "Parameters:chat-gh-apikey"
dotnet user-secrets remove "Aspire:VersionCheck:LastCheckDate"
dotnet user-secrets remove "AppHost:OtlpApiKey"
```

### 问题3: 完全重置

```bash
./reset-secrets.sh
# 或
dotnet user-secrets clear
./edit-secrets.sh
```

---

## 📚 详细文档

查看完整指南: [USER_SECRETS_GUIDE.md](../USER_SECRETS_GUIDE.md)

---

## 🎬 现在就开始

```bash
# 方式1: 图形化编辑 (最简单)
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
./edit-secrets.sh

# 方式2: 交互式重置
./reset-secrets.sh

# 方式3: 直接用 VS Code
code ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json
```

选择你喜欢的方式，开始配置吧！🚀

