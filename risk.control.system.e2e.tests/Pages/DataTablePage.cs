using Microsoft.Playwright;

namespace risk.control.system.e2e.tests.Pages;

/// <summary>
/// Page Object Model for data tables and list views
/// </summary>
public class DataTablePage
{
    private readonly IPage _page;

    // Generic locators for tables
    private const string TableSelector = "table, [role='grid'], [role='table']";
    private const string TableRowSelector = "tbody tr, [role='row']";
    private const string SearchInputSelector = "input[type='search'], input[placeholder*='Search'], input[placeholder*='search']";
    private const string PaginationSelector = ".pagination, [role='navigation']";
    private const string AddButtonSelector = "button:has-text('Add'), button:has-text('Create'), button:has-text('New')";
    private const string EditButtonSelector = "button:has-text('Edit'), a:has-text('Edit')";
    private const string DeleteButtonSelector = "button:has-text('Delete'), a:has-text('Delete')";
    private const string FilterSelector = ".filter, [data-testid='filter']";

    public DataTablePage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Gets the number of rows in the table
    /// </summary>
    public async Task<int> GetRowCount()
    {
        try
        {
            var rows = await _page.QuerySelectorAllAsync(TableRowSelector);
            return rows.Count;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets all table data
    /// </summary>
    public async Task<List<Dictionary<string, string>>> GetTableData()
    {
        return await TestHelper.GetTableData(_page, TableSelector);
    }

    /// <summary>
    /// Searches for a value in the table
    /// </summary>
    public async Task Search(string searchValue)
    {
        try
        {
            await _page.FillAsync(SearchInputSelector, searchValue);
            await _page.DispatchEventAsync(SearchInputSelector, "change");
            await Task.Delay(1000);
        }
        catch
        {
            // Search not available
        }
    }

    /// <summary>
    /// Clears search filter
    /// </summary>
    public async Task ClearSearch()
    {
        try
        {
            await _page.FillAsync(SearchInputSelector, "");
            await _page.DispatchEventAsync(SearchInputSelector, "change");
            await Task.Delay(1000);
        }
        catch
        {
            // Search not available
        }
    }

    /// <summary>
    /// Clicks the Add/Create button
    /// </summary>
    public async Task ClickAddButton()
    {
        try
        {
            await _page.ClickAsync(AddButtonSelector);
            await Task.Delay(1000);
        }
        catch
        {
            throw new Exception("Add button not found");
        }
    }

    /// <summary>
    /// Clicks Edit on a row by row index
    /// </summary>
    public async Task ClickEditInRow(int rowIndex)
    {
        var selector = $"{TableRowSelector}:nth-child({rowIndex + 1}) {EditButtonSelector}";
        await _page.ClickAsync(selector);
        await Task.Delay(1000);
    }

    /// <summary>
    /// Clicks Delete on a row by row index
    /// </summary>
    public async Task ClickDeleteInRow(int rowIndex)
    {
        var selector = $"{TableRowSelector}:nth-child({rowIndex + 1}) {DeleteButtonSelector}";
        await _page.ClickAsync(selector);
        await Task.Delay(1000);
    }

    /// <summary>
    /// Clicks a cell in a row by row and column index
    /// </summary>
    public async Task ClickCell(int rowIndex, int columnIndex)
    {
        var selector = $"{TableRowSelector}:nth-child({rowIndex + 1}) td:nth-child({columnIndex + 1})";
        await _page.ClickAsync(selector);
    }

    /// <summary>
    /// Gets text from a specific cell
    /// </summary>
    public async Task<string?> GetCellText(int rowIndex, int columnIndex)
    {
        var selector = $"{TableRowSelector}:nth-child({rowIndex + 1}) td:nth-child({columnIndex + 1})";
        return await _page.TextContentAsync(selector);
    }

    /// <summary>
    /// Navigates to next page
    /// </summary>
    public async Task GoToNextPage()
    {
        try
        {
            await _page.ClickAsync(".pagination a:has-text('Next'), [aria-label='Next Page']");
            await Task.Delay(1000);
        }
        catch
        {
            throw new Exception("Next page button not found");
        }
    }

    /// <summary>
    /// Navigates to previous page
    /// </summary>
    public async Task GoToPreviousPage()
    {
        try
        {
            await _page.ClickAsync(".pagination a:has-text('Previous'), [aria-label='Previous Page']");
            await Task.Delay(1000);
        }
        catch
        {
            throw new Exception("Previous page button not found");
        }
    }

    /// <summary>
    /// Checks if table is empty
    /// </summary>
    public async Task<bool> IsTableEmpty()
    {
        var rowCount = await GetRowCount();
        return rowCount == 0;
    }

    /// <summary>
    /// Checks if a specific value exists in the table
    /// </summary>
    public async Task<bool> ValueExists(string value)
    {
        try
        {
            var tableText = await _page.TextContentAsync(TableSelector);
            return tableText?.Contains(value) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sorts table by column
    /// </summary>
    public async Task SortByColumn(string columnName)
    {
        try
        {
            await _page.ClickAsync($"th:has-text('{columnName}')");
            await Task.Delay(1000);
        }
        catch
        {
            throw new Exception($"Column '{columnName}' not found");
        }
    }

    /// <summary>
    /// Applies a filter
    /// </summary>
    public async Task ApplyFilter(string filterValue)
    {
        try
        {
            await _page.ClickAsync(FilterSelector);
            await Task.Delay(500);
            await _page.FillAsync($"{FilterSelector} input", filterValue);
            await Task.Delay(1000);
        }
        catch
        {
            throw new Exception("Filter not available");
        }
    }
}
