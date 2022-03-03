using NUnit.Framework;
using TFrengler.CSSelenium;
using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;

namespace Tests
{
    [TestFixture]
    public class SeleniumFactoryTests
    {
        public DirectoryInfo TempStaticBrowserDriverFolder;
        DriverService[] Drivers;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            Drivers = new DriverService[3];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                TempStaticBrowserDriverFolder = new DirectoryInfo(@"C:\Temp\Webdrivers");
            }
            else
            {
                TempStaticBrowserDriverFolder = new DirectoryInfo(@"~/Temp/Webdrivers");
            }

            if (!TempStaticBrowserDriverFolder.Exists)
                throw new Exception($"Error setting up unit tests for {this.GetType().Name} | Temp static webdriver folder does not exist: {TempStaticBrowserDriverFolder.FullName}");

            lock(Drivers.SyncRoot)
            {
                DriverService Service = ChromeDriverService.CreateDefaultService(TempStaticBrowserDriverFolder.FullName);
                Drivers[(int)Browser.CHROME] = Service;
                Service.Start();

                Service = FirefoxDriverService.CreateDefaultService(TempStaticBrowserDriverFolder.FullName);
                Drivers[(int)Browser.FIREFOX] = Service;
                Service.Start();

                Service = EdgeDriverService.CreateDefaultService(TempStaticBrowserDriverFolder.FullName);
                Drivers[(int)Browser.EDGE] = Service;
                Service.Start();
            }
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            foreach(var Service in Drivers)
            {
                Service?.Dispose();
            }

            System.Threading.Thread.Sleep(2000);

            var AllProcesses = Process.GetProcesses();
            var BrowserDriverProcessNames = new string[] { "chromedriver", "geckodriver", "msedgedriver" };

            foreach(var CurrentProcess in AllProcesses)
            {
                if (Array.IndexOf(BrowserDriverProcessNames, CurrentProcess.ProcessName) > -1)
                    CurrentProcess.Kill(true);
            }
        }

        #region CREATE STANDARD

        [TestCase(Category="CreateVanilla")]
        public void Create_Chrome_1()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.CHROME];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(Browser.CHROME, DriverURL));
            Webdriver.Quit();
        }

        [TestCase(Category="CreateVanilla")]
        public void Create_Firefox()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.FIREFOX];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(Browser.FIREFOX, DriverURL));
            Webdriver.Quit();
        }

        [TestCase(Category="CreateVanilla")]
        public void Create_Edge()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.EDGE];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(Browser.EDGE, DriverURL));
            Webdriver.Quit();
        }

        #endregion
        #region CREATE WITH BROWSER ARGUMENTS

        [TestCase(Category="CreateWithArguments")]
        public void Create_Chrome_WithArguments()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.CHROME];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(Browser.CHROME, DriverURL, new string[] { "--headless" }));
            Webdriver.Quit();
        }

        [TestCase(Category="CreateWithArguments")]
        public void Create_Firefox_WithArguments()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.FIREFOX];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(Browser.FIREFOX, DriverURL, new string[] { "-headless" }));
            Webdriver.Quit();
        }

        [TestCase(Category="CreateWithArguments")]
        public void Create_Edge_WithArguments()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.EDGE];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(Browser.EDGE, DriverURL, new string[] { "--headless" }));
            Webdriver.Quit();
        }

        #endregion
        #region CREATE WITH OPTIONS

        [TestCase(Category="CreateWithOptions")]
        public void Create_Chrome_WithOptions()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.CHROME];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(DriverURL, new ChromeOptions()));
            Webdriver.Quit();
        }

        [TestCase(Category="CreateWithOptions")]
        public void Create_Firefox_WithOptions()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.FIREFOX];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(DriverURL, new FirefoxOptions()));
            Webdriver.Quit();
        }

        [TestCase(Category="CreateWithOptions")]
        public void Create_Edge_WithOptions()
        {
            DriverService Service;
            Uri DriverURL;

            lock(Drivers.SyncRoot)
            {
                Service = Drivers[(int)Browser.EDGE];
                DriverURL = Service.ServiceUrl;
            }

            IWebDriver Webdriver = null;
            Assert.DoesNotThrow(()=> Webdriver = SeleniumFactory.Create(DriverURL, new EdgeOptions()));
            Webdriver.Quit();
        }

        #endregion
    }
}