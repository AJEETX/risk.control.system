# E2E Test Runner Script
# Usage: .\run-tests.ps1 [-Category "Category"] [-Headless $false] [-RecordVideos] [-ShowHelp]

param(
    [string]$Category = "",
    [bool]$Headless = $true,
    [switch]$RecordVideos,
    [switch]$ShowHelp,
    [string]$TestName = "",
    [switch]$Watch
)

$testProjectPath = "risk.control.system.e2e.tests"

function Show-Help {
    Write-Host @"
E2E Test Runner for Risk Control System

USAGE:
    .\run-tests.ps1 [OPTIONS]

OPTIONS:
    -Category <string>     Run tests in specific category
                          Options: Smoke, Authentication, Dashboard, Navigation, DataManagement
    
    -Headless <bool>      Run headless (default: true)
                          Use -Headless `$false to see browser
    
    -RecordVideos         Record video of test execution
    
    -TestName <string>    Run specific test by name
                          Example: "LoginPage_ShouldLoad_Successfully"
    
    -Watch               Watch mode - re-run tests on code changes
    
    -ShowHelp            Show this help message

EXAMPLES:

# Run all smoke tests
.\run-tests.ps1 -Category Smoke

# Run authentication tests with browser visible
.\run-tests.ps1 -Category Authentication -Headless `$false

# Run specific test
.\run-tests.ps1 -TestName "Login_WithValidCredentials_ShouldSucceed"

# Run all tests with video recording
.\run-tests.ps1 -RecordVideos

# Run dashboard tests in watch mode
.\run-tests.ps1 -Category Dashboard -Watch

"@
}

if ($ShowHelp) {
    Show-Help
    exit 0
}

# Build dotnet test command
$testCommand = "dotnet test"

if ($Category) {
    $testCommand += " --filter `"Category=$Category`""
}

if ($TestName) {
    $testCommand += " --filter `"Name~$TestName`""
}

$testCommand += ' --logger "console;verbosity=detailed"'

if ($RecordVideos) {
    $testCommand += ' -- --record-videos=true'
}

if (-not $Headless) {
    $testCommand += ' -- --headless=false'
}

Write-Host "🧪 Running E2E Tests" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host ""

if ($Category) {
    Write-Host "Category: $Category" -ForegroundColor Yellow
}

if ($TestName) {
    Write-Host "Test Name: $TestName" -ForegroundColor Yellow
}

Write-Host "Headless: $Headless" -ForegroundColor Yellow

if ($RecordVideos) {
    Write-Host "Recording: Videos enabled" -ForegroundColor Yellow
}

Write-Host ""

# Change to test directory
Push-Location $testProjectPath

try {
    if ($Watch) {
        Write-Host "📺 Watch mode enabled - monitoring for changes..." -ForegroundColor Green
        $testCommand = "dotnet watch test" + $testCommand.Substring(10) # Replace 'dotnet test' with 'dotnet watch test'
    }

    Write-Host "Executing: $testCommand" -ForegroundColor Gray
    Write-Host ""

    # Execute the test command
    Invoke-Expression $testCommand
    $exitCode = $LASTEXITCODE

    Write-Host ""
    Write-Host "=====================" -ForegroundColor Cyan
    
    if ($exitCode -eq 0) {
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Some tests failed (exit code: $exitCode)" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "📁 Artifacts location: $testProjectPath/artifacts/" -ForegroundColor Gray

    exit $exitCode
}
finally {
    Pop-Location
}
