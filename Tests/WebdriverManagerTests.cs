using NUnit.Framework;
using TFrengler.Selenium;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace Tests
{
    [TestFixture]
    public class WebdriverManagerTests
    {
        public WebdriverManager WebdriverManager;
        public DirectoryInfo TempBrowserDriverFolder;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            TempBrowserDriverFolder = new DirectoryInfo(Directory.GetCurrentDirectory() + "/TempBrowserDrivers");
            TempBrowserDriverFolder.Create();
            Console.WriteLine("Temp browser driver-folder is: " + TempBrowserDriverFolder.FullName);

            Platform PlatformToUse;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                PlatformToUse = Platform.WINDOWS;
            else
                PlatformToUse = Platform.LINUX;

            WebdriverManager = new WebdriverManager(TempBrowserDriverFolder);
            WebdriverManager.GetLatestWebdriverBinary(Browser.CHROME, PlatformToUse, TFrengler.Selenium.Architecture.x64);
            WebdriverManager.GetLatestWebdriverBinary(Browser.FIREFOX, PlatformToUse, TFrengler.Selenium.Architecture.x86);
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            WebdriverManager?.Dispose();
            // TempBrowserDriverFolder.Delete(true);
        }

        #region TESTS: Start

        [Test, Order(1)]
        public void Start_Chrome_Standard()
        {
            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.CHROME, false, 0);
            });
        }

        [Test, Order(2)]
        public void Start_Firefox_Standard()
        {
            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.FIREFOX, false, 0);
            });
        }

        [Test, Order(3)]
        public void Start_Chrome_AlreadyRunning_KillExisting()
        {
            WebdriverManager.Start(Browser.CHROME, false, 0);
            Assert.DoesNotThrow(()=> WebdriverManager.Start(Browser.CHROME, true, 0));
        }

        [Test, Order(4)]
        public void Start_Chrome_AlreadyRunning_NoKillingExisting()
        {
            WebdriverManager.Start(Browser.CHROME, false, 0);
            Assert.Throws<Exception>(()=> WebdriverManager.Start(Browser.CHROME, false, 0));
        }

        #endregion
    }
}