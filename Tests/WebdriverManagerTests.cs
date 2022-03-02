using NUnit.Framework;
using TFrengler.CSSelenium;
using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Diagnostics;
using System.Linq;

namespace Tests
{
    [TestFixture]
    public class DriverManagerTests
    {
        public DriverManager WebdriverManager;
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
            System.Threading.Thread.Sleep(1000);
            var RunningDriverProcesses = Process.GetProcessesByName(name);

            if (RunningDriverProcesses.Length == 0)
            {
                return new DriverProcessInfo()
                {
                    Id = new int[0],
                    Instances = 0
                };
            }

            return new DriverProcessInfo()
            {
                Id = RunningDriverProcesses.Select(process => process.Id).ToArray(),
                Instances = RunningDriverProcesses.Length
            };
        }

        #region TESTS: Start

        [TestCase(Category="StaticDrivers")]
        public void Start_Chrome_Standard()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.CHROME, false, 0);
            });

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Start_Firefox_Standard()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.FIREFOX, false, 0);
            });

            var DriverProcesses = GetWebdriverProcessInfo("geckodriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Start_Edge_Standard()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.DoesNotThrow(()=> {
                WebdriverManager.Start(Browser.EDGE, false, 0);
            });

            var DriverProcesses = GetWebdriverProcessInfo("msedgedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Start_Chrome_AlreadyRunning_KillExisting()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            WebdriverManager.Start(Browser.CHROME, false, 0);
            int ExistingPID = GetWebdriverProcessInfo("chromedriver").Id[0];

            Assert.DoesNotThrow(() => WebdriverManager.Start(Browser.CHROME, true, 0));
            var RunningDriverProcesses = GetWebdriverProcessInfo("chromedriver");

            Assert.IsTrue(RunningDriverProcesses.Instances == 1);
            Assert.IsTrue(RunningDriverProcesses.Id[0] != ExistingPID);

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Start_Chrome_AlreadyRunning_NoKillingExisting()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            WebdriverManager.Start(Browser.CHROME, false, 0);
            int ExistingPID = GetWebdriverProcessInfo("chromedriver").Id[0];

            Assert.Throws<WebdriverAlreadyRunningException>(()=> WebdriverManager.Start(Browser.CHROME, false, 0));

            Assert.AreEqual(0, GetWebdriverProcessInfo("chromedriver").Instances);
            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Start_Chrome_DifferentPort()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

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

        [TestCase(Category="StaticDrivers")]
        public void Chrome_IsRunning()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.IsRunning(Browser.CHROME));
            WebdriverManager.Start(Browser.CHROME);
            Assert.IsTrue(WebdriverManager.IsRunning(Browser.CHROME));

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Firefox_IsRunning()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.IsRunning(Browser.FIREFOX));
            WebdriverManager.Start(Browser.FIREFOX);
            Assert.IsTrue(WebdriverManager.IsRunning(Browser.FIREFOX));

            var DriverProcesses = GetWebdriverProcessInfo("geckodriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Edge_IsRunning()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.IsRunning(Browser.EDGE));
            WebdriverManager.Start(Browser.EDGE);
            Assert.IsTrue(WebdriverManager.IsRunning(Browser.EDGE));

            var DriverProcesses = GetWebdriverProcessInfo("msedgedriver");
            Assert.IsTrue( DriverProcesses.Instances == 1 );

            WebdriverManager.Dispose();
        }

        #endregion

        #region TESTS: Stop

        [TestCase(Category="StaticDrivers")]
        public void Chrome_Stop()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.Stop(Browser.CHROME));
            WebdriverManager.Start(Browser.CHROME);
            Assert.IsTrue(WebdriverManager.Stop(Browser.CHROME));

            var DriverProcesses = GetWebdriverProcessInfo("chromedriver");
            Assert.AreEqual(0, DriverProcesses.Instances );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Firefox_Stop()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.Stop(Browser.FIREFOX));
            WebdriverManager.Start(Browser.FIREFOX);
            Assert.IsTrue(WebdriverManager.Stop(Browser.FIREFOX));

            var DriverProcesses = GetWebdriverProcessInfo("geckodriver");
            Assert.AreEqual(0, DriverProcesses.Instances );

            WebdriverManager.Dispose();
        }

        [TestCase(Category="StaticDrivers")]
        public void Edge_Stop()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);

            Assert.IsFalse(WebdriverManager.Stop(Browser.EDGE));
            WebdriverManager.Start(Browser.EDGE);
            Assert.IsTrue(WebdriverManager.Stop(Browser.EDGE));

            var DriverProcesses = GetWebdriverProcessInfo("msedgedriver");
            Assert.AreEqual(0, DriverProcesses.Instances );

            WebdriverManager.Dispose();
        }

        #endregion

        #region TESTS: DetermineLatestAvailableVersion

        [TestCase(Category="DetermineLatestVersion")]
        public void Chrome_DetermineLatestAvailableVersion()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            var VersionInfo = WebdriverManager.DetermineLatestVersion(Browser.CHROME);
            Assert.IsTrue(VersionInfo.Normalized > 0 && !string.IsNullOrEmpty(VersionInfo.Readable));
            WebdriverManager.Dispose();
        }

        [TestCase(Category="DetermineLatestVersion")]
        public void Firefox_DetermineLatestAvailableVersion()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            var VersionInfo = WebdriverManager.DetermineLatestVersion(Browser.FIREFOX);
            Assert.IsTrue(VersionInfo.Normalized > 0 && !string.IsNullOrEmpty(VersionInfo.Readable));
            WebdriverManager.Dispose();
        }

        [TestCase(Category="DetermineLatestVersion")]
        public void Edge_DetermineLatestAvailableVersion()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            var VersionInfo = WebdriverManager.DetermineLatestVersion(Browser.EDGE);
            Assert.IsTrue(VersionInfo.Normalized > 0 && !string.IsNullOrEmpty(VersionInfo.Readable));
            WebdriverManager.Dispose();
        }

        #endregion

        #region TESTS: Specific versions

        [TestCase(Category="DetermineSpecificVersions")]
        public void Chrome_SpecificVersion()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            var VersionInfo = WebdriverManager.DetermineAvailableVersionChrome(96);
            Assert.IsTrue(VersionInfo.Normalized > 0 && !string.IsNullOrEmpty(VersionInfo.Readable));
            WebdriverManager.Dispose();
        }

        [TestCase(Category="DetermineSpecificVersions")]
        public void Firefox_SpecificVersion()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            var VersionInfo = WebdriverManager.DetermineAvailableVersionFirefox(29);
            Assert.IsTrue(VersionInfo.Normalized > 0 && !string.IsNullOrEmpty(VersionInfo.Readable));
            WebdriverManager.Dispose();
        }

        [TestCase(Category="DetermineSpecificVersions")]
        public void Edge_SpecificVersion()
        {
            var WebdriverManager = new DriverManager(TempStaticBrowserDriverFolder);
            var VersionInfo = WebdriverManager.DetermineAvailableVersionEdge(96);
            Assert.IsTrue(VersionInfo.Normalized > 0 && !string.IsNullOrEmpty(VersionInfo.Readable));
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

            string VersionName = Enum.GetName(typeof(Browser), browser);

            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{VersionName}_WINDOWS", "v1.2.3");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{VersionName}_LINUX", "v1.2.3");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}.exe", "DUMMY");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{DriverName}", "DUMMY");

            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            var WindowsVersion = WebdriverManager.GetCurrentVersion(browser, Platform.WINDOWS);

            Console.WriteLine("WINDOWS Normalized: " + WindowsVersion.Normalized);
            Console.WriteLine("WINDOWS Readable: " + WindowsVersion.Readable);

            Assert.AreNotEqual(0, WindowsVersion.Normalized);
            Assert.AreNotEqual("0", WindowsVersion.Readable);

            if (browser != Browser.EDGE)
            {
                var LinuxVersion = WebdriverManager.GetCurrentVersion(browser, Platform.LINUX);

                Console.WriteLine("LINUX Normalized: " + WindowsVersion.Normalized);
                Console.WriteLine("LINUX Readable: " + WindowsVersion.Readable);

                Assert.AreNotEqual(0, LinuxVersion.Normalized);
                Assert.AreNotEqual("0", LinuxVersion.Readable);
            }

            WebdriverManager.Dispose();
        }

        private void CurrentVersionTest_NOK_Missing_Driver(Browser browser)
        {
            string VersionName = Enum.GetName(typeof(Browser), browser);

            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{VersionName}_WINDOWS", "0");
            File.WriteAllText(TempBrowserDriverDownloadFolder.FullName + $"/{VersionName}_LINUX", "0");

            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            var WindowsVersion = WebdriverManager.GetCurrentVersion(browser, Platform.WINDOWS);
            var LinuxVersion = WebdriverManager.GetCurrentVersion(browser, Platform.LINUX);

            Assert.AreEqual(0, WindowsVersion.Normalized);
            Assert.AreEqual(0, LinuxVersion.Normalized);
            Assert.AreEqual("0", WindowsVersion.Readable);
            Assert.AreEqual("0", LinuxVersion.Readable);

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

            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            var WindowsVersion = WebdriverManager.GetCurrentVersion(browser, Platform.WINDOWS);
            var LinuxVersion = WebdriverManager.GetCurrentVersion(browser, Platform.LINUX);

            Assert.AreEqual(0, WindowsVersion.Normalized);
            Assert.AreEqual(0, LinuxVersion.Normalized);
            Assert.AreEqual("0", WindowsVersion.Readable);
            Assert.AreEqual("0", LinuxVersion.Readable);

            WebdriverManager.Dispose();
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Chrome_GetCurrentVersion()
        {
            CurrentVersionTest_OK(Browser.CHROME);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Firefox_GetCurrentVersion()
        {
            CurrentVersionTest_OK(Browser.FIREFOX);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Edge_GetCurrentVersion()
        {
            CurrentVersionTest_OK(Browser.EDGE);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Chrome_GetCurrentVersion_Missing_Driver()
        {
            CurrentVersionTest_NOK_Missing_Driver(Browser.CHROME);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Firefox_GetCurrentVersion_Missing_Driver()
        {
            CurrentVersionTest_NOK_Missing_Driver(Browser.FIREFOX);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Edge_GetCurrentVersion_Missing_Driver()
        {
            CurrentVersionTest_NOK_Missing_Driver(Browser.EDGE);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Chrome_GetCurrentVersion_Missing_VersionFile()
        {
            CurrentVersionTest_NOK_Missing_VersionFile(Browser.CHROME);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Firefox_GetCurrentVersion_Missing_VersionFile()
        {
            CurrentVersionTest_NOK_Missing_VersionFile(Browser.FIREFOX);
        }

        [TestCase(Category="GetCurrentVersion")]
        public void Edge_GetCurrentVersion_Missing_VersionFile()
        {
            CurrentVersionTest_NOK_Missing_VersionFile(Browser.EDGE);
        }

        #endregion

        #region TESTS: Update

        [TestCase(Category="DownloadDrivers")]
        public void Chrome_GetLatestWebdriverBinary_OK_Windows()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            var UpdateResult = WebdriverManager.Update(Browser.CHROME, Platform.WINDOWS, TFrengler.CSSelenium.Architecture.x86);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/chromedriver.exe" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/CHROME_WINDOWS" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );

            Assert.IsTrue(UpdateResult.Updated);
            Assert.AreEqual("0", UpdateResult.OldVersion);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(UpdateResult.NewVersion) && UpdateResult.NewVersion != "0");
        }

        [TestCase(Category="DownloadDrivers")]
        public void Chrome_GetLatestWebdriverBinary_OK_Linux()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);

            var UpdateResult = WebdriverManager.Update(Browser.CHROME, Platform.LINUX, TFrengler.CSSelenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/chromedriver" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/CHROME_LINUX" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );

            Assert.IsTrue(UpdateResult.Updated);
            Assert.AreEqual("0", UpdateResult.OldVersion);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(UpdateResult.NewVersion) && UpdateResult.NewVersion != "0");
        }

        [TestCase(Category="DownloadDrivers")]
        public void Firefox_GetLatestWebdriverBinary_OK_Windows()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            var UpdateResult = WebdriverManager.Update(Browser.FIREFOX, Platform.WINDOWS, TFrengler.CSSelenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/geckodriver.exe" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/FIREFOX_WINDOWS" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );

            Assert.IsTrue(UpdateResult.Updated);
            Assert.AreEqual("0", UpdateResult.OldVersion);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(UpdateResult.NewVersion) && UpdateResult.NewVersion != "0");
        }

        [TestCase(Category="DownloadDrivers")]
        public void Firefox_GetLatestWebdriverBinary_OK_Linux()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);

            var UpdateResult = WebdriverManager.Update(Browser.FIREFOX, Platform.LINUX, TFrengler.CSSelenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/geckodriver" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/FIREFOX_LINUX" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );

            Assert.IsTrue(UpdateResult.Updated);
            Assert.AreEqual("0", UpdateResult.OldVersion);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(UpdateResult.NewVersion) && UpdateResult.NewVersion != "0");
        }

        [TestCase(Category="DownloadDrivers")]
        public void Edge_GetLatestWebdriverBinary_OK()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);

            var UpdateResult = WebdriverManager.Update(Browser.EDGE, Platform.WINDOWS, TFrengler.CSSelenium.Architecture.x64);

            var WebdriverFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/msedgedriver.exe" );
            var VersionFile = new FileInfo( TempBrowserDriverDownloadFolder.FullName + "/EDGE_WINDOWS" );

            Assert.IsTrue( WebdriverFile.Exists );
            Assert.IsTrue( VersionFile.Exists );

            Assert.IsTrue(UpdateResult.Updated);
            Assert.AreEqual("0", UpdateResult.OldVersion);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(UpdateResult.NewVersion) && UpdateResult.NewVersion != "0");
        }

        [TestCase(Category="DownloadDrivers")]
        public void Edge_GetLatestWebdriverBinary_NOK()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.Update(Browser.EDGE, Platform.LINUX, TFrengler.CSSelenium.Architecture.x64);
            });
        }

        [TestCase(Category="DownloadDrivers")]
        public void Chrome_GetLatestWebdriverBinary_NOK()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.Update(Browser.CHROME, Platform.WINDOWS, TFrengler.CSSelenium.Architecture.x64);
            });

            Assert.Throws<UnsupportedWebdriverConfigurationException>(() =>
            {
                WebdriverManager.Update(Browser.CHROME, Platform.LINUX, TFrengler.CSSelenium.Architecture.x86);
            });
        }

        #endregion

        #region Reset and Clear

        [TestCase(Category="ResetAndClear")]
        public void Reset()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            Assert.DoesNotThrow(()=> WebdriverManager.Reset());
        }

        [TestCase(Category="ResetAndClear")]
        public void ClearTemp()
        {
            var WebdriverManager = new DriverManager(TempBrowserDriverDownloadFolder);
            Assert.DoesNotThrow(()=> WebdriverManager.ResetTemp());
        }

        #endregion
    }
}