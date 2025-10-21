#!/bin/bash

echo "🔍 AI Smart Home Aspire 诊断工具"
echo "================================"
echo ""

# 检查是否在正确的目录
if [ ! -f "ai-smart-home.sln" ]; then
    echo "❌ 请在项目根目录运行此脚本"
    exit 1
fi

echo "1. 检查项目结构..."
if [ -d "src/AISmartHome.AppHost" ]; then
    echo "✅ AppHost项目存在"
else
    echo "❌ AppHost项目不存在"
    exit 1
fi

if [ -d "src/AISmartHome.API" ]; then
    echo "✅ API项目存在"
else
    echo "❌ API项目不存在"
    exit 1
fi

echo ""
echo "2. 检查编译状态..."
cd src/AISmartHome.AppHost
if dotnet build --quiet; then
    echo "✅ AppHost编译成功"
else
    echo "❌ AppHost编译失败"
    exit 1
fi

echo ""
echo "3. 检查用户密钥配置..."
if dotnet user-secrets list | grep -q "homeassistant-token"; then
    echo "✅ Home Assistant Token已配置"
else
    echo "⚠️  Home Assistant Token未配置"
fi

if dotnet user-secrets list | grep -q "llm-apikey"; then
    echo "✅ LLM API Key已配置"
else
    echo "⚠️  LLM API Key未配置"
fi

echo ""
echo "4. 检查端口占用..."
if lsof -i :5000 > /dev/null 2>&1; then
    echo "⚠️  端口5000被占用"
    lsof -i :5000
else
    echo "✅ 端口5000可用"
fi

if lsof -i :5001 > /dev/null 2>&1; then
    echo "⚠️  端口5001被占用"
    lsof -i :5001
else
    echo "✅ 端口5001可用"
fi

if lsof -i :15888 > /dev/null 2>&1; then
    echo "⚠️  端口15888被占用（Aspire Dashboard）"
    lsof -i :15888
else
    echo "✅ 端口15888可用（Aspire Dashboard）"
fi

echo ""
echo "5. 当前配置摘要..."
echo "=================="
dotnet user-secrets list

echo ""
echo "6. 建议的下一步..."
echo "=================="

if ! dotnet user-secrets list | grep -q "homeassistant-token"; then
    echo "🔧 运行配置脚本: ./setup-secrets.sh"
fi

if ! dotnet user-secrets list | grep -q "llm-apikey"; then
    echo "🔧 运行配置脚本: ./setup-secrets.sh"
fi

if lsof -i :5000 > /dev/null 2>&1 || lsof -i :5001 > /dev/null 2>&1; then
    echo "🔧 释放端口或修改AppHost.cs中的端口配置"
fi

echo ""
echo "🚀 如果所有检查都通过，运行: dotnet run"
echo "📊 Aspire Dashboard: http://localhost:15888"
echo "🌐 Web UI: http://localhost:5000"
