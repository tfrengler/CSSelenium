using NUnit.Framework;
using TFrengler.Selenium;
using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace Tests
{
    [TestFixture]
    public class WebdriverManagerTests
    {
        public WebdriverManager WebdriverManager;
        public DirectoryInfo TempStaticBrowserDriverFolder;
        public DirectoryInfo TempBrowserDriverDownloadFolder;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                TempStaticBrowserDriverFolder = new DirectoryInfo(@"C:\Temp\Webdrivers");
                TempBrowserDriverDownloadFolder = new DirectoryInfo(@"C:\Temp\DownloadedWebdrivers");
            }
            else
            {
                TempStaticBrowserDriverFolder = new DirectoryInfo(@"~/Temp/Webdrivers");
                TempBrowserDriverDownloadFolder = new DirectoryInfo(@"~/Temp/DownloadedWebdrivers");
            }

            if (!TempStaticBrowserDriverFolder.Exists)
                throw new Exception($"Error setting up unit tests for {this.GetType().Name} | Temp static webdriver folder does not exist: {TempStaticBrowserDriverFolder.FullName}");

            if (!TempStaticBrowserDriverFolder.Exists)
                throw new Exception($"Error setting up unit tests for {this.GetType().Name} | Temp downloaded webdriver folder does not exist: {TempStaticBrowserDriverFolder.FullName}");
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            WebdriverManager?.Dispose();
        }

        [TearDown]
        public void AfterEach()
        {
            TempBrowserDriverDownloadFolder.Refresh();
            FileInfo[] AllFiles = TempBrowserDriverDownloadFolder.GetFiles();

            foreach(FileInfo CurrentFile in AllFiles)
                CurrentFile.Delete();
        }

        [SetUp]
        public void BeforeEach()
        {

        }

        #region TESTS: Start

        [Test, Order(1)]
        public void Start_Chrome_Standard()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.CHROME, false, 0);
            });

            WebdriverManager.Dispose();
        }

        [Test, Order(2)]
        public void Start_Firefox_Standard()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.FIREFOX, false, 0);
            });

            WebdriverManager.Dispose();
        }

        [Test, Order(3), Ignore("Not possible currently")]
        public void Start_Edge_Standard()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.EDGE, false, 0);
            });

            WebdriverManager.Dispose();
        }

        [Test, Order(4)]
        public void Start_Chrome_AlreadyRunning_KillExisting()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);
            WebdriverManager.Start(Browser.CHROME, false, 0);

            Assert.DoesNotThrow(()=> WebdriverManager.Start(Browser.CHROME, true, 0));
            WebdriverManager.Dispose();
        }

        [Test, Order(5)]
        public void Start_Chrome_AlreadyRunning_NoKillingExisting()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            WebdriverManager.Start(Browser.CHROME, false, 0);
            Assert.Throws<WebdriverAlreadyRunningException>(()=> WebdriverManager.Start(Browser.CHROME, false, 0));

            WebdriverManager.Dispose();
        }

        [Test, Order(5)]
        public void Start_Chrome_DifferentPort()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            WebdriverManager.Start(Browser.CHROME, false, 7001);
            Assert.DoesNotThrow(() =>
            {
                TcpClient Client = null;

                try
                {
                    Client = new TcpClient("127.0.0.1", 7001);
                }
                catch (SocketException)
                {
                    throw new Exception("Error pinging webdriver on address and port: 127.0.0.1:7001");
                }
                finally
                {
                    Client?.Dispose();
                    WebdriverManager.Dispose();
                }
            });
        }

        #endregion
    }
}