using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.IO.Compression;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.IE;
using OpenQA.Selenium;
using System.Security;

namespace TFrengler.Selenium
{
    public sealed class WebdriverManager : IDisposable
    {
        private readonly DirectoryInfo FileLocation;
        private readonly DriverService[] DriverServices;
        private readonly string[] DriverNames;
        private readonly HttpClient HttpClient;
        private readonly Dictionary<Browser, string> BrowserLatestVersionURLs;
        private bool IsDisposed = false;

        // Utility methods
        private string GetVersionFileName(Browser browser, Platform platform) => $"{DriverNames[(int)browser]}_{Enum.GetName(typeof(Platform), platform)}_version.txt";
        private long ParseVersionNumber(string version) => long.Parse(Regex.Replace(version, @"[a-zA-Z|\.]", ""));

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileLocation">The folder where the webdriver executables are. Note that the original file names of the webdrivers are expected! (chromedriver, geckodriver etc)</param>
        public WebdriverManager(DirectoryInfo fileLocation)
        {
            DriverNames = new string[4] { "msedgedriver", "geckodriver", "chromedriver", "IEDriverServer" };
            DriverServices = new DriverService[4];
            FileLocation = fileLocation;
            HttpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false });

            BrowserLatestVersionURLs = new Dictionary<Browser, string>()
            {
                { Browser.CHROME, "https://chromedriver.storage.googleapis.com/LATEST_RELEASE" },
                { Browser.FIREFOX, "https://github.com/mozilla/geckodriver/releases/latest" },
                { Browser.EDGE, "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/LATEST_STABLE" }
            };

