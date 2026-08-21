# E2E Testing Guide - Risk Control System

## 📋 Overview

A comprehensive end-to-end test suite has been created for the Risk Control System using **Playwright** with **NUnit** testing framework. This document provides a quick-start guide and overview.

## 🚀 Quick Start

### 1. Install Playwright Browsers (One-time setup)
```bash
cd risk.control.system.e2e.tests
dotnet playwright install
```

### 2. Update Test Configuration
Edit `risk.control.system.e2e.tests/Configuration/TestConfiguration.cs`:
- Update `BaseUrl` to match your application URL
- Update test user credentials (PortalAdminUser, CompanyUser, AgencyUser)

### 3. Run Tests

#### Using PowerShell (Windows):
```powershell
# Show help
.\run-e2e-tests.ps1 -ShowHelp

# Run all tests
.\run-e2e-tests.ps1

# Run smoke tests
.\run-e2e-tests.ps1 -Category Smoke

# Run tests with browser visible
.\run-e2e-tests.ps1 -Headless $false

# Run specific test
.\run-e2e-tests.ps1 -TestName "Login_WithValidCredentials_ShouldSucceed"
```

#### Using Command Prompt (Windows):
```cmd
# Run all tests
run-e2e-tests.bat

# Run smoke tests
run-e2e-tests.bat Smoke

# Run tests with browser visible
run-e2e-tests.bat Authentication headful
```

#### Using dotnet CLI (All platforms):
```bash
cd risk.control.system.e2e.tests

# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=Smoke"

# Run with browser visible
dotnet test -- --headless=false
```

## 📁 Project Structure

```
risk.control.system.e2e.tests/
├── Configuration/
│   └── TestConfiguration.cs              # Test settings and user credentials
├── Fixtures/
│   └── BaseTest.cs                       # Base class for all tests with browser setup
├── Pages/
│   ├── LoginPage.cs                      # Login page interactions
│   ├── DashboardPage.cs                  # Dashboard page interactions
│   └── DataTablePage.cs                  # Data table and list interactions
├── Helpers/
│   └── TestHelper.cs                     # Utility functions for tests
├── Tests/
│   ├── Authentication/                   # Login/logout tests
│   ├── Dashboard/                        # Dashboard functionality tests
│   ├── Navigation/                       # Page navigation tests
│   ├── DataManagement/                   # CRUD operation tests
│   └── Smoke/                            # Quick health check tests
├── README.md                             # Detailed documentation
├── launchSettings.json                   # VS launch profiles
└── risk.control.system.e2e.tests.csproj # Project file
```

## 🧪 Test Categories

### Smoke Tests
Quick tests to verify basic application functionality
- Application accessibility
- Page load without errors
- Static assets loading
- Performance baseline

**Run:** `dotnet test --filter "Category=Smoke"`

### Authentication Tests
User login, logout, and credential validation
- Valid credentials login
- Invalid credentials handling
- Password validation
- Remember Me functionality
- Session management

**Run:** `dotnet test --filter "Category=Authentication"`

### Dashboard Tests
Dashboard functionality and user interaction
- Dashboard loading
- User information display
- Navigation menu
- Responsive design
- Page refresh handling

**Run:** `dotnet test --filter "Category=Dashboard"`

### Navigation Tests
Page navigation and routing
- Browser back/forward buttons
- Direct URL navigation
- Page transitions
- Menu navigation
- Loading states

**Run:** `dotnet test --filter "Category=Navigation"`

### Data Management Tests
CRUD operations on data tables and forms
- View list of records
- Search and filter
- Sort data
- Pagination
- Edit/Delete operations

**Run:** `dotnet test --filter "Category=DataManagement"`

## ⚙️ Configuration

### Test User Credentials
Update credentials in `Configuration/TestConfiguration.cs`:

```csharp
public TestUser PortalAdminUser { get; set; } = new TestUser 
{ 
    Email = "admin@test.com", 
    Password = "TestPassword123!" 
};
```

### Browser Settings
Customize in `Configuration/TestConfiguration.cs`:
- `BaseUrl`: Application URL
- `Headless`: Run without UI (true/false)
- `BrowserType`: chromium, firefox, or webkit
- `CaptureScreenshots`: Save screenshots on failure
- `RecordVideos`: Record test execution
- `ArtifactsPath`: Output directory for artifacts

