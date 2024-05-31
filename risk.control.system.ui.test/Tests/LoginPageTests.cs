using NUnit.Framework;
using static NUnit.Framework.TestContext;

using FluentAssertions;
using risk.control.system.ui.test.PageObject;
using risk.control.system.ui.test.Setup;

namespace risk.control.system.ui.test.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture()]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class LoginPageTests : SelfHostedPageTest<Program>
{
    public LoginPageTests() :
        base(services =>
        {
            // configure needed services, like mocked db access, fake mail service, etc.
        })
    { }
    //   [Test]
    //   public void CheckmarkTest()
    //{
    //       //Act
    //       var checkboxesPage = TheInternet
    //           .Open()
    //           .ClickCheckboxes()
    //           .SelectCheckboxOne()
    //           .UnSelectCheckboxTwo();

    //       //Assertions
    //       checkboxesPage.IsCheckBoxOneSelected().Should().BeTrue();
    //       checkboxesPage.IsCheckBoxTwoSelected().Should().BeFalse();
    //   }

    [Test]
    public void LoginTest()
    {
        //Act
        var serverAddress = GetServerAddress();
        var SecureAreaPage = TheInternet
            .Open(serverAddress)
            .ClickFormAuthentication()
            .EnterUsername(username: Parameters.Get("username"))
            .EnterPassword(password: Parameters.Get("password"))
            .ClickLogin();

        //Assertions

        SecureAreaPage.GetLogin().Should().Contain("Dashboard");
        SecureAreaPage.GetLoginStatus().Should().Contain("Reita creator");
    }
}