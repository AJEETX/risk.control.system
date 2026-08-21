# Risk Control System - End-to-End (E2E) Tests

This directory contains comprehensive end-to-end tests for the Risk Control System application using Playwright with NUnit.

## 📋 Overview

The E2E test suite covers:
- **Authentication Tests**: Login, logout, password validation, remember me functionality
- **Dashboard Tests**: Dashboard loading, responsiveness, user information display
- **Navigation Tests**: Page navigation, browser controls, URL handling
- **Smoke Tests**: Basic application functionality and health checks

## 🏗️ Project Structure

```
risk.control.system.e2e.tests/
├── Configuration/
│   └── TestConfiguration.cs          # Test configuration settings
├── Fixtures/
│   └── BaseTest.cs                   # Base test class with setup/teardown
├── Pages/
│   ├── LoginPage.cs                  # Login page object model
│   └── DashboardPage.cs              # Dashboard page object model
├── Helpers/
│   └── TestHelper.cs                 # Utility helper methods
├── Tests/
│   ├── Authentication/
│   │   └── AuthenticationTests.cs    # Login/logout tests
│   ├── Dashboard/
│   │   └── DashboardTests.cs         # Dashboard functionality tests
│   ├── Navigation/
│   │   └── NavigationTests.cs        # Navigation and page routing tests
│   └── Smoke/
│       └── SmokeTests.cs             # Quick smoke tests
├── launchSettings.json               # Launch profiles
└── README.md                         # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 10.0 or later
- Visual Studio 2022 or VS Code
- Playwright (automatically installed via NuGet)

### Installation

1. Navigate to the test project directory:
```bash
cd risk.control.system.e2e.tests
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Install Playwright browsers (required first time):
```bash
dotnet playwright install
```

## 🧪 Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Tests with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Category

#### Smoke Tests Only
```bash
dotnet test --filter "Category=Smoke"
```

#### Authentication Tests Only
```bash
dotnet test --filter "Category=Authentication"
```

#### Dashboard Tests Only
```bash
dotnet test --filter "Category=Dashboard"
```

#### Navigation Tests Only
```bash
dotnet test --filter "Category=Navigation"
```

### Run Tests in Headful Mode (See Browser)
```bash
dotnet test -- --headless=false
```

### Run Tests with Video Recording
```bash
dotnet test -- --record-videos=true
```

### Run Tests with Screenshots on Failure
```bash
dotnet test -- --capture-screenshots=true
```

### Run Specific Test
```bash
dotnet test --filter "Name~LoginPage_ShouldLoad_Successfully"
```

### Run Tests in Parallel
```bash
dotnet test -- --parallel
```

## ⚙️ Configuration

Edit `Configuration/TestConfiguration.cs` to customize test behavior:

```csharp
public class TestConfiguration
{
    public string BaseUrl { get; set; } = "https://localhost:7086"; // Change application URL
    public int NavigationTimeout { get; set; } = 30000;              // Navigation timeout in ms
    public int ActionTimeout { get; set; } = 10000;                  // Action timeout in ms
    public bool Headless { get; set; } = true;                       // Run without UI
    public string BrowserType { get; set; } = "chromium";            // Browser type
    public bool CaptureScreenshots { get; set; } = true;             // Save screenshots on failure
    public bool RecordVideos { get; set; } = false;                  // Record videos of test runs
    public string ArtifactsPath { get; set; } = "artifacts";         // Output directory
}
```

### Test User Credentials

Update test user credentials in `TestConfiguration.cs`:

```csharp
public TestUser PortalAdminUser { get; set; } = new TestUser 
{ 
    Email = "admin@test.com", 
    Password = "TestPassword123!" 
};
```

## 📄 Adding New Tests

### 1. Create a New Test File

```csharp
using NUnit.Framework;
using risk.control.system.e2e.tests.Fixtures;

namespace risk.control.system.e2e.tests.Tests.YourCategory;

[TestFixture(Category = "YourCategory")]
public class YourTests : BaseTest
{
    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        // Additional setup
    }

    [Test]
    public async Task YourTest_ShouldDoSomething()
    {
        // Arrange
        
        // Act
        
        // Assert
    }
}
```

### 2. Use Page Object Models

Create a new page object in `Pages/`:

```csharp
using Microsoft.Playwright;

public class MyPage
{
    private readonly IPage _page;
    private const string MyElementSelector = ".my-element";

    public MyPage(IPage page) => _page = page;

    public async Task DoSomething()
    {
        await _page.ClickAsync(MyElementSelector);
    }
}
```

### 3. Use Base Test Methods

Leverage inherited methods from `BaseTest`:

```csharp
await NavigateTo("path/to/page");      // Navigate to URL
await NavigateToHome();                // Go to home page
await FillField(selector, value);      // Fill input field
await IsElementVisible(selector);      // Check element visibility
await GetElementText(selector);        // Get element text
await ClickElement(selector);          // Click element
await WaitForElement(selector);        // Wait for element
await TakeScreenshot("name");          // Save screenshot
```

## 🐛 Debugging Tests

### Enable Headful Mode with Slowdown

```bash
dotnet test -- --headless=false --slow-mo=1000
```

### Pause Tests for Inspection

Add in your test:
```csharp
await Page!.PauseAsync();
```

### Generate Trace File

```bash
dotnet test -- --trace=on
```

## 📊 Test Results

Test results are displayed in the console. Artifacts (screenshots, videos) are saved to the `artifacts` directory.

### View Test Results

Screenshots and videos are organized by test name:
```
artifacts/
├── LoginPage_ShouldLoad_Successfully.png
├── Dashboard_ShouldLoad_AfterLogin_failure.png
└── videos/
    └── test_recording.webm
```

## 🔗 Page Object Models

The project uses the Page Object Model pattern for maintainability:

- **LoginPage.cs**: Handles login form interactions
- **DashboardPage.cs**: Handles dashboard interactions

Each page object:
- Encapsulates selectors (private constants)
- Provides high-level methods (e.g., `Login()`, `Logout()`)
- Hides implementation details

## 🎯 Best Practices

1. **Use Page Objects**: Encapsulate UI interactions in page objects
2. **Follow AAA Pattern**: Arrange, Act, Assert
3. **Use Waits Wisely**: Avoid hard delays; use explicit waits
4. **Keep Tests Independent**: Each test should be self-contained
5. **Use Meaningful Names**: Test names should describe what they test
6. **Capture Evidence**: Take screenshots/videos on failures
7. **Maintain Selectors**: Update selectors when UI changes
8. **Mock External Services**: Don't depend on external APIs

## 🚨 Troubleshooting

### Playwright Browsers Not Found
```bash
dotnet playwright install
```

### Tests Timeout
- Increase timeout in `TestConfiguration.cs`
- Check if application is running
- Verify network connectivity

### Element Not Found
- Check selector accuracy
- Verify element is loaded (wait for it)
- Take screenshot to inspect page state

### Login Fails
- Verify test user exists in application
- Update credentials in `TestConfiguration.cs`
- Check if authentication service is running

## 📚 Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Framework](https://nunit.org/)
- [Page Object Model Pattern](https://www.selenium.dev/documentation/test_practices/encouraged/page_object_models/)

## 🤝 Contributing

When adding new tests:
1. Follow existing test structure
2. Use page objects for UI interactions
3. Add meaningful test documentation
4. Ensure tests are independent and repeatable
5. Update this README with new test categories

## 📝 License

Part of Risk Control System project.

## 🆘 Support

For issues or questions:
1. Check test output for detailed error messages
2. Review Playwright logs
3. Take screenshots to understand test failures
4. Verify application is running and accessible
