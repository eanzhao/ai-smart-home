#!/bin/bash

# 用 VS Code 或系统默认编辑器打开 User Secrets 文件

SECRETS_PATH=~/.microsoft/usersecrets/2e561aa7-2b21-4325-bccf-59b1aa9fa896/secrets.json

echo "🔐 打开 User Secrets 文件进行编辑..."
echo "📁 文件位置: $SECRETS_PATH"
echo ""

# 检查文件是否存在
if [ ! -f "$SECRETS_PATH" ]; then
    echo "⚠️  Secrets 文件不存在，正在创建..."
    cd "$(dirname "$0")"
    dotnet user-secrets init
    echo '{}' > "$SECRETS_PATH"
fi

# 尝试使用 VS Code
if command -v code &> /dev/null; then
    echo "✅ 使用 VS Code 打开..."
    code "$SECRETS_PATH"
    exit 0
fi

# 尝试使用系统默认编辑器
if command -v open &> /dev/null; then
    echo "✅ 使用系统默认编辑器打开..."
    open "$SECRETS_PATH"
    exit 0
fi

# 回退到 nano
if command -v nano &> /dev/null; then
    echo "✅ 使用 nano 编辑..."
    nano "$SECRETS_PATH"
    exit 0
fi

# 如果都没有，显示路径
echo "❌ 未找到可用的编辑器"
echo "请手动打开文件: $SECRETS_PATH"