            if (!FileLocation.Exists)
                throw new DirectoryNotFoundException("Unable to instantiate WebdriverManager. Directory with drivers does not exist: " + fileLocation.FullName);
        }

        /// <summary>
        /// Starts the given webdriver for the given browser, and return the URI that it's running on
        /// </summary>
        /// <param name="browser">The browser whose driver you wish to start</param>
        /// <param name="killExisting">If passed as true it will kill the already running instance. Otherwise it will throw an exception. Optional, defaults to false</param>
        /// <param name="port">The port you wish to start the webdriver on. Optional, defaults to a random, free port on the system</param>
        /// <returns>The URI that the webdriver is running on</returns>
        public Uri Start(Browser browser, bool killExisting = false, ushort port = 0)
        {
            bool RunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (!RunningOnWindows && (browser == Browser.IE11 || browser == Browser.EDGE))
                throw new UnsupportedOSException($"You are attempting to run the {Enum.GetName(typeof(Browser), browser)} driver on a non-Windows OS ({RuntimeInformation.OSDescription})");

            string DriverName;
            lock (DriverNames.SyncRoot)
            {
                if (RunningOnWindows)
                    DriverName = DriverNames[(int)browser] + ".exe";
                else
                    DriverName = DriverNames[(int)browser];
            }

            DriverService Service;
            lock (DriverServices.SyncRoot)
            {
                Service = DriverServices[(int)browser];
            }

            if (!killExisting && Service != null)
            {
                Dispose();
                throw new WebdriverAlreadyRunningException("Unable to start browser driver as it appears to already be running");
            }

            if (killExisting && Service != null)
                Stop(browser);

            var DriverExecutable = new FileInfo(FileLocation.FullName + "/" + DriverName);
            if (!DriverExecutable.Exists)
                throw new FileNotFoundException($"Cannot start browser driver - executable does not exist ({DriverExecutable.FullName})");

            if (DriverExecutable.IsReadOnly)
                throw new SecurityException($"Cannot start browser driver - executable is read-only ({DriverExecutable.FullName})");

            Service = browser switch
            {
                Browser.CHROME => ChromeDriverService.CreateDefaultService(FileLocation.FullName),
                Browser.FIREFOX => FirefoxDriverService.CreateDefaultService(FileLocation.FullName),
                Browser.EDGE => EdgeDriverService.CreateDefaultService(FileLocation.FullName),
                Browser.IE11 => InternetExplorerDriverService.CreateDefaultService(FileLocation.FullName),
                _ => throw new NotImplementedException("Fatal error - BROWSER enum not an expected value!")
            };

            if (port > 0) Service.Port = port;
            Service.Start();

            lock (DriverServices.SyncRoot)
            {
                DriverServices[(int)browser] = Service;
            }

            return Service.ServiceUrl;
        }

        /// <summary>
        /// Check whether the webdriver for the given browser is running, and able to receive commands
        /// </summary>
        /// <param name="browser">The browser whose status you want to check</param>
        /// <returns>A boolean to indicate whether the driver is running</returns>
        public bool IsRunning(Browser browser)
        {
            lock (DriverServices.SyncRoot)
            {
                DriverService Service = DriverServices[(int)browser];
                if (Service == null) return false;
                return Service.IsRunning;
            }
        }

        /// <summary>
        /// Shuts down the given webdriver. If any browser instances are open, those will be killed as well.
        /// </summary>
        /// <param name="browser">The browser whose webdriver you want to stop</param>
        /// <returns>A boolean to indicate whether the driver was stopped or not. If the driver isn't running it is therefore safe to call stop without worrying about exceptions</returns>
        public bool Stop(Browser browser)
        {
            lock (DriverServices.SyncRoot)
            {
                DriverService Service = DriverServices[(int)browser];
                if (Service != null)
                {
                    DriverServices[(int)browser] = null;
                    Service.Dispose();
                    return true;
                }
            }

            return false;
        }

        #region WEBDRIVER DOWNLOAD

        /// <summary>
        /// Downloads the latest webdriver binary for a given browser and platform if it's newer than the current version (or there is no current version)
        /// </summary>
        /// <returns>A string with a text message indicating whether the driver was updated or not</returns>
        public string GetLatestWebdriverBinary(Browser browser, Platform platform, Architecture architecture)
        {
            if (browser == Browser.EDGE && platform == Platform.LINUX)
                throw new UnsupportedWebdriverConfigurationException("Error fetching latest webdriver binary! Edge is not available on Linux");

            if (browser == Browser.CHROME && platform == Platform.LINUX && architecture == Architecture.x86)
                throw new UnsupportedWebdriverConfigurationException("Error fetching latest webdriver binary! Chrome on Linux only supports x64");

            if (browser == Browser.CHROME && platform == Platform.WINDOWS && architecture == Architecture.x64)
                throw new UnsupportedWebdriverConfigurationException("Error fetching latest webdriver binary! Chrome on Linux only supports x86");

            if (browser == Browser.IE11)
                throw new UnsupportedWebdriverConfigurationException("Error fetching latest webdriver binary! The IE11 driver is not supported for automatic downloading");

            var VersionFile = new FileInfo(FileLocation.FullName + GetVersionFileName(browser, platform));
            string CurrentVersion = GetCurrentVersion(browser, platform);
            string LatestVersion = DetermineLatestAvailableVersion(browser);

            string NormalizedCurrentVersion = CurrentVersion.PadRight(20, '0');
            string NormalizedLatestVersion = LatestVersion.PadRight(20, '0');

            if (ParseVersionNumber(NormalizedCurrentVersion) >= ParseVersionNumber(NormalizedLatestVersion))
                return $"The {Enum.GetName(typeof(Browser), browser)}-webdriver is already up to date, not downloading (Current: {CurrentVersion} | Latest: {LatestVersion})";

            Uri LatestWebdriverVersionURL = ResolveDownloadURL(LatestVersion, browser, platform, architecture);
            DownloadAndExtract(LatestWebdriverVersionURL, browser, platform, LatestVersion);

            return $"The {Enum.GetName(typeof(Browser), browser)}-webdriver has been updated to the latest version ({LatestVersion})";
        }

        /// <summary>
        /// Retrieves the current version if a given webdriver for a given platform
        /// </summary>
        /// <returns>The current version if both the webdriver and the version file exists, otherwise "0"</returns>
        public string GetCurrentVersion(Browser browser, Platform platform)
        {
            var VersionFile = new FileInfo(Path.Combine(FileLocation.FullName, GetVersionFileName(browser, platform)));

            string DriverName = DriverNames[(int)browser];
            if (platform == Platform.WINDOWS) DriverName = DriverName + ".exe";
            var WebdriverFile = new FileInfo(Path.Combine(FileLocation.FullName, DriverName));

            if (WebdriverFile.Exists && VersionFile.Exists)
                return File.ReadAllText(VersionFile.FullName);

            return "0";
        }

        /// <summary>
        /// Attempts to determines and retrieve the latest available version of the webdriver for the given browser
        /// </summary>
        /// <returns>A string representing the latest available version of the webdriver for a given browser</returns>
        public string DetermineLatestAvailableVersion(Browser browser)
        {
            var Request = new HttpRequestMessage()
            {
                RequestUri = new Uri(BrowserLatestVersionURLs[browser]),
                Method = HttpMethod.Get
            };

            var CancellationTokenSource = new CancellationTokenSource(10000);
            HttpResponseMessage Response = HttpClient.SendAsync(Request, CancellationTokenSource.Token).GetAwaiter().GetResult();
            Stream ResponseStream = Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

            if (browser == Browser.FIREFOX)
            {
                return Response.Headers.Location.ToString().Split('/').Last();
            }

            using (var StreamReader = new StreamReader(ResponseStream))
            {
                return StreamReader.ReadToEnd().Trim();
            };
        }

        private Uri ResolveDownloadURL(string version, Browser browser, Platform platform, Architecture architecture)
        {
            string PlatformPart = "", ArchitecturePart = "", FileTypePart = "", ReturnData = "";

            switch (architecture)
            {
                case Architecture.x64:
                    ArchitecturePart = "64";
                    break;
                case Architecture.x86:
                    ArchitecturePart = "32";
                    break;
            }

            switch (platform)
            {
                case Platform.LINUX:
                    PlatformPart = "linux";
                    FileTypePart = "tar.gz";
                    break;

                case Platform.WINDOWS:
                    PlatformPart = "win";
                    FileTypePart = "zip";
                    break;
            }

            switch (browser)
            {
                case Browser.FIREFOX:
                    ReturnData = $"https://github.com/mozilla/geckodriver/releases/download/{version}/geckodriver-{version}-{PlatformPart}{ArchitecturePart}.{FileTypePart}";
                    break;

                case Browser.CHROME:
                    ReturnData = $"https://chromedriver.storage.googleapis.com/{version}/chromedriver_{PlatformPart}{ArchitecturePart}.zip";
                    break;

                case Browser.EDGE:
                    ReturnData = $"https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/{version}/edgedriver_{PlatformPart}{ArchitecturePart}.zip";
                    break;
            }

            return new Uri(ReturnData);
        }

        private void DownloadAndExtract(Uri URL, Browser browser, Platform platform, string version)
        {
            string DownloadedFileName = URL.ToString().Split('/').Last();
            FileInfo DownloadedPathAndFile = new FileInfo(Path.GetTempPath() + DownloadedFileName);
            string VersionFileName = GetVersionFileName(browser, platform);

            var Request = new HttpRequestMessage()
            {
                RequestUri = URL,
                Method = HttpMethod.Get
            };

            var CancellationTokenSource = new CancellationTokenSource(60000);
            HttpResponseMessage Response = HttpClient.SendAsync(Request, CancellationTokenSource.Token).GetAwaiter().GetResult();

            // Need to do a second download for Firefox due to the redirect
            if (browser == Browser.FIREFOX)
            {
                if (Response.StatusCode != HttpStatusCode.Redirect)
                    throw new HttpRequestException($"Error downloading newest geckodriver: request didn't yield a 302 as expected ({URL})");

                var FirefoxRequest = new HttpRequestMessage()
                {
                    RequestUri = Response.Headers.Location,
                    Method = HttpMethod.Get
                };

                Response = HttpClient.SendAsync(FirefoxRequest).GetAwaiter().GetResult();
            }

            if (!Response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error downloading newest webdriver: URL didn't return a status indicating success | STATUS: {Enum.GetName(typeof(System.Net.HttpStatusCode), Response.StatusCode)} | URL: {URL} |");
            }

            Stream ResponseStream = Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            var Buffer = new byte[ResponseStream.Length];

            using (var StreamReader = new StreamReader(ResponseStream))
            {
                ResponseStream.Read(Buffer, 0, (int)ResponseStream.Length);
            }

            if (browser == Browser.FIREFOX && platform == Platform.LINUX)
            {
                ExtractTarGz(Buffer);
            }
            else
            {
                string ExtractedFileName = DriverNames[(int)browser];
                if (platform == Platform.WINDOWS) ExtractedFileName = ExtractedFileName + ".exe";

                var ExtractedFileNameAndPath = new FileInfo(Path.GetTempPath() + ExtractedFileName);
                // Of course the Edge-zip contains a silly, extra folder and not just the driver binary...
                var ExtraFolder = new DirectoryInfo(Path.GetTempPath() + "Driver_Notes");

                // Deleting the file (and extra folders...) in case they exist or ZipFile.ExtractToDirectory will throw an exception
                if (browser == Browser.EDGE && ExtraFolder.Exists) ExtraFolder.Delete(true);
                ExtractedFileNameAndPath.Delete();

                // Write the downloaded file to disk (temp location) and then extract the zip, and copy the binary
                File.WriteAllBytes(DownloadedPathAndFile.FullName, Buffer);
                ZipFile.ExtractToDirectory(DownloadedPathAndFile.FullName, Path.GetTempPath());
                File.Copy(ExtractedFileNameAndPath.FullName, Path.Combine(FileLocation.FullName, ExtractedFileName), true);

                // Clean-up, remove the extracted binaries from the temp folder (and Edge's extra folder)
                DownloadedPathAndFile.Delete();
                ExtractedFileNameAndPath.Delete();
                if (browser == Browser.EDGE && ExtraFolder.Exists) ExtraFolder.Delete(true);
            }

            // (over)Write the version file with the new version and delete the temporary, downloaded zip-file
            File.WriteAllText(Path.Combine(FileLocation.FullName, VersionFileName), version);

            /* if (RunningOnLinux)
            {
                // Need to set read/write and execute permissions on Linux
                var unixFileInfo = new Mono.Unix.UnixFileInfo("test.txt");
                // set file permission to 644
                unixFileInfo.FileAccessPermissions =
                FileAccessPermissions.UserRead | FileAccessPermissions.UserWrite
                | FileAccessPermissions.GroupRead
                | FileAccessPermissions.OtherRead;

                https://www.nuget.org/packages/Mono.Posix.NETStandard/1.0.0
                https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core
            } */
        }

        private void ExtractTarGz(byte[] tarGzData)
        {
            var DecompressedFileStream = new MemoryStream();
            GZipStream DecompressionStream = new GZipStream(new MemoryStream(tarGzData), CompressionMode.Decompress);

            int ChunkSize = 4096;
            int Read = ChunkSize;
            byte[] ReadBuffer = new byte[ChunkSize];

            while (Read == ChunkSize)
            {
                Read = DecompressionStream.Read(ReadBuffer, 0, ChunkSize);
                DecompressedFileStream.Write(ReadBuffer, 0, Read);
            }

            DecompressedFileStream.Seek(0, SeekOrigin.Begin);
            ExtractTar(DecompressedFileStream);

            DecompressionStream.Dispose();
            DecompressedFileStream.Dispose();
        }

        // https://gist.github.com/ForeverZer0/a2cd292bd2f3b5e114956c00bb6e872b
        private void ExtractTar(Stream tarStream)
        {
            var ReadBuffer = new byte[100];

            while (true)
            {
                tarStream.Read(ReadBuffer, 0, 100);
                string Name = Encoding.ASCII.GetString(ReadBuffer).Trim('\0');

                if (String.IsNullOrWhiteSpace(Name))
                    break;

                tarStream.Seek(24, SeekOrigin.Current);
                tarStream.Read(ReadBuffer, 0, 12);
                Int64 Size = Convert.ToInt64(Encoding.UTF8.GetString(ReadBuffer, 0, 12).Trim('\0').Trim(), 8);

                tarStream.Seek(376L, SeekOrigin.Current);
                string Output = Path.Combine(FileLocation.FullName, Name);

                // Get the directory part of the output and create the folder if it doesn't exist
                if (!Directory.Exists(Path.GetDirectoryName(Output)))
                    Directory.CreateDirectory(Path.GetDirectoryName(Output));

                // Is a directory? If not then it's a file and we'll extract it
                if (!Name.EndsWith("/"))
                {
                    using (FileStream FileOutputStream = File.Open(Output, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        var OutputBuffer = new byte[Size];
                        tarStream.Read(OutputBuffer, 0, OutputBuffer.Length);
                        FileOutputStream.Write(OutputBuffer, 0, OutputBuffer.Length);
                    }
                }

                Int64 Position = tarStream.Position;
                Int64 Offset = 512 - (Position % 512);

                if (Offset == 512)
                    Offset = 0;

                tarStream.Seek(Offset, SeekOrigin.Current);
            }
        }

        #endregion

        /// <summary>
        /// Shuts down all the running webdrivers and any browser instances that are open
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            Stop(Browser.EDGE);
            Stop(Browser.FIREFOX);
            Stop(Browser.CHROME);
            Stop(Browser.IE11);
        }
    }
}