# 🔒 Security Notice / 安全提示

## ⚠️ Important / 重要

**This repository's git history was rewritten to remove sensitive credentials.**

**本仓库的git历史已被重写以移除敏感凭证。**

---

## 🔑 Credentials That Were Exposed / 已暴露的凭证

The following credentials were accidentally committed and have been removed from git history:

以下凭证曾被意外提交，现已从git历史中移除：

1. **Home Assistant Access Token** (JWT)
   - 已暴露的令牌已失效
   - The exposed token has been invalidated
   
2. **GitHub API Personal Access Token**
   - 已暴露的密钥已轮换
   - The exposed key has been rotated

---

## ✅ Actions Taken / 已采取的措施

### 1. Git History Rewritten / Git历史已重写

使用 `git filter-branch` 从所有历史提交中移除了 `src/AISmartHome.API/appsettings.json` 文件。

Used `git filter-branch` to remove `src/AISmartHome.API/appsettings.json` from all historical commits.

### 2. .gitignore Updated / .gitignore已更新

添加了规则防止未来意外提交敏感配置文件：

Added rules to prevent accidental commits of sensitive configuration files:

```gitignore
# Sensitive configuration files
**/appsettings.json
!**/appsettings.Development.json
!**/appsettings.example.json
```

### 3. Example Configuration Provided / 提供了示例配置

创建了 `appsettings.example.json` 作为配置模板。

Created `appsettings.example.json` as a configuration template.

---

## 🛡️ Recommendations / 建议

### For Repository Owners / 对于仓库所有者

1. **Rotate All Credentials / 轮换所有凭证**
   - ✅ Home Assistant access token
   - ✅ API keys
   - ✅ Any other secrets that were in the file

2. **Force Push Required / 需要强制推送**
   ```bash
   git push --force --all
   git push --force --tags
   ```

3. **Notify Collaborators / 通知协作者**
   - 告知所有协作者历史已重写
   - 他们需要重新克隆仓库或重置本地分支

### For Collaborators / 对于协作者

If you have a local clone of this repository:

如果你有此仓库的本地克隆：

```bash
# Backup your work / 备份你的工作
git branch backup-branch

# Fetch the rewritten history / 获取重写的历史
git fetch origin

# Reset to the new history / 重置到新历史
git reset --hard origin/main

# Clean up / 清理
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

---

## 📝 Best Practices Going Forward / 未来的最佳实践

### 1. Use User Secrets / 使用用户密钥

For .NET projects, use User Secrets for local development:

对于.NET项目，使用用户密钥进行本地开发：

```bash
cd src/AISmartHome.AppHost
dotnet user-secrets set "HomeAssistant:AccessToken" "your-token"
dotnet user-secrets set "LLM:ApiKey" "your-api-key"
```

See [USER_SECRETS_GUIDE.md](USER_SECRETS_GUIDE.md) for details.

详见 [USER_SECRETS_GUIDE.md](USER_SECRETS_GUIDE.md)。

### 2. Environment Variables / 环境变量

Use environment variables in production:

在生产环境中使用环境变量：

```bash
export HomeAssistant__AccessToken="your-token"
export LLM__ApiKey="your-api-key"
```

### 3. Never Commit Secrets / 永远不要提交密钥

- Always use `.gitignore` for sensitive files
- Use `appsettings.example.json` as templates
- Keep actual `appsettings.json` files local only

---

## 🔍 Verification / 验证

To verify the file is no longer in history:

验证文件已不在历史中：

```bash
git log --all --full-history -- src/AISmartHome.API/appsettings.json
# Should return nothing / 应该没有任何输出
```

---

## 📞 Contact / 联系

If you have any questions or concerns about this security incident:

如果你对此安全事件有任何问题或疑虑：

- Open an issue in this repository
- 在此仓库中开启一个issue

---

**Updated / 更新日期**: 2025-10-21

**Status / 状态**: ✅ Resolved / 已解决

