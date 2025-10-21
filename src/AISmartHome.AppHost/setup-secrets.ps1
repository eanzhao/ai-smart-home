# AISmartHome.AppHost User Secrets Setup Script (PowerShell)
# This script helps you configure the necessary secrets for running the AppHost

Write-Host "🔐 AI Smart Home - User Secrets Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the right directory
if (-not (Test-Path "AISmartHome.AppHost.csproj")) {
    Write-Host "❌ Error: Please run this script from the AISmartHome.AppHost directory" -ForegroundColor Red
    exit 1
}

# Function to set a secret
function Set-UserSecret {
    param(
        [string]$Key,
        [string]$Description,
        [bool]$IsSecret = $false
    )
    
    Write-Host ""
    Write-Host "📝 $Description" -ForegroundColor Yellow
    
    if ($IsSecret) {
        $secureValue = Read-Host "Enter value (hidden)" -AsSecureString
        $value = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureValue)
        )
    } else {
        $value = Read-Host "Enter value"
    }
    
    if ($value) {
        dotnet user-secrets set $Key $value
        Write-Host "✅ Set $Key" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Skipped (empty value)" -ForegroundColor Yellow
    }
}

Write-Host "This script will help you configure the required secrets."
Write-Host "Press Enter to skip any optional configuration."
Write-Host ""

# Required secrets
Set-UserSecret -Key "Parameters:homeassistant-token" -Description "Home Assistant Long-Lived Access Token" -IsSecret $true
Set-UserSecret -Key "Parameters:llm-apikey" -Description "LLM API Key" -IsSecret $true

# Optional configurations
Write-Host ""
Write-Host "Optional configurations (press Enter to use defaults):" -ForegroundColor Cyan
Write-Host ""

$ha_url = Read-Host "Home Assistant URL [http://homeassistant.local:8123]"
if ($ha_url) {
    dotnet user-secrets set "Parameters:homeassistant-url" $ha_url
    Write-Host "✅ Set custom Home Assistant URL" -ForegroundColor Green
}

$llm_model = Read-Host "LLM Model [gpt-4o-mini]"
if ($llm_model) {
    dotnet user-secrets set "Parameters:llm-model" $llm_model
    Write-Host "✅ Set custom LLM model" -ForegroundColor Green
}

$llm_endpoint = Read-Host "LLM Endpoint [https://api.openai.com/v1]"
if ($llm_endpoint) {
    dotnet user-secrets set "Parameters:llm-endpoint" $llm_endpoint
    Write-Host "✅ Set custom LLM endpoint" -ForegroundColor Green
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "✅ Setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Current secrets:" -ForegroundColor Cyan
dotnet user-secrets list
Write-Host ""
Write-Host "You can now run the AppHost with:" -ForegroundColor Yellow
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Or start debugging in your IDE." -ForegroundColor Yellow
Write-Host ""

