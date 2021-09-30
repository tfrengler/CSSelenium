#r "../bin/Debug/netcoreapp3.1/CSSelenium.dll"
#r "../bin/Debug/netcoreapp3.1/publish/WebDriver.dll"
#r "../bin/Debug/netcoreapp3.1/publish/WebDriver.Support.dll"
#r "../bin/Debug/netcoreapp3.1/publish/System.Text.Encoding.CodePages.dll"
#r "System.IO.FileSystem"

using TFrengler.Selenium;
using OpenQA.Selenium;

// Core check
var BrowserDriverFolder = new DirectoryInfo(@"C:/temp/");
var Webdrivers = new WebdriverManager(BrowserDriverFolder);
SeleniumWrapper Selenium;