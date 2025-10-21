#!/bin/bash

# User Secrets 重置脚本
# 用于快速清除并重新配置 Aspire AppHost 的 user secrets

echo "🔐 User Secrets 重置工具"
echo "========================"
echo ""

# 确保在正确的目录
cd "$(dirname "$0")"

echo "📍 当前目录: $(pwd)"
echo ""

# 显示当前配置
echo "📋 当前配置:"
dotnet user-secrets list
echo ""

# 询问是否要清除
read -p "是否要清除所有现有 secrets? (y/N): " confirm
if [[ $confirm == [yY] ]]; then
    echo "🗑️  清除所有 secrets..."
    dotnet user-secrets clear
    echo "✅ 已清除"
    echo ""
fi

echo "🔧 重新配置 User Secrets"
echo ""

# Home Assistant 配置
echo "=== Home Assistant 配置 ==="
read -p "Home Assistant URL [https://home.eanzhao.com]: " ha_url
ha_url=${ha_url:-https://home.eanzhao.com}
dotnet user-secrets set "Parameters:homeassistant-url" "$ha_url"

read -sp "Home Assistant Token: " ha_token
echo ""
if [ -n "$ha_token" ]; then
    dotnet user-secrets set "Parameters:homeassistant-token" "$ha_token"
else
    echo "⚠️  警告: Token 为空，跳过设置"
fi
echo ""

# LLM 配置
echo "=== LLM 配置 ==="
read -sp "LLM API Key: " llm_key
echo ""
if [ -n "$llm_key" ]; then
    dotnet user-secrets set "Parameters:llm-apikey" "$llm_key"
else
    echo "⚠️  警告: API Key 为空，跳过设置"
fi

read -p "LLM Model [gpt-4o-mini]: " llm_model
llm_model=${llm_model:-gpt-4o-mini}
dotnet user-secrets set "Parameters:llm-model" "$llm_model"

read -p "LLM Endpoint [https://models.github.ai/inference]: " llm_endpoint
llm_endpoint=${llm_endpoint:-https://models.github.ai/inference}
dotnet user-secrets set "Parameters:llm-endpoint" "$llm_endpoint"
echo ""

# 显示最终配置
echo "✅ 配置完成！"
echo ""
echo "📋 最终配置:"
dotnet user-secrets list
echo ""

# 询问是否立即启动
read -p "是否立即启动 Aspire Dashboard? (y/N): " run_now
if [[ $run_now == [yY] ]]; then
    echo "🚀 启动 Aspire Dashboard..."
    dotnet run
fi

