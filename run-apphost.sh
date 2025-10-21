#!/bin/bash

# Quick start script for AI Smart Home AppHost

echo "🚀 Starting AI Smart Home with .NET Aspire"
echo "=========================================="
echo ""

# Check if user secrets are configured
cd src/AISmartHome.AppHost

if ! dotnet user-secrets list | grep -q "homeassistant-token"; then
    echo "⚠️  User secrets not configured!"
    echo ""
    echo "Running setup script..."
    ./setup-secrets.sh
    echo ""
fi

echo "🔄 Starting AppHost..."
echo ""
echo "📊 Aspire Dashboard will open at: http://localhost:15888"
echo "🌐 Web UI will be available at: http://localhost:5000"
echo ""
echo "Press Ctrl+C to stop"
echo ""

dotnet run

