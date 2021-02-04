#r "../bin/Debug/netcoreapp3.1/CSSelenium.dll"
#r "../bin/Debug/netcoreapp3.1/Publish/WebDriver.dll"
#r "../bin/Debug/netcoreapp3.1/Publish/WebDriver.Support.dll"
#r "../bin/Debug/netcoreapp3.1/Publish/System.Text.Encoding.CodePages.dll"
#r "System.IO.FileSystem"

using TFrengler.Selenium;
using OpenQA.Selenium;

// Core check
var BrowserDriverFolder = new DirectoryInfo(@"C:/temp/");
var Webdrivers = new WebdriverManager(BrowserDriverFolder);
SeleniumWrapper Selenium;