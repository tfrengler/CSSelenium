using NUnit.Framework;
using TFrengler.Selenium;
using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Diagnostics;
using System.Linq;

namespace Tests
{
    [TestFixture]
    public class WebdriverManagerTests
    {
        public WebdriverManager WebdriverManager;
        public DirectoryInfo TempStaticBrowserDriverFolder;
        public DirectoryInfo TempBrowserDriverDownloadFolder;

        public record DriverProcessInfo
        {
            public int[] Id { get; init; }
            public int Instances { get; init; }
        }

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

            var AllProcesses = Process.GetProcesses();
            var BrowserDriverProcessNames = new string[] { "chromedriver", "geckodriver", "msedgedriver" };

            foreach(var CurrentProcess in AllProcesses)
            {
                if (Array.IndexOf(BrowserDriverProcessNames, CurrentProcess.ProcessName) > -1)
                    CurrentProcess.Kill(true);
            }
        }

        [TearDown]
        public void AfterEach()
        {
            TempBrowserDriverDownloadFolder.Refresh();
            FileInfo[] AllFiles = TempBrowserDriverDownloadFolder.GetFiles();

            foreach(FileInfo CurrentFile in AllFiles)
                CurrentFile.Delete();
        }

        public DriverProcessInfo GetWebdriverProcessInfo(string name)
        {
            var RunningDriverProcesses = Process.GetProcessesByName(name);

            if (RunningDriverProcesses.Length == 0)
            {
                return default;
            }

            return new DriverProcessInfo()
            {
                Id = RunningDriverProcesses.Select(process => process.Id).ToArray(),
                Instances = RunningDriverProcesses.Length
            };
        }

        #region TESTS: Start

