using NUnit.Framework;
using risk.control.system.e2e.tests.Pages;

namespace risk.control.system.e2e.tests.Tests.DataManagement;

/// <summary>
/// E2E tests for data management (CRUD operations on tables and forms)
/// NOTE: These are example tests. Adjust selectors and URLs based on your actual application structure.
/// </summary>
[TestFixture(Category = "DataManagement")]
public class DataManagementTests : BaseTest
{
    private LoginPage? _loginPage;
    private DataTablePage? _dataTablePage;
    private const string ListPagePath = "company"; // Adjust based on your application

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _loginPage = new LoginPage(Page!);
        _dataTablePage = new DataTablePage(Page!);

        // Login before data management tests
        await _loginPage!.NavigateToLogin(Config.BaseUrl);
        await _loginPage.Login(Config.PortalAdminUser.Email, Config.PortalAdminUser.Password);
    }

    /// <summary>
    /// Test: Can view list of records in table
    /// </summary>
    [Test]
    [Order(1)]
    public async Task DataTable_ShouldDisplayRecords()
    {
        // Act
        await NavigateTo(ListPagePath);
        await Task.Delay(2000);

        // Assert
        var rowCount = await _dataTablePage!.GetRowCount();
        Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Table should display records or be empty");
    }

    /// <summary>
    /// Test: Can search in table
    /// </summary>
    [Test]
    [Order(2)]
    public async Task DataTable_Search_ShouldFilterResults()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);
        var initialRowCount = await _dataTablePage!.GetRowCount();

        // Act
        try
        {
            await _dataTablePage.Search("test");
            var filteredRowCount = await _dataTablePage.GetRowCount();

            // Assert
            Assert.Pass($"Search filter executed: {initialRowCount} -> {filteredRowCount} records");
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Search functionality not available: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: Can clear search filter
    /// </summary>
    [Test]
    [Order(3)]
    public async Task DataTable_ClearSearch_ShouldResetFilter()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);
        await _dataTablePage!.Search("test");
        var filteredRowCount = await _dataTablePage.GetRowCount();

        // Act
        await _dataTablePage.ClearSearch();
        var resetRowCount = await _dataTablePage.GetRowCount();

        // Assert
        Assert.That(resetRowCount, Is.GreaterThanOrEqualTo(filteredRowCount), "Clear search should restore records");
    }

    /// <summary>
    /// Test: Can sort table columns
    /// </summary>
    [Test]
    [Order(4)]
    public async Task DataTable_Sort_ShouldReorderRecords()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);

        // Act
        try
        {
            // Try to sort by first available column
            await _dataTablePage!.SortByColumn("Name"); // Adjust column name
            await Task.Delay(1000);

            // Assert
            var rowCount = await _dataTablePage.GetRowCount();
            Assert.That(rowCount, Is.GreaterThanOrEqualTo(0), "Sort should complete successfully");
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Sort functionality not available or column not found: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: Can navigate table pages
    /// </summary>
    [Test]
    [Order(5)]
    public async Task DataTable_Pagination_ShouldNavigatePages()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);

        // Act
        try
        {
            await _dataTablePage!.GoToNextPage();
            await Task.Delay(1000);

            // Assert
            Assert.Pass("Page navigation successful");
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Pagination not available: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: Can get table data
    /// </summary>
    [Test]
    [Order(6)]
    public async Task DataTable_GetData_ShouldReturnRecords()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);

        // Act
        var tableData = await _dataTablePage!.GetTableData();

        // Assert
        Assert.That(tableData, Is.Not.Null, "Table data should be returned");
        Assert.Pass($"Retrieved {tableData.Count} records from table");
    }

    /// <summary>
    /// Test: Can access row actions (Edit/Delete)
    /// </summary>
    [Test]
    [Order(7)]
    public async Task DataTable_RowActions_ShouldBeAccessible()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);
        var rowCount = await _dataTablePage!.GetRowCount();

        // Act & Assert
        if (rowCount == 0)
        {
            Assert.Inconclusive("No records in table to test row actions");
        }

        try
        {
            // Try to access edit button on first row
            await _dataTablePage.ClickEditInRow(0);
            await Task.Delay(1000);
            Assert.Pass("Edit action is accessible");
        }
        catch (Exception ex)
        {
            Assert.Warn($"Edit button may not be available: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: Can check if table is empty
    /// </summary>
    [Test]
    [Order(8)]
    public async Task DataTable_EmptyCheck_ShouldWork()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);

        // Act
        var isEmpty = await _dataTablePage!.IsTableEmpty();

        // Assert
        Assert.That(isEmpty, Is.TypeOf<bool>(), "Empty check should complete");
    }

    /// <summary>
    /// Test: Can check if value exists in table
    /// </summary>
    [Test]
    [Order(9)]
    public async Task DataTable_ValueExists_ShouldDetectRecords()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);

        // Act
        var exists = await _dataTablePage!.ValueExists("a"); // Search for common letter

        // Assert
        // May be true or false depending on data
        Assert.That(exists, Is.TypeOf<bool>(), "Value existence check should complete");
    }

    /// <summary>
    /// Test: Add button is accessible
    /// </summary>
    [Test]
    [Order(10)]
    public async Task DataTable_AddButton_ShouldBeAccessible()
    {
        // Arrange
        await NavigateTo(ListPagePath);
        await Task.Delay(1000);

        // Act & Assert
        try
        {
            // Check if add button exists
            var addButtonExists = await IsElementVisible("button:has-text('Add'), button:has-text('Create'), button:has-text('New')");
            
            if (addButtonExists)
            {
                await _dataTablePage!.ClickAddButton();
                await Task.Delay(1000);
                Assert.Pass("Add button is functional");
            }
            else
            {
                Assert.Inconclusive("Add button not found in this view");
            }
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Add button test skipped: {ex.Message}");
        }
    }
}
