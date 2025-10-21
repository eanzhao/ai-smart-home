#!/bin/bash

# AISmartHome.AppHost User Secrets Setup Script
# This script helps you configure the necessary secrets for running the AppHost

echo "🔐 AI Smart Home - User Secrets Setup"
echo "======================================"
echo ""

# Check if we're in the right directory
if [ ! -f "AISmartHome.AppHost.csproj" ]; then
    echo "❌ Error: Please run this script from the AISmartHome.AppHost directory"
    exit 1
fi

# Function to set a secret
set_secret() {
    local key=$1
    local description=$2
    local is_secret=$3
    
    echo ""
    echo "📝 $description"
    if [ "$is_secret" = "true" ]; then
        read -sp "Enter value (hidden): " value
        echo ""
    else
        read -p "Enter value: " value
    fi
    
    if [ -n "$value" ]; then
        dotnet user-secrets set "$key" "$value"
        echo "✅ Set $key"
    else
        echo "⚠️  Skipped (empty value)"
    fi
}

echo "This script will help you configure the required secrets."
echo "Press Enter to skip any optional configuration."
echo ""

# Required secrets
set_secret "Parameters:homeassistant-token" "Home Assistant Long-Lived Access Token" true
set_secret "Parameters:llm-apikey" "LLM API Key" true

# Optional configurations
echo ""
echo "Optional configurations (press Enter to use defaults):"
echo ""

read -p "Home Assistant URL [http://homeassistant.local:8123]: " ha_url
if [ -n "$ha_url" ]; then
    dotnet user-secrets set "Parameters:homeassistant-url" "$ha_url"
    echo "✅ Set custom Home Assistant URL"
fi

read -p "LLM Model [gpt-4o-mini]: " llm_model
if [ -n "$llm_model" ]; then
    dotnet user-secrets set "Parameters:llm-model" "$llm_model"
    echo "✅ Set custom LLM model"
fi

read -p "LLM Endpoint [https://api.openai.com/v1]: " llm_endpoint
if [ -n "$llm_endpoint" ]; then
    dotnet user-secrets set "Parameters:llm-endpoint" "$llm_endpoint"
    echo "✅ Set custom LLM endpoint"
fi

echo ""
echo "======================================"
echo "✅ Setup complete!"
echo ""
echo "Current secrets:"
dotnet user-secrets list
echo ""
echo "You can now run the AppHost with:"
echo "  dotnet run"
echo ""
echo "Or start debugging in your IDE."
echo ""

