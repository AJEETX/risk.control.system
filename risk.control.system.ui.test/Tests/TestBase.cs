using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using risk.control.system.ui.test.PageObject;

namespace risk.control.system.ui.test.Tests
{
    public abstract class TestBase : ContextTest
    {
        public IPage Page { get; private set; } = null!;

        internal TheInternet? TheInternet;

        [SetUp]
        public void PageSetup()
        {
            Page = Context.NewPageAsync().Result;
            TheInternet = TheInternet.Initialize(Page);
        }
    }
}