        [Test]
        public void Start_Chrome_Standard()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.CHROME, false, 0);
            });

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [Test]
        public void Start_Firefox_Standard()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.FIREFOX, false, 0);
            });

            var DriverProcesses = GetWebdriverProcessInfo("geckodriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [Test, Ignore("Not possible currently")]
        public void Start_Edge_Standard()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.EDGE, false, 0);
            });

            var DriverProcesses = GetWebdriverProcessInfo("msedgedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [Test]
        public void Start_Chrome_AlreadyRunning_KillExisting()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);
            WebdriverManager.Start(Browser.CHROME, false, 0);
            int ExistingPID = GetWebdriverProcessInfo("chromedriver").Id[0];

            Assert.DoesNotThrow(() => WebdriverManager.Start(Browser.CHROME, true, 0));
            var RunningDriverProcesses = GetWebdriverProcessInfo("chromedriver");

            Assert.IsTrue(RunningDriverProcesses.Instances == 1);
            Assert.IsTrue(RunningDriverProcesses.Id[0] != ExistingPID);

            WebdriverManager.Dispose();
        }

        [Test]
        public void Start_Chrome_AlreadyRunning_NoKillingExisting()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            WebdriverManager.Start(Browser.CHROME, false, 0);
            int ExistingPID = GetWebdriverProcessInfo("chromedriver").Id[0];

            Assert.Throws<WebdriverAlreadyRunningException>(()=> WebdriverManager.Start(Browser.CHROME, false, 0));

            Assert.IsNull(GetWebdriverProcessInfo("chromedriver"));
            WebdriverManager.Dispose();
        }

        [Test]
        public void Start_Chrome_DifferentPort()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            WebdriverManager.Start(Browser.CHROME, false, 7001);
            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

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

        #region TESTS: IsRunning

        [Test]
        public void Chrome_IsRunning()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.IsRunning(Browser.CHROME));
            WebdriverManager.Start(Browser.CHROME);
            Assert.IsTrue(WebdriverManager.IsRunning(Browser.CHROME));

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [Test]
        public void Firefox_IsRunning()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.IsRunning(Browser.FIREFOX));
            WebdriverManager.Start(Browser.FIREFOX);
            Assert.IsTrue(WebdriverManager.IsRunning(Browser.FIREFOX));

            var DriverProcesses = GetWebdriverProcessInfo("geckodriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [Test, Ignore("Not currently possible")]
        public void Edge_IsRunning()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.IsRunning(Browser.EDGE));
            WebdriverManager.Start(Browser.EDGE);
            Assert.IsTrue(WebdriverManager.IsRunning(Browser.EDGE));

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        #endregion

        #region TESTS: Stop

        [Test]
        public void Chrome_Stop()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.Stop(Browser.CHROME));
            WebdriverManager.Start(Browser.CHROME);
            Assert.IsTrue(WebdriverManager.Stop(Browser.CHROME));

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsNull( DriverProcesses );

            WebdriverManager.Dispose();
        }

        [Test]
        public void Firefox_Stop()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.Stop(Browser.FIREFOX));
            WebdriverManager.Start(Browser.FIREFOX);
            Assert.IsTrue(WebdriverManager.Stop(Browser.FIREFOX));

            var DriverProcesses = GetWebdriverProcessInfo("geckodriver");
            Assert.IsNull( DriverProcesses );

            WebdriverManager.Dispose();
        }

        [Test, Ignore("Not currently possible")]
        public void Edge_Stop()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.Stop(Browser.EDGE));
            WebdriverManager.Start(Browser.EDGE);
            Assert.IsTrue(WebdriverManager.Stop(Browser.EDGE));

            var DriverProcesses = GetWebdriverProcessInfo("msedgedriver");
            Assert.IsTrue( DriverProcesses.Instances == 0 );

            WebdriverManager.Dispose();
        }

        #endregion

        #region TESTS: DetermineLatestAvailableVersion

        [Test]
        public void Chrome_DetermineLatestAvailableVersion()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);
            Assert.IsNotEmpty(WebdriverManager.DetermineLatestAvailableVersion(Browser.CHROME));
            WebdriverManager.Dispose();
        }

        [Test]
        public void Firefox_DetermineLatestAvailableVersion()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);
            Assert.IsNotEmpty(WebdriverManager.DetermineLatestAvailableVersion(Browser.FIREFOX));
            WebdriverManager.Dispose();
        }

        [Test]
        public void Edge_DetermineLatestAvailableVersion()
        {
            var WebdriverManager = new WebdriverManager(TempStaticBrowserDriverFolder);
            Assert.IsNotEmpty(WebdriverManager.DetermineLatestAvailableVersion(Browser.EDGE));
            WebdriverManager.Dispose();
        }

        #endregion

        #region TESTS: GetCurrentVersion

        private void CurrentVersionTest_OK(Browser browser)
        {
            string DriverName = browser switch
            {
                Browser.CHROME => "chromedriver",
                Browser.FIREFOX => "geckodriver",
                Browser.EDGE => "msedgedriver"
            };

            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}_WINDOWS_version.txt", "DUMMY");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}_LINUX_version.txt", "DUMMY");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}.exe", "DUMMY");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}", "DUMMY");

            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);
            string WindowsVersion = WebdriverManager.GetCurrentVersion(browser, Platform.WINDOWS);
            string LinuxVersion = WebdriverManager.GetCurrentVersion(browser, Platform.LINUX);

            Assert.IsNotEmpty(WindowsVersion);
            Assert.IsNotEmpty(LinuxVersion);

            Assert.AreNotEqual(WindowsVersion, "0");
            Assert.AreNotEqual(LinuxVersion, "0");

            WebdriverManager.Dispose();
        }

        private void CurrentVersionTest_NOK_Missing_Driver(Browser browser)
        {
            string DriverName = browser switch
            {
                Browser.CHROME => "chromedriver",
                Browser.FIREFOX => "geckodriver",
                Browser.EDGE => "msedgedriver"
            };

            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}_WINDOWS_version.txt", "DUMMY");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}_LINUX_version.txt", "DUMMY");

            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);
            string WindowsVersion = WebdriverManager.GetCurrentVersion(browser, Platform.WINDOWS);
            string LinuxVersion = WebdriverManager.GetCurrentVersion(browser, Platform.LINUX);

            Assert.IsNotEmpty(WindowsVersion);
            Assert.IsNotEmpty(LinuxVersion);

            Assert.AreEqual(WindowsVersion, "0");
            Assert.AreEqual(LinuxVersion, "0");

            WebdriverManager.Dispose();
        }

        private void CurrentVersionTest_NOK_Missing_VersionFile(Browser browser)
        {
            string DriverName = browser switch
            {
                Browser.CHROME => "chromedriver",
                Browser.FIREFOX => "geckodriver",
                Browser.EDGE => "msedgedriver"
            };

            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}.exe", "DUMMY");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}", "DUMMY");

            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);
            string WindowsVersion = WebdriverManager.GetCurrentVersion(browser, Platform.WINDOWS);
            string LinuxVersion = WebdriverManager.GetCurrentVersion(browser, Platform.LINUX);

            Assert.IsNotEmpty(WindowsVersion);
            Assert.IsNotEmpty(LinuxVersion);

            Assert.AreEqual(WindowsVersion, "0");
            Assert.AreEqual(LinuxVersion, "0");

            WebdriverManager.Dispose();
        }

        [Test]
        public void Chrome_GetCurrentVersion()
        {
            CurrentVersionTest_OK(Browser.CHROME);
        }

        [Test]
        public void Firefox_GetCurrentVersion()
        {
            CurrentVersionTest_OK(Browser.FIREFOX);
        }

        [Test]
        public void Edge_GetCurrentVersion()
        {
            CurrentVersionTest_OK(Browser.EDGE);
        }

        [Test]
        public void Chrome_GetCurrentVersion_Missing_Driver()
        {
            CurrentVersionTest_NOK_Missing_Driver(Browser.CHROME);
        }

        [Test]
        public void Firefox_GetCurrentVersion_Missing_Driver()
        {
            CurrentVersionTest_NOK_Missing_Driver(Browser.FIREFOX);
        }

        [Test]
        public void Edge_GetCurrentVersion_Missing_Driver()
        {
            CurrentVersionTest_NOK_Missing_Driver(Browser.EDGE);
        }

        [Test]
        public void Chrome_GetCurrentVersion_Missing_VersionFile()
        {
            CurrentVersionTest_NOK_Missing_VersionFile(Browser.CHROME);
        }

        [Test]
        public void Firefox_GetCurrentVersion_Missing_VersionFile()
        {
            CurrentVersionTest_NOK_Missing_VersionFile(Browser.FIREFOX);
        }

        [Test]
        public void Edge_GetCurrentVersion_Missing_VersionFile()
        {
            CurrentVersionTest_NOK_Missing_VersionFile(Browser.EDGE);
        }

        #endregion

        #region TESTS: GetLatestWebdriverBinary

        [Test]
        public void Chrome_GetLatestWebdriverBinary_OK_Windows()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            WebdriverManager.GetLatestWebdriverBinary(Browser.CHROME, Platform.WINDOWS, TFrengler.Selenium.Architecture.x86);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/chromedriver.exe" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/chromedriver_WINDOWS_version.txt" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );
        }

        [Test]
        public void Chrome_GetLatestWebdriverBinary_OK_Linux()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            WebdriverManager.GetLatestWebdriverBinary(Browser.CHROME, Platform.LINUX, TFrengler.Selenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/chromedriver" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/chromedriver_LINUX_version.txt" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );
        }

        [Test]
        public void Firefox_GetLatestWebdriverBinary_OK_Windows()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            WebdriverManager.GetLatestWebdriverBinary(Browser.FIREFOX, Platform.WINDOWS, TFrengler.Selenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/geckodriver.exe" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/geckodriver_WINDOWS_version.txt" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );
        }

        [Test]
        public void Firefox_GetLatestWebdriverBinary_OK_Linux()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            WebdriverManager.GetLatestWebdriverBinary(Browser.FIREFOX, Platform.LINUX, TFrengler.Selenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/geckodriver" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/geckodriver_LINUX_version.txt" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );
        }

        [Test]
        public void Edge_GetLatestWebdriverBinary_OK()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            WebdriverManager.GetLatestWebdriverBinary(Browser.EDGE, Platform.WINDOWS, TFrengler.Selenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/msedgedriver.exe" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/msedgedriver_WINDOWS_version.txt" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );
        }

        [Test]
        public void Edge_GetLatestWebdriverBinary_NOK()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.GetLatestWebdriverBinary(Browser.EDGE, Platform.LINUX, TFrengler.Selenium.Architecture.x64);
            });
        }

        [Test]
        public void Chrome_GetLatestWebdriverBinary_NOK()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.GetLatestWebdriverBinary(Browser.CHROME, Platform.WINDOWS, TFrengler.Selenium.Architecture.x64);
            });

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.GetLatestWebdriverBinary(Browser.CHROME, Platform.LINUX, TFrengler.Selenium.Architecture.x86);
            });
        }

        [Test]
        public void IE11_GetLatestWebdriverBinary_NOK()
        {
            var WebdriverManager = new WebdriverManager(TempBrowserDriverDownloadFolder);

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.GetLatestWebdriverBinary(Browser.IE11, Platform.WINDOWS, TFrengler.Selenium.Architecture.x86);
            });
        }

        #endregion
    }
}