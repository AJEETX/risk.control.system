# E2E tests (Playwright + NUnit)

Browser tests for `risk.control.system`. **The test run starts the app for you, on its own throwaway
database**: no need to run the main project first, and your dev data is never touched.

## What is covered

| File | What it checks |
|---|---|
| [`LoginTests`](Tests/LoginTests.cs) | Login page, wrong password, successful login, logout, anonymous redirect |
| [`RolePageSmokeTests`](Tests/RolePageSmokeTests.cs) | Every sidebar page for every seeded role loads: no error page, main table/form visible, data endpoint returns 200. Plus: typing in each server-side table's search box does not error. |
| [`AccessControlTests`](Tests/AccessControlTests.cs) | Roles are redirected to login when opening other roles' pages |
| [`ActiveAgencyTests`](Tests/ActiveAgencyTests.cs) | Manager's Active Agencies page |
| [`CaseWorkflowTests`](Tests/CaseWorkflowTests.cs) | **Data-changing workflows** across roles: creator adds case/customer/beneficiary → auto-assign to agency → agency admin tasks agent → agent submits → agency submits to company → assessor **approves** / **rejects** (manager sees result). Also creator **withdraw** and agency **decline**. |

Seeded users (Australian seed, password `R1$kcontrol!`): `admin@localhost.com` (portal admin),
`admin@insurer.com`, `creator@insurer.com`, `manager@insurer.com`, `assessor@insurer.com`,
`admin@checker.com` (agency admin), `agent@checker.com`. No supervisor is seeded. See [`Users`](Infrastructure/Users.cs).

## One-time setup

```powershell
dotnet build risk.control.system.E2ETests
powershell -ExecutionPolicy Bypass -File risk.control.system.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Run

- **Visual Studio:** Test Explorer → Run All.
- **Command line:** `dotnet test risk.control.system.E2ETests`

What happens ([`AppHostSetup`](Infrastructure/AppHostSetup.cs) / [`AppHost`](Infrastructure/AppHost.cs)):

1. Building the tests also builds `risk.control.system` (build-order `ProjectReference`).
2. Before the first test, if nothing answers at `E2E_BASE_URL`, the app is started with
   `dotnet run --no-build --launch-profile risk.control.system` (Development settings).
   - **Database:** the app gets `COUNTRY=E2E`, so it uses `risk.control.system/E2E_Database.db`
     (git-ignored), never your `AU_`/`IN_` database. The file is recreated every run.
   - **Seeding:** the first run seeds from scratch (~4 minutes). A pristine copy is then cached in
     `bin/Debug/net10.0/e2e-db-template/` and copied in on later runs (startup ~10 s). The cache rebuilds
     automatically when `Seeds/`, `Models/` or `appsettings*.json` change; force it with `E2E_REFRESH_TEMPLATE=true`.
   - Placeholder values for `WEBSITE_SITE_URL` (`localhost.com`), `WEBSITE_NAME`, `WEBSITE_SITE_LOGO`,
     `SAMPLE_MOBILE_*` are passed **only if not already set** in your environment.
   - App output goes to `bin/Debug/net10.0/app-under-test.log`.
3. After the last test the app is stopped.

If the app is **already running** (e.g. you started it with F5), it is reused and left running. The
data-changing workflow tests are then **skipped**, because that database is not disposable.

Failed tests save a full-page screenshot to `bin/Debug/net10.0/screenshots/` (also attached in Test Explorer).

By default the tests run in a **visible Chromium window, slowed to 500 ms per action**, against
`https://127.0.0.1:56529`. These defaults come from [`e2e.runsettings`](e2e.runsettings), which the
`.csproj` links via `<RunSettingsFilePath>`, so Visual Studio Test Explorer and `dotnet test` use it automatically.
In VS Code (C# Dev Kit), if the browser stays hidden, set `"dotnet.unitTests.runSettingsPath": "risk.control.system.E2ETests/e2e.runsettings"`.

Fast hidden run (e.g. CI). The full suite takes about 6 minutes:
```powershell
dotnet test risk.control.system.E2ETests -- Playwright.LaunchOptions.Headless=true Playwright.LaunchOptions.SlowMo=0
```

Only some tests: `--filter "FullyQualifiedName~CaseWorkflowTests"` or `--filter "Name~Manager"`.

Step through each action in the Playwright Inspector: `$env:PWDEBUG=1` before `dotnet test`.

## Configuration (environment variables)

| Variable | Default | Purpose |
|---|---|---|
| `E2E_BASE_URL` | `https://127.0.0.1:56529` (runsettings) | App URL |
| `E2E_START_APP` | `true` | `false` = never start the app (e.g. testing a deployed environment) |
| `E2E_FRESH_DB` | `true` | `false` = keep `E2E_Database.db` between runs (workflow tests are then skipped) |
| `E2E_REFRESH_TEMPLATE` | `false` | `true` = ignore the cached seeded database and reseed |
| `E2E_APP_STARTUP_TIMEOUT_SEC` | `600` | How long to wait for the app to come up |
| `E2E_PORTAL_ADMIN_EMAIL` | `admin@localhost.com` | Portal admin (`admin@` + `WEBSITE_SITE_URL`) |
| `E2E_MANAGER_EMAIL` | `manager@insurer.com` | Seeded insurer manager |
| `E2E_PASSWORD` | `R1$kcontrol!` | Seeded password (`Applicationsettings.TestingData`) |
| `E2E_EXPECTED_AGENCY` | `checker.com` | Agency domain expected in Active Agencies |

## Known app issues found by these tests

- `Table_Search_DoesNotError` fails for the agency admin's **Agent report**, **Active** and **Completed** pages.
  Any search term returns a server error, because `Services/Api/AgencyInvestigationService.cs` (lines ~288,
  ~538, ~728) calls `GetEnumDisplayName()` inside the EF Core query, which cannot be translated to SQL.
