#load "Core.csx"

using TFrengler.Selenium;
using OpenQA.Selenium;

// Tools check
Uri FirefoxDriver = Webdrivers.Start(Browser.FIREFOX);
Selenium = new SeleniumWrapper(Browser.FIREFOX, FirefoxDriver);

Selenium.Webdriver.Navigate().GoToUrl("https://www.selenium.dev/documentation/en/");
var MenuItemsContainer = Selenium.GetElement.ByClass("topics", "ul");
var MenuLinks = Selenium.GetElements.Within(MenuItemsContainer).ByTagName("a");

foreach(var Link in MenuLinks)
    if (Link.TagName != "a")
    {
        Webdrivers.Dispose();
        throw new Exception("Expected all the menu links to a hyperlink but this is a " + Link.TagName);
    }

var MenuLink = Selenium.GetElement.ByTextContains("Getting started with WebDriver", "a");

if (MenuLink.TagName != "a")
{
    Webdrivers.Dispose();
    throw new Exception("Expected menu link to a hyperlink but it is a " + MenuLink.TagName);
}

var ListItems = Selenium.GetElements.ByAttributeStartsWith("data-nav-id", "/en/", "li");

if (ListItems.Count == 0)
{
    Webdrivers.Dispose();
    throw new Exception("Expected to find list items but didn't");
}

WriteLine("Press key to exit...");
Console.ReadKey();

var Success = Webdrivers.Stop(Browser.FIREFOX);
WriteLine("Driver stopped? " + Success);
Webdrivers.Dispose();