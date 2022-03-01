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
using OpenQA.Selenium;
using System.Security;
using System.Xml.Linq;
using System.Net.Http.Headers;
using Mono.Unix;
using SharpCompress.Readers;
using SharpCompress.Common;

namespace TFrengler.CSSelenium
{
    /// <summary>Represents the status of attempting to update a browser-driver</summary>
    public readonly struct UpdateResponse
    {
        public UpdateResponse(bool updated, string oldVersion, string newVersion)
        {
            Updated = updated;
            OldVersion = oldVersion;
            NewVersion = newVersion;
        }

        /// <summary>True if the driver was updated, false if not</summary>
        public bool Updated { get; }
        /// <summary>The version of the driver binary before it was updated. Is "0" if the driver binary didn't exist</summary>
        public string OldVersion { get; }
        /// <summary>The version of the driver binary after it was updated. Is empty if Updated is false</summary>
        public string NewVersion { get; }
    }

    /// <summary>Represents version info about a given browser driver</summary>
    public readonly struct VersionInfo
    {
        public VersionInfo(uint normalized, string readable)
        {
            Normalized = normalized;
            Readable = readable;
        }

        /// <summary>The normalized (summed up) version number, used for comparison between versions</summary>
        public uint Normalized { get; }
        /// <summary>The UI-friendly string version of the version</summary>
        public string Readable { get; }
    }

    /// <summary>Utility class for managing the driver lifetimes and also facilitatates auto-updating the various driver binaries to the latest or a specific version</summary>
    public sealed class DriverManager : IDisposable
    {
        private sealed class HttpResponse
        {
            public HttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, MemoryStream content)
            {
                StatusCode = statusCode;
                Headers = headers;
                Content = content;
            }

            public HttpStatusCode StatusCode {get;}
            public HttpResponseHeaders Headers {get;}
            public MemoryStream Content {get;}
        }

        private readonly DirectoryInfo FileLocation;
        private readonly DirectoryInfo TempFolder;
        private readonly DriverService[] DriverServices;
        private readonly string[] DriverNames;
        private HttpClient HttpClient { get => _HttpClient.Value; }
        private readonly Lazy<HttpClient> _HttpClient;
        private readonly Dictionary<Browser, string> BrowserLatestVersionURLs;
        private bool IsDisposed = false;

        private const string EDGE_VERSION_URL = "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver?comp=list&prefix=";
        private const string CHROME_VERSION_URL = "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_";
        private const string FIREFOX_VERSION_URL = "https://github.com/mozilla/geckodriver/releases?q=0.";

        // Utility methods
        private string GetVersionFileName(Browser browser, Platform platform) => $"{GetBrowserName(browser)}_{GetPlatformName(platform)}";
        private string CleanVersionString(string version) => Regex.Replace(version, @"[a-zA-Z]", "");
        private string GetBrowserName(Browser browser) => Enum.GetName(typeof(Browser), browser);
        private string GetPlatformName(Platform platform) => Enum.GetName(typeof(Platform), platform);
        private string GetArchitectureName(Architecture architecture) => Enum.GetName(typeof(Architecture), architecture);
        private uint NormalizeVersion(string version) => (uint)CleanVersionString(version).Split('.', StringSplitOptions.RemoveEmptyEntries).Select(part => Convert.ToInt32(part.Trim())).Sum();

