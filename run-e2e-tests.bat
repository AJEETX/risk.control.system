@echo off
REM E2E Test Runner Script for Windows Command Prompt
REM Usage: run-e2e-tests.bat [category] [options]
REM Examples:
REM   run-e2e-tests.bat                        - Run all tests
REM   run-e2e-tests.bat Smoke                  - Run smoke tests
REM   run-e2e-tests.bat Authentication headful - Run auth tests with browser
REM   run-e2e-tests.bat help                   - Show help

setlocal enabledelayedexpansion

if "%1"=="help" (
    echo.
    echo E2E Test Runner for Risk Control System
    echo.
    echo USAGE:
    echo   run-e2e-tests.bat [CATEGORY] [OPTIONS]
    echo.
    echo CATEGORIES:
    echo   Smoke           - Quick smoke tests
    echo   Authentication  - Login/logout tests
    echo   Dashboard       - Dashboard tests
    echo   Navigation      - Navigation tests
    echo   DataManagement  - Data table and form tests
    echo   (empty)         - Run all tests
    echo.
    echo OPTIONS:
    echo   headful         - Run with browser visible (default: headless)
    echo   videos          - Record video of test execution
    echo   help            - Show this help message
    echo.
    echo EXAMPLES:
    echo   run-e2e-tests.bat
    echo   run-e2e-tests.bat Smoke
    echo   run-e2e-tests.bat Authentication headful
    echo   run-e2e-tests.bat Dashboard videos
    echo.
    goto :end
)

setlocal
cd risk.control.system.e2e.tests

echo.
echo 🧪 Running E2E Tests
echo =====================
echo.

set testCommand=dotnet test
set category=%1%
set option=%2%

if not "!category!"=="" (
    if not "!category!"=="help" (
        echo Category: !category!
        set testCommand=!testCommand! --filter "Category=!category!"
    )
)

if "!option!"=="headful" (
    echo Mode: Headful
    set testCommand=!testCommand! -- --headless=false
) else (
    echo Mode: Headless
)

if "!option!"=="videos" (
    echo Recording: Videos enabled
    set testCommand=!testCommand! -- --record-videos=true
)

set testCommand=!testCommand! --logger "console;verbosity=detailed"

echo.
echo Executing: !testCommand!
echo.

call !testCommand!
set exitCode=!ERRORLEVEL!

echo.
echo =====================
if !exitCode! equ 0 (
    echo ✅ All tests passed!
) else (
    echo ❌ Some tests failed
)
echo 📁 Artifacts location: artifacts\
echo.

endlocal
:end
exit /b %exitCode%