## 🔧 Key Features

### Page Object Model (POM)
Tests use the Page Object Model pattern for maintainability:
- Encapsulates UI selectors
- Provides high-level interactions
- Reduces test maintenance

Example:
```csharp
var loginPage = new LoginPage(Page);
await loginPage.Login("user@example.com", "password");
```

### Base Test Class
All tests inherit from `BaseTest` providing:
- Browser setup/teardown
- Navigation helpers
- Element interaction methods
- Screenshot/video capture
- Automatic failure logging

### Helper Utilities
`TestHelper` class provides:
- API response waiting
- Table data extraction
- Form filling
- Dialog handling
- JavaScript execution

## 📊 Test Execution Reports

Test results are generated in multiple formats:

### Console Output
Detailed test execution logs in the terminal

### TRX Format (Visual Studio)
XML format for integration with CI/CD systems
Location: `TestResults/*.trx`

### Screenshots & Videos
Captured on test failures and when enabled
Location: `artifacts/`

## 🔄 CI/CD Integration

A GitHub Actions workflow is configured in `.github/workflows/e2e-tests.yml` to:
- Run tests on push to main/develop branches
- Run nightly scheduled tests
- Generate test reports
- Upload artifacts on failure

### Enable in GitHub
1. Ensure `.github/workflows/e2e-tests.yml` is in repository
2. Configure test user credentials in GitHub Secrets
3. Adjust application startup in workflow if needed
4. Tests will automatically run on specified triggers

## 🛠️ Developing New Tests

### 1. Create Test File
```csharp
[TestFixture(Category = "YourCategory")]
public class YourTests : BaseTest
{
    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
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

### 2. Create Page Object (if needed)
```csharp
public class YourPage
{
    private readonly IPage _page;
    private const string SelectorName = ".selector";

    public YourPage(IPage page) => _page = page;

    public async Task DoSomething()
    {
        await _page.ClickAsync(SelectorName);
    }
}
```

### 3. Use Common Methods
From `BaseTest`:
- `NavigateTo(path)`
- `FillField(selector, value)`
- `ClickElement(selector)`
- `WaitForElement(selector)`
- `IsElementVisible(selector)`
- `TakeScreenshot(name)`

## 🐛 Troubleshooting

### Issue: Playwright Browsers Not Found
```bash
dotnet playwright install
```

### Issue: Tests Timeout
1. Increase timeout in `TestConfiguration.cs`
2. Verify application is running
3. Check network connectivity
4. Run with `--headless=false` to observe

### Issue: Element Not Found
1. Verify selector is correct
2. Wait for element: `await WaitForElement(selector)`
3. Take screenshot: `await TakeScreenshot("debug")`
4. Check if element is hidden

### Issue: Login Fails
1. Verify test user exists in application
2. Update credentials in configuration
3. Check authentication service is running
4. Verify correct Base URL is configured

## 📚 Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Testing Framework](https://nunit.org/)
- [Page Object Model Pattern](https://www.selenium.dev/documentation/test_practices/encouraged/page_object_models/)
- [Best Practices for E2E Testing](https://playwright.dev/dotnet/docs/best-practices)

## 📝 Next Steps

1. **Customize Configuration**: Update BaseUrl and test credentials
2. **Run Smoke Tests**: `dotnet test --filter "Category=Smoke"`
3. **Review Test Output**: Check console and artifacts
4. **Add Custom Tests**: Create tests for your specific workflows
5. **Integrate with CI/CD**: Enable GitHub Actions or your CI system

## 💡 Tips

- Use `dotnet test -- --headless=false` to watch tests execute
- Add `await Page!.PauseAsync()` in tests to pause and inspect
- Screenshots help debug failing tests
- Run tests frequently during development
- Keep tests independent and repeatable
- Use meaningful test names describing what they test

## ❓ Questions?

Refer to the comprehensive README.md in the `risk.control.system.e2e.tests` directory for detailed documentation and advanced usage.

---

**Happy Testing! 🎉**
