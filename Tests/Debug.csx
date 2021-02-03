#r "../bin/Debug/netcoreapp3.1/CSSelenium.dll"
#r "../bin/Debug/netcoreapp3.1/WebDriver.dll"
#r "System.IO.FileSystem"

using TFrengler.Selenium;

var BrowserDriverFolder = new DirectoryInfo(@"C:\Users\thoma\source\repos\GemDenHaag\BrowserDrivers");
var Webdrivers = new WebdriverManager(BrowserDriverFolder);

Uri Location = Webdrivers.Start(Browser.CHROME);
Uri Location2 = Webdrivers.Start(Browser.CHROME);

var Selenium = new SeleniumWrapper(Browser.CHROME, Location);
Selenium.Webdriver.Navigate().GoToUrl("https://www.nu.nl");

WriteLine(Location);
WriteLine("Press key to exit...");
Console.ReadKey();

Selenium.Webdriver.Quit();
var Success = Webdrivers.Stop(Browser.CHROME);
WriteLine(Success);