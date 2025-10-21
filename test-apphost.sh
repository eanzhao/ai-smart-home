#!/bin/bash

echo "🧪 Testing AI Smart Home AppHost"
echo "================================"
echo ""

cd src/AISmartHome.AppHost

echo "1. Building AppHost..."
if ! dotnet build --quiet; then
    echo "❌ Build failed"
    exit 1
fi
echo "✅ Build successful"
echo ""

echo "2. Checking user secrets..."
if ! dotnet user-secrets list | grep -q "homeassistant-token"; then
    echo "⚠️  User secrets not configured. Running setup..."
    ./setup-secrets.sh
    echo ""
fi

echo "3. Current configuration:"
dotnet user-secrets list
echo ""

echo "4. Testing AppHost startup (5 second timeout)..."
timeout 5s dotnet run --no-build || {
    if [ $? -eq 124 ]; then
        echo "✅ AppHost started successfully (timeout reached)"
    else
        echo "❌ AppHost failed to start"
        exit 1
    fi
}

echo ""
echo "🎉 AppHost test completed successfully!"
echo ""
echo "To run the full AppHost:"
echo "  cd src/AISmartHome.AppHost"
echo "  dotnet run"
echo ""
echo "Or use the quick start script:"
echo "  ./run-apphost.sh"