        /// <param name="fileLocation">The folder where the webdriver executables are. Note that the original file names of the webdrivers are expected! (chromedriver, geckodriver etc). If you plan on using the auto-update functionality this folder has to be writeable as well!</param>
        public DriverManager(DirectoryInfo fileLocation)
        {
            DriverNames = new string[3] { "msedgedriver", "geckodriver", "chromedriver" };
            DriverServices = new DriverService[3];
            FileLocation = fileLocation;
            _HttpClient = new Lazy<HttpClient>(()=>  new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false }));

            BrowserLatestVersionURLs = new Dictionary<Browser, string>()
            {
                { Browser.CHROME, "https://chromedriver.storage.googleapis.com/LATEST_RELEASE" },
                { Browser.FIREFOX, "https://github.com/mozilla/geckodriver/releases/latest" },
                { Browser.EDGE, "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/LATEST_STABLE" }
            };

            if (!FileLocation.Exists)
                throw new DirectoryNotFoundException($"Unable to instantiate {nameof(DriverManager)}. Directory with drivers does not exist: " + fileLocation.FullName);

            TempFolder = new DirectoryInfo(FileLocation.FullName + "/Temp");
        }

        /// <summary>
        /// Starts the given webdriver for the given browser, and return the URI that it's running on
        /// </summary>
        /// <param name="browser">The browser whose driver you wish to start</param>
        /// <param name="killExisting">If passed as true it will kill the already running instance. Otherwise it will throw an exception. Optional, defaults to false</param>
        /// <param name="port">The port you wish to start the webdriver on. Optional, defaults to a random, free port on the system</param>
        /// <returns>The URI that the webdriver is running on</returns>
        public Uri Start(Browser browser, bool killExisting = false, uint port = 0)
        {
            bool RunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (!RunningOnWindows && browser == Browser.EDGE)
                throw new UnsupportedOSException($"You are attempting to run the {GetBrowserName(browser)} driver on a non-Windows OS ({RuntimeInformation.OSDescription})");

            string DriverName;

            if (RunningOnWindows)
                DriverName = DriverNames[(int)browser] + ".exe";
            else
                DriverName = DriverNames[(int)browser];

            DriverService Service;
            lock (DriverServices.SyncRoot)
            {
                Service = DriverServices[(int)browser];

                if (!killExisting && Service != null)
                {
                    Dispose();
                    throw new WebdriverAlreadyRunningException($"Unable to start the {GetBrowserName(browser)}-driver as it appears to already be running");
                }

                if (killExisting && Service != null)
                    Stop(browser);

                var DriverExecutable = new FileInfo(FileLocation.FullName + "/" + DriverName);
                if (!DriverExecutable.Exists)
                {
                    Dispose();
                    throw new FileNotFoundException($"Cannot start the {GetBrowserName(browser)}-driver - executable does not exist ({DriverExecutable.FullName})");
                }

                if (DriverExecutable.IsReadOnly)
                {
                    Dispose();
                    throw new SecurityException($"Cannot start the {GetBrowserName(browser)}-driver - executable is read-only ({DriverExecutable.FullName})");
                }

                Service = browser switch
                {
                    Browser.CHROME => ChromeDriverService.CreateDefaultService(FileLocation.FullName),
                    Browser.FIREFOX => FirefoxDriverService.CreateDefaultService(FileLocation.FullName),
                    Browser.EDGE => EdgeDriverService.CreateDefaultService(FileLocation.FullName),
                    _ => throw new NotImplementedException("Fatal error - BROWSER enum not an expected value: " + browser)
                };

                if (port > 0) Service.Port = (int)port;
                Service.Start();

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

        /// <summary>Deletes all files in the driver-folder and removes the temp-folder</summary>
        public void Reset()
        {
            FileLocation.Refresh();

            foreach(FileInfo CurrentFile in FileLocation.EnumerateFiles())
            {
                CurrentFile.Delete();
            }

            if (TempFolder.Exists)
            {
                TempFolder.Delete(true);
                TempFolder.Refresh();
            }

            TempFolder.Create();
        }

        /// <summary>Deletes the temp-folder</summary>
        public void ResetTemp()
        {
            if (TempFolder.Exists)
            {
                TempFolder.Delete(true);
                TempFolder.Refresh();
            }
        }

        /// <summary>Shuts down all the running drivers along with any open browser windows belonging to those drivers</summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            Stop(Browser.EDGE);
            Stop(Browser.FIREFOX);
            Stop(Browser.CHROME);
        }

        #region WEBDRIVER DOWNLOAD

        /// <summary>
        /// Attempts to update the webdriver for the given browser to a desired version or latest available version. If the current version matches the desired version for the given configuration then it does nothing.
        /// <para>Note that not all platform and architecture combinations are supported:</para>
        /// <para>- EDGE is not available on LINUX</para>
        /// <para>- CHROME on LINUX only has an x64 version available</para>
        /// <para>- CHROME on WINDOWS only has an x86 version available</para>
        /// </summary>
        /// <param name="version">The version of the driver to download. This is the major revision only! The latest version for a given major revision is always chosen</param>
        /// <param name="browser">The browser whose webdriver you want to update</param>
        /// <param name="platform">The desired platform for the driver. Note that not all platform and architecture combinations are supported!</param>
        /// <param name="architecture">The desired architecture of the driver. Note that not all platform and architecture combinations are supported!</param>
        /// <returns>A structure indicating whether the driver was updated, what the old and new version is and/or whether an error was encountered</returns>
        public UpdateResponse Update(Browser browser, Platform platform, Architecture architecture, uint version = 0)
        {
            if (browser == Browser.EDGE && platform == Platform.LINUX)
            {
                throw new UnsupportedWebdriverConfigurationException("Edge is not available on Linux");
            }

            if (browser == Browser.CHROME && platform == Platform.LINUX && architecture == Architecture.x86)
            {
                throw new UnsupportedWebdriverConfigurationException("Chrome on Linux only has x64 driver available");
            }

            if (browser == Browser.CHROME && platform == Platform.WINDOWS && architecture == Architecture.x64)
            {
                throw new UnsupportedWebdriverConfigurationException("Chrome on Windows only has x86 driver available");
            }

            string CurrentVersion = GetCurrentVersion(browser, platform);
            VersionInfo DesiredVersionInfo;

            if (version == 0)
            {
                DesiredVersionInfo = DetermineLatestVersion(browser);
            }
            else
            {
                DesiredVersionInfo = browser switch
                {
                    Browser.CHROME => DetermineAvailableVersionChrome(version),
                    Browser.EDGE => DetermineAvailableVersionEdge(version),
                    Browser.FIREFOX => DetermineAvailableVersionFirefox(version),
                    _ => throw new NotImplementedException("FATAL ERROR!!!")
                };
            }

            if (DesiredVersionInfo.Normalized == NormalizeVersion(CurrentVersion))
            {
                return new UpdateResponse(false, CurrentVersion, DesiredVersionInfo.Readable);
            };

            if (!TempFolder.Exists)
            {
                TempFolder.Create();
            }

            Uri DesiredVersionURL = ResolveDownloadURL(DesiredVersionInfo.Readable, browser, platform, architecture);
            DownloadAndExtract(DesiredVersionURL, browser, platform, architecture, DesiredVersionInfo.Readable);

            return new UpdateResponse(true, CurrentVersion, DesiredVersionInfo.Readable);
        }

        /// <summary>Retrieves the current version of the driver for a given browser, platform and architecture</summary>
        /// <returns>The current version if both the webdriver and the version file exists, otherwise "0"</returns>
        public string GetCurrentVersion(Browser browser, Platform platform)
        {
            var VersionFile = new FileInfo(Path.Combine(FileLocation.FullName, GetVersionFileName(browser, platform)));

            string DriverName = DriverNames[(int)browser];
            if (platform == Platform.WINDOWS) DriverName = DriverName + ".exe";
            var WebdriverFile = new FileInfo(Path.Combine(FileLocation.FullName, DriverName));

            if (WebdriverFile.Exists && VersionFile.Exists)
            {
                using (FileStream File = new FileStream(VersionFile.FullName, FileMode.Open, FileAccess.Read))
                using (StreamReader Reader = new StreamReader(File))
                {
                    return Reader.ReadToEnd();
                }
            }

            return "0";
        }

        /// <summary>
        /// Attempts to determines and retrieve the latest available version of the webdriver for the given browser
        /// </summary>
        /// <returns>A string representing the latest available version of the webdriver for a given browser</returns>
        public VersionInfo DetermineLatestVersion(Browser browser)
        {
            var URL = new Uri(BrowserLatestVersionURLs[browser]);
            HttpResponse Response = MakeRequest(URL);

            // Firefox is different in that the latest version is in the redirect-header
            if (browser == Browser.FIREFOX)
            {
                string StringVersion = Response.Headers.Location == null ? null : Response.Headers.Location.ToString().Split('/').Last();
                uint NormalizedVersion = StringVersion == null ? 0 : NormalizeVersion(StringVersion);
                Exception Error = null;

                if (StringVersion == null)
                {
                    Error = new HttpRequestException($"Location-header absent in HTTP response to determine latest FIREFOX-driver version ({URL.AbsolutePath})");
                }

                return new VersionInfo(NormalizedVersion, StringVersion);
            }

            if (Response.Content == null)
            {
                throw new HttpRequestException($"HTTP-response from call to determine latest {GetBrowserName(browser)}-driver version contained no body ({URL.AbsolutePath})");
            }

            string VersionString = new StreamReader(Response.Content).ReadToEnd().Trim();
            return new VersionInfo(NormalizeVersion(VersionString), VersionString);
        }

        #region PRIVATE

        private VersionInfo DetermineAvailableVersionEdge(uint version)
        {
            var URL = new Uri(EDGE_VERSION_URL + version);
            HttpResponse Response = MakeRequest(URL);

            if (Response.Content == null)
            {
                throw new HttpRequestException($"HTTP-response from call to determine specific EDGE-driver version contained no body ({URL.AbsolutePath})");
            }

            string StringContent = new StreamReader(Response.Content).ReadToEnd().Trim();
            XDocument AvailableEdgeVersionsXML;

            AvailableEdgeVersionsXML = XDocument.Parse(StringContent);
            IEnumerable<XElement> AvailableVersions = AvailableEdgeVersionsXML.Descendants("Name");

            if (AvailableVersions.Count() == 0)
            {
                throw new UnavailableVersionException(Browser.EDGE, version);
            }

            string DesiredMajorRevision = Convert.ToString(version);
            IEnumerable<string> ParsedVersions = AvailableVersions.Select(element=> element.Value.Split('/', StringSplitOptions.None)[0]);
            IEnumerable<uint> NormalizedVersions = ParsedVersions.Select(tag=> NormalizeVersion(tag));

            uint LatestVersionNormalized = NormalizedVersions.Max();
            int LatestIndex = Array.IndexOf(NormalizedVersions.ToArray(), LatestVersionNormalized);
            string LatestVersionString = ParsedVersions.ElementAt(LatestIndex);

            return new VersionInfo(LatestVersionNormalized, LatestVersionString);
        }

        private VersionInfo DetermineAvailableVersionChrome(uint version)
        {
            var URL = new Uri(CHROME_VERSION_URL + version);
            HttpResponse Response = MakeRequest(URL);

            if (Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new UnavailableVersionException(Browser.CHROME, version);
            }

            if (Response.Content == null)
            {
                throw new HttpRequestException($"HTTP-response from call to determine specific CHROME-driver version contained no body ({URL.AbsolutePath})");
            }

            string StringContent = new StreamReader(Response.Content).ReadToEnd().Trim();
            return new VersionInfo( NormalizeVersion(StringContent), StringContent );
        }

        private VersionInfo DetermineAvailableVersionFirefox(uint version)
        {
            var URL = new Uri(FIREFOX_VERSION_URL + version);
            HttpResponse Response = MakeRequest(URL);

            if (Response.Content == null)
            {
                throw new HttpRequestException($"HTTP-response from call to determine specific FIREFOX-driver version contained no body ({URL.AbsolutePath})");
            }

            string StringContent = new StreamReader(Response.Content).ReadToEnd().Trim();
            var TagPattern = new Regex("<a href=\"/mozilla/geckodriver/releases/tag/v(\\d+.\\d+.\\d+)\"");
            MatchCollection Matches = TagPattern.Matches(StringContent);

            if (Matches.Count == 0)
            {
                throw new UnavailableVersionException(Browser.FIREFOX, version);
            }

            string[] Tags = new string[Matches.Count];

            for (int Index = 0; Index < Matches.Count; Index++)
            {
                Tags[Index] = Matches[Index].Groups[1].Value;
            }

            string DesiredMajorRevision = Convert.ToString(version);
            IEnumerable<string> FilteredTags = Tags.Where(tag=> tag.Split('.', StringSplitOptions.None)[1] == DesiredMajorRevision);
            IEnumerable<uint> NormalizedTags = FilteredTags.Select(tag=> NormalizeVersion(tag));

            uint LatestVersionNormalized = NormalizedTags.Max();
            int LatestIndex = Array.IndexOf(NormalizedTags.ToArray(), LatestVersionNormalized);
            string LatestVersionString = FilteredTags.ElementAt(LatestIndex);

            return new VersionInfo(LatestVersionNormalized, LatestVersionString);
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

        private void DownloadAndExtract(Uri URL, Browser browser, Platform platform, Architecture architecture, string version)
        {
            string DownloadedFileName = URL.ToString().Split('/').Last();
            string VersionFileName = GetVersionFileName(browser, platform);
            string DriverFileName = DriverNames[(int)browser];

            if (platform == Platform.WINDOWS)
                DriverFileName = DriverFileName + ".exe";

            FileInfo DriverFileFinal = new FileInfo(Path.Combine(FileLocation.FullName, DriverFileName));
            FileInfo DownloadedArchive = new FileInfo(Path.Combine(TempFolder.FullName, DownloadedFileName));
            FileInfo DriverFileTemp = new FileInfo(Path.Combine(TempFolder.FullName, DriverFileName));

            HttpResponse Response = MakeRequest(URL);
            // Need to do a second download for Firefox due to the redirect
            if (browser == Browser.FIREFOX)
            {
                if (Response.StatusCode != HttpStatusCode.Redirect)
                {
                    throw new HttpRequestException($"Error downloading latest FIREFOX-driver: request didn't yield a 302 as expected ({URL})");
                }

                Response = MakeRequest(Response.Headers.Location);
            }

            if (Response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Error downloading latest {GetBrowserName(browser)}-driver: URL didn't return a status indicating success | STATUS: {Enum.GetName(typeof(System.Net.HttpStatusCode), Response.StatusCode)} | URL: {URL} |");
            }

            // Write the downloaded file to disk (temp location) and then extract the zip, and copy the binary to the "normal" folder
            using (FileStream File = new FileStream(DownloadedArchive.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                File.Write(Response.Content.ToArray());
                File.Position = 0;

                ExtractArchive(TempFolder, File);
            }

            File.Copy(DriverFileTemp.FullName, DriverFileFinal.FullName, true);
            // Clean-up, remove the extracted file from the temp folder
            DriverFileTemp.Delete();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Need to set read/write and execute permissions on Linux
                var UnixFileInfo = new Mono.Unix.UnixFileInfo(DriverFileFinal.FullName);
                // Set file permission to 644
                UnixFileInfo.FileAccessPermissions = FileAccessPermissions.UserRead | FileAccessPermissions.UserWrite | FileAccessPermissions.GroupRead | FileAccessPermissions.OtherRead;
                //https://www.nuget.org/packages/Mono.Posix.NETStandard/1.0.0
                //https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core
            }

            // (over)Write the version file with the new version
            using (FileStream File = new FileStream(Path.Combine(FileLocation.FullName, VersionFileName), FileMode.Create, FileAccess.Write))
            using (StreamWriter Writer = new StreamWriter(File))
            {
                Writer.Write(version);
            }
        }

        private HttpResponse MakeRequest(Uri uri)
        {
            var Request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };

            var CancellationTokenSource = new CancellationTokenSource(30000);
            HttpResponseMessage Response = HttpClient.SendAsync(Request, CancellationTokenSource.Token).GetAwaiter().GetResult();
            Stream ResponseStream = Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

            var Buffer = new MemoryStream();
            if (ResponseStream != null)
            {
                ResponseStream.CopyTo(Buffer);
                Buffer.Position = 0;
            }

            return new HttpResponse(Response.StatusCode, Response.Headers, Buffer);
        }

        private List<FileInfo> ExtractArchive(DirectoryInfo outputDir, Stream input)
        {
            var ReturnData = new List<FileInfo>();
            var Reader = ReaderFactory.Open(input);

            while (Reader.MoveToNextEntry())
            {
                if (!Reader.Entry.IsDirectory)
                {
                    Reader.WriteEntryToDirectory(outputDir.FullName, new ExtractionOptions() { ExtractFullPath = false, Overwrite = true });
                    ReturnData.Add( new FileInfo(Path.Combine(outputDir.FullName, Reader.Entry.Key)) );
                }
            }

            return ReturnData;
        }

        #endregion
        #endregion
    }
}