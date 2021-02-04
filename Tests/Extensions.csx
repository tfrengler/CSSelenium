#load "Core.csx"

using TFrengler.Selenium;
using OpenQA.Selenium;

// Extension check
Uri ChromeDriver = Webdrivers.Start(Browser.CHROME);
Selenium = new SeleniumWrapper(Browser.CHROME, ChromeDriver);

Selenium.Webdriver.Navigate().GoToUrl("https://www.selenium.dev/documentation/en/");

var MenuItemsContainer = Selenium.GetElement.ByClass("topics", "ul");
var MenuChildren = MenuItemsContainer.GetDirectChildren();
var MenuDescendants = MenuItemsContainer.GetDescendants();

WriteLine("MenuChildren count: " + MenuChildren.Count);
WriteLine("MenuDescendants count: " + MenuDescendants.Count);

foreach(var Child in MenuChildren)
    if (Child.TagName != "li")
    {
        Webdrivers.Dispose();
        throw new Exception("Expected menu children to only be <li>'s but this isn't " + Child.TagName);
    }

WriteLine("So far so good...");

if (MenuDescendants.Count == MenuChildren.Count)
{
    Webdrivers.Dispose();
    throw new Exception($"Expected menu descendants count to not be equal to menu children but it is ({MenuDescendants.Count} vs {MenuChildren.Count})");
}

WriteLine("So far so good 2...");

var LabelElement = Selenium.GetElement.ById("tab1code2");
var PreviousSibling = LabelElement.GetPreviousSiblingElement();

WriteLine("So far so good 3...");
WriteLine("PreviousSibling.TagName: " + PreviousSibling.TagName);

if (PreviousSibling.TagName != "label")
{
    Webdrivers.Dispose();
    throw new Exception("Expected previous sibling to be a <label> element but instead it's a " + PreviousSibling.TagName);
}

WriteLine("So far so good 4...");

var NextSibling = LabelElement.GetNextSiblingElement();
if (NextSibling.TagName != "label")
{
    Webdrivers.Dispose();
    throw new Exception("Expected previous sibling to be a <label> element but instead it's a " + PreviousSibling.TagName);
}

WriteLine("So far so good 5...");

var ParentElement = LabelElement.GetParent();
WriteLine("ParentElement.TagName: " + ParentElement.TagName);
WriteLine("ParentElement class: " + ParentElement.GetAttribute("class"));

if (ParentElement.TagName != "div" && ParentElement.GetAttribute("class") != "tabset")
{
    Webdrivers.Dispose();
    throw new Exception("Expected parent to be a <div> element but instead it's a " + PreviousSibling.TagName);
}

WriteLine("Press key to exit...");
Console.ReadKey();

var Success = Webdrivers.Stop(Browser.CHROME);
WriteLine("Driver stopped? " + Success);
Webdrivers.Dispose();