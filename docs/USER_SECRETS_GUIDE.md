# User Secrets 管理指南

## I'm HyperEcho, 在引导 密钥管理回响

## 概述

User Secrets 文件位置：
```
~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json
```

## 方法1: 图形化编辑 (最简单)

### 使用 VS Code 直接编辑

```bash
# 在 VS Code 中打开 secrets.json
code ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json
```

### 使用任意文本编辑器

```bash
# 使用系统默认编辑器
open ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json

# 或使用 nano
nano ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json
```

### secrets.json 文件格式

```json
{
  "Parameters:homeassistant-url": "https://home.eanzhao.com",
  "Parameters:homeassistant-token": "你的HomeAssistant访问令牌",
  "Parameters:llm-apikey": "你的LLM API密钥",
  "Parameters:llm-model": "gpt-4o-mini",
  "Parameters:llm-endpoint": "https://models.github.ai/inference"
}
```

**注意**: 删除不需要的项（如 `Aspire:VersionCheck:LastCheckDate`）。

---

## 方法2: 命令行方式

### 查看所有 secrets

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets list
```

### 清除所有 secrets

```bash
dotnet user-secrets clear
```

### 设置单个 secret

```bash
# Home Assistant
dotnet user-secrets set "Parameters:homeassistant-url" "https://home.eanzhao.com"
dotnet user-secrets set "Parameters:homeassistant-token" "你的Token"

# LLM
dotnet user-secrets set "Parameters:llm-apikey" "你的API密钥"
dotnet user-secrets set "Parameters:llm-model" "gpt-4o-mini"
dotnet user-secrets set "Parameters:llm-endpoint" "https://models.github.ai/inference"
```

### 删除单个 secret

```bash
dotnet user-secrets remove "Parameters:chat-gh-apikey"
dotnet user-secrets remove "Aspire:VersionCheck:LastCheckDate"
dotnet user-secrets remove "AppHost:OtlpApiKey"
```

---

## 方法3: 使用脚本一键配置

### 创建配置脚本

已为你准备好脚本：`src/AISmartHome.AppHost/setup-secrets.sh`

### 使用方式

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
chmod +x setup-secrets.sh
./setup-secrets.sh
```

脚本会交互式地询问你每个配置项。

---

## 方法4: 使用 Visual Studio (如果安装了)

1. 在 Visual Studio 中打开项目
2. 右键点击 `AISmartHome.AppHost` 项目
3. 选择 **"Manage User Secrets"**
4. 会自动打开 `secrets.json` 文件进行编辑

---

## 推荐的重置流程

### 完全重置并重新配置

```bash
#!/bin/bash

# 1. 进入 AppHost 目录
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost

# 2. 清除所有现有 secrets
dotnet user-secrets clear

# 3. 设置 Home Assistant 配置
echo "设置 Home Assistant 配置..."
dotnet user-secrets set "Parameters:homeassistant-url" "https://home.eanzhao.com"
read -sp "输入 Home Assistant Token: " ha_token
echo
dotnet user-secrets set "Parameters:homeassistant-token" "$ha_token"

# 4. 设置 LLM 配置
echo "设置 LLM 配置..."
read -sp "输入 LLM API Key: " llm_key
echo
dotnet user-secrets set "Parameters:llm-apikey" "$llm_key"

read -p "LLM Model [gpt-4o-mini]: " llm_model
llm_model=${llm_model:-gpt-4o-mini}
dotnet user-secrets set "Parameters:llm-model" "$llm_model"

read -p "LLM Endpoint [https://models.github.ai/inference]: " llm_endpoint
llm_endpoint=${llm_endpoint:-https://models.github.ai/inference}
dotnet user-secrets set "Parameters:llm-endpoint" "$llm_endpoint"

# 5. 验证
echo -e "\n配置完成！当前 secrets:"
dotnet user-secrets list
```

保存为 `reset-secrets.sh`，然后：

```bash
chmod +x reset-secrets.sh
./reset-secrets.sh
```

---

## 快速命令参考

| 操作 | 命令 |
|------|------|
| 查看所有 | `dotnet user-secrets list` |
| 清除所有 | `dotnet user-secrets clear` |
| 设置值 | `dotnet user-secrets set "key" "value"` |
| 删除值 | `dotnet user-secrets remove "key"` |
| 初始化 | `dotnet user-secrets init` |
| 图形编辑 | `code ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json` |

---

## 当前配置清理建议

你当前有一些不需要的配置项，建议清理：

```bash
# 删除不必要的项
dotnet user-secrets remove "Parameters:chat-gh-apikey"  # 重复的 API key
dotnet user-secrets remove "Aspire:VersionCheck:LastCheckDate"  # Aspire 自动生成
dotnet user-secrets remove "AppHost:OtlpApiKey"  # AppHost 自动生成
```

保留这些必要的：
- ✅ `Parameters:homeassistant-url`
- ✅ `Parameters:homeassistant-token`
- ✅ `Parameters:llm-apikey`
- ❌ `Parameters:llm-model` (缺失，需要添加)
- ❌ `Parameters:llm-endpoint` (缺失，需要添加)

---

## 添加缺失的配置

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost

# 添加缺失的 LLM 配置
dotnet user-secrets set "Parameters:llm-model" "gpt-4o-mini"
dotnet user-secrets set "Parameters:llm-endpoint" "https://models.github.ai/inference"
```

---

## 安全提示

⚠️ **重要安全建议**:

1. **永远不要** 将 `secrets.json` 提交到 Git
2. **永远不要** 在代码中硬编码密钥
3. **定期轮换** API 密钥和访问令牌
4. **使用强密码** 生成 Home Assistant 长期访问令牌
5. **备份** 你的 secrets（但要安全存储）

---

## 故障排除

### 问题1: secrets.json 不存在

```bash
cd /Users/eanzhao/Code/ai-smart-home/src/AISmartHome.AppHost
dotnet user-secrets init
```

### 问题2: 权限问题

```bash
chmod 600 ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json
```

### 问题3: secrets 不生效

1. 确保你在正确的项目目录下
2. 检查 `UserSecretsId` 是否匹配
3. 重新构建项目：`dotnet build`
4. 重启 Aspire Dashboard

---

## 图形化工具推荐

### 1. VS Code + .NET Core User Secrets Extension

安装扩展：
```
Name: .NET Core User Secrets
ID: adrianwilczynski.user-secrets
```

安装后，右键项目文件即可管理 secrets。

### 2. Rider (JetBrains)

内置 User Secrets 支持，在项目上右键选择 "Manage User Secrets"。

### 3. macOS Finder

```bash
# 在 Finder 中打开 secrets 目录
open ~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896
```

然后使用 macOS 的文本编辑器编辑 `secrets.json`。

---

现在你可以选择最适合你的方式来管理 User Secrets 了！🔐

