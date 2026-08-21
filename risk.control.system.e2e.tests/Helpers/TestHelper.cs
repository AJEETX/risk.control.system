using Microsoft.Playwright;

namespace risk.control.system.e2e.tests.Helpers;

/// <summary>
/// Helper utilities for E2E tests
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Waits for an API response matching a pattern
    /// </summary>
    public static async Task<IResponse?> WaitForApiResponse(IPage page, string urlPattern, int timeoutMs = 10000)
    {
        try
        {
            var response = await page.WaitForResponseAsync(response =>
                response.Url.Contains(urlPattern) && response.Status == 200,
                new PageWaitForResponseOptions { Timeout = timeoutMs });
            return response;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Waits for network to be idle
    /// </summary>
    public static async Task WaitForNetworkIdle(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Accepts a dialog (alert, confirm, prompt)
    /// </summary>
    public static void AcceptDialog(IPage page, string? responseText = null)
    {
        page.Dialog += async (_, dialog) =>
        {
            if (dialog.Type == DialogType.Alert || dialog.Type == DialogType.Confirm)
            {
                await dialog.AcceptAsync();
            }
            else if (dialog.Type == DialogType.Prompt && responseText != null)
            {
                await dialog.AcceptAsync(responseText);
            }
        };
    }

    /// <summary>
    /// Dismisses a dialog
    /// </summary>
    public static void DismissDialog(IPage page)
    {
        page.Dialog += async (_, dialog) =>
        {
            await dialog.DismissAsync();
        };
    }

    /// <summary>
    /// Gets all text content from a table
    /// </summary>
    public static async Task<List<Dictionary<string, string>>> GetTableData(IPage page, string tableSelector)
    {
        var result = new List<Dictionary<string, string>>();

        try
        {
            // Get headers
            var headers = await page.EvaluateAsync<List<string>>(@"
                (selector) => {
                    const table = document.querySelector(selector);
                    const headers = Array.from(table.querySelectorAll('thead th')).map(h => h.textContent.trim());
                    return headers;
                }
            ", tableSelector);

            // Get rows
            var rows = await page.EvaluateAsync<List<List<string>>>(@"
                (selector) => {
                    const table = document.querySelector(selector);
                    const rows = Array.from(table.querySelectorAll('tbody tr')).map(tr =>
                        Array.from(tr.querySelectorAll('td')).map(td => td.textContent.trim())
                    );
                    return rows;
                }
            ", tableSelector);

            // Combine headers with row data
            foreach (var row in rows)
            {
                var rowDict = new Dictionary<string, string>();
                for (int i = 0; i < headers.Count && i < row.Count; i++)
                {
                    rowDict[headers[i]] = row[i];
                }
                result.Add(rowDict);
            }
        }
        catch
        {
            // Table not found or parsing failed
        }

        return result;
    }

    /// <summary>
    /// Fills a form with provided data
    /// </summary>
    public static async Task FillForm(IPage page, Dictionary<string, string> formData)
    {
        foreach (var kvp in formData)
        {
            var selector = $"input[name='{kvp.Key}'], textarea[name='{kvp.Key}'], select[name='{kvp.Key}']";
            try
            {
                await page.FillAsync(selector, kvp.Value);
            }
            catch
            {
                // Field not found, skip
            }
        }
    }

    /// <summary>
    /// Gets element attributes
    /// </summary>
    public static async Task<string?> GetAttribute(IPage page, string selector, string attributeName)
    {
        try
        {
            return await page.GetAttributeAsync(selector, attributeName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if element has a specific CSS class
    /// </summary>
    public static async Task<bool> HasClass(IPage page, string selector, string className)
    {
        try
        {
            var classAttr = await GetAttribute(page, selector, "class");
            return classAttr?.Contains(className) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Scrolls element into view
    /// </summary>
    public static async Task ScrollIntoView(IPage page, string selector)
    {
        await page.EvaluateAsync($"() => document.querySelector('{selector}').scrollIntoView();");
    }

    /// <summary>
    /// Executes JavaScript in page context
    /// </summary>
    public static async Task<T?> ExecuteScript<T>(IPage page, string script, params object[] args)
    {
        try
        {
            return await page.EvaluateAsync<T>(script, args);
        }
        catch
        {
            return default;
        }
    }
}
