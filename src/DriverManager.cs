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

namespace TFrengler.CSSelenium
{
    public struct UpdateResponse
    {
        public bool Updated { get; set; }
        public string OldVersion { get; set; }
        public string NewVersion { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>Class that can manage webdriver lifetimes as well as update them to desired versions</summary>
    public sealed class DriverManager : IDisposable
    {
        private sealed class HttpResponse
        {
            public HttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, byte[] content)
            {
                StatusCode = statusCode;
                Headers = headers;
                Content = content;
            }

            public HttpStatusCode StatusCode {get;}
            public HttpResponseHeaders Headers {get;}
            public byte[] Content {get;}
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
        private string GetVersionFileName(Browser browser, Platform platform, Architecture architecture) => $"{GetBrowserName(browser)}_{GetPlatformName(platform)}_{GetArchitectureName(architecture)}";
        private string CleanVersionString(string version) => Regex.Replace(version, @"[a-zA-Z]", "");
        private string GetBrowserName(Browser browser) => Enum.GetName(typeof(Browser), browser);
        private string GetPlatformName(Platform platform) => Enum.GetName(typeof(Platform), platform);
        private string GetArchitectureName(Architecture architecture) => Enum.GetName(typeof(Architecture), architecture);
        private uint NormalizeVersion(string version) => (uint)CleanVersionString(version).Split('.', StringSplitOptions.RemoveEmptyEntries).Select(part => Convert.ToInt32(part.Trim())).Sum();

        /// <summary>
        /// Constructor
        /// </summary>
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

        /// <summary>Deletes all files in the driver-folder as well as recreates the temp-folder</summary>
        public void Reset()
        {
            FileLocation.Refresh();

            foreach(FileInfo CurrentFile in FileLocation.EnumerateFiles())
            {
                CurrentFile.Delete();
            }

            TempFolder.Delete(true);
            TempFolder.Refresh();
            TempFolder.Create();
        }

        /// <summary>
        /// Shuts down all the running drivers along with any open browser windows belonging to those drivers
        /// </summary>
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
                return new UpdateResponse()
                {
                    Updated = false,
                    OldVersion = "",
                    NewVersion = "",
                    Error = new UnsupportedWebdriverConfigurationException("Edge is not available on Linux")
                };
            }

            if (browser == Browser.CHROME && platform == Platform.LINUX && architecture == Architecture.x86)
            {
                return new UpdateResponse()
                {
                    Updated = false,
                    OldVersion = "",
                    NewVersion = "",
                    Error = new UnsupportedWebdriverConfigurationException("Chrome on Linux only has x64 driver available")
                };
            }

            if (browser == Browser.CHROME && platform == Platform.WINDOWS && architecture == Architecture.x64)
            {
                return new UpdateResponse()
                {
                    Updated = false,
                    OldVersion = "",
                    NewVersion = "",
                    Error = new UnsupportedWebdriverConfigurationException("Chrome on Windows only has x86 driver available")
                };
            }

            var VersionFile = new FileInfo(FileLocation.FullName + GetVersionFileName(browser, platform, architecture));
            string CurrentVersion = GetCurrentVersion(browser, platform, architecture);
            Tuple<uint, string, Exception> DesiredVersionCall;

            if (version == 0)
            {
                DesiredVersionCall = DetermineLatestVersion(browser);
            }
            else
            {
                DesiredVersionCall = browser switch
                {
                    Browser.CHROME => DetermineAvailableVersionChrome(version),
                    Browser.EDGE => DetermineAvailableVersionEdge(version),
                    Browser.FIREFOX => DetermineAvailableVersionFirefox(version)
                };
            }

            if (DesiredVersionCall.Item3 != null)
            {
                return new UpdateResponse()
                {
                    Updated = false,
                    OldVersion = CurrentVersion,
                    NewVersion = "",
                    Error = DesiredVersionCall.Item3
                };
            }

            uint DesiredVersion = DesiredVersionCall.Item1;
            uint NormalizedCurrentVersion = NormalizeVersion(CurrentVersion);

            if (NormalizedCurrentVersion == DesiredVersion)
            {
                return new UpdateResponse()
                {
                    Updated = false,
                    OldVersion = CurrentVersion,
                    NewVersion = DesiredVersionCall.Item2
                };
            }

            if (!TempFolder.Exists)
            {
                TempFolder.Create();
            }

            Uri DesiredVersionURL = ResolveDownloadURL(DesiredVersionCall.Item2, browser, platform, architecture);
            DownloadAndExtract(DesiredVersionURL, browser, platform, DesiredVersionCall.Item2);

            return new UpdateResponse()
            {
                Updated = true,
                OldVersion = CurrentVersion,
                NewVersion = DesiredVersionCall.Item2
            };
        }

        /// <summary>
        /// Retrieves the current version if a given webdriver for a given platform and architecture
        /// </summary>
        /// <returns>The current version if both the webdriver and the version file exists, otherwise "0"</returns>
        public string GetCurrentVersion(Browser browser, Platform platform, Architecture architecture)
        {
            var VersionFile = new FileInfo(Path.Combine(FileLocation.FullName, GetVersionFileName(browser, platform, architecture)));

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
        public Tuple<uint, string, Exception> DetermineLatestVersion(Browser browser)
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
                    Error = new HttpRequestException($"Location-header absent in HTTP response to determine latest FIREFOX driver version ({URL.AbsolutePath})");
                }

                return new Tuple<uint, string, Exception>(NormalizedVersion, StringVersion, Error);
            }

            if (Response.Content == null)
            {
                return new Tuple<uint, string, Exception>(0, null, new HttpRequestException($"HTTP-response from call to determine latest {GetBrowserName(browser)} driver version contained no body ({URL.AbsolutePath})"));
            }

            string VersionString = Encoding.UTF8.GetString(Response.Content);
            return new Tuple<uint, string, Exception>(NormalizeVersion(VersionString), VersionString, null);
        }

        #region PRIVATE

        private Tuple<uint, string, Exception> DetermineAvailableVersionEdge(uint version)
        {
            var URL = new Uri(EDGE_VERSION_URL + version);
            HttpResponse Response = MakeRequest(URL);

            if (Response.Content == null)
            {
                return new Tuple<string, Exception>("", new HttpRequestException($"HTTP-response from call to determine specific EDGE-driver version contained no body ({URL.AbsolutePath})"));
            }

            string StringContent = Encoding.UTF8.GetString(Response.Content);
            XDocument AvailableEdgeVersionsXML;

            try
            {
                AvailableEdgeVersionsXML = XDocument.Parse(StringContent);
            }
            catch(System.Xml.XmlException error)
            {
                return new Tuple<string, Exception>(null, error);
            }

            IEnumerable<XElement> AvailableVersions = AvailableEdgeVersionsXML.Descendants("Name");

            if (AvailableVersions.Count() == 0)
            {
                return new Tuple<string, Exception>(null, new UnavailableVersionException(Browser.EDGE, version));
            }

            string DesiredMajorRevision = Convert.ToString(version);
            IEnumerable<string> ParsedVersions = AvailableVersions.Select(element=> element.Value.Split('/', StringSplitOptions.None)[0]);
            IEnumerable<int> NormalizedVersions = ParsedVersions.Select(tag=> NormalizeVersion(tag));
            int LatestIndex = Array.IndexOf(NormalizedVersions.ToArray(), NormalizedVersions.Max());
            string LatestVersion = ParsedVersions.ElementAt(LatestIndex);

            return new Tuple<string, Exception>(LatestVersion, null);
        }

        private Tuple<uint, string, Exception> DetermineAvailableVersionChrome(uint version)
        {
            var URL = new Uri(CHROME_VERSION_URL + version);
            HttpResponse Response = MakeRequest(URL);

            if (Response.StatusCode == HttpStatusCode.NotFound)
            {
                return new Tuple<string, Exception>(null, new UnavailableVersionException(Browser.CHROME, version));
            }

            if (Response.Content == null)
            {
                return new Tuple<string, Exception>("", new HttpRequestException($"HTTP-response from call to determine specific CHROME-driver version contained no body ({URL.AbsolutePath})"));
            }

            string StringContent = Encoding.UTF8.GetString(Response.Content);
            return new Tuple<string, Exception>(StringContent, null);
        }

        private Tuple<uint, string, Exception> DetermineAvailableVersionFirefox(uint version)
        {
            var URL = new Uri(FIREFOX_VERSION_URL + version);
            HttpResponse Response = MakeRequest(URL);

            if (Response.Content == null)
            {
                return new Tuple<string, Exception>("", new HttpRequestException($"HTTP-response from call to determine specific FIREFOX-driver version contained no body ({URL.AbsolutePath})"));
            }

            string StringContent = Encoding.UTF8.GetString(Response.Content);
            var TagPattern = new Regex("<a href=\"/mozilla/geckodriver/releases/tag/v(\\d+.\\d+.\\d+)\"");
            MatchCollection Matches = TagPattern.Matches(StringContent);

            if (Matches.Count == 0)
            {
                return new Tuple<string, Exception>(null, new UnavailableVersionException(Browser.FIREFOX, version));
            }

            string[] Tags = new string[Matches.Count];

            for (int Index = 0; Index < Matches.Count; Index++)
            {
                Tags[Index] = Matches[Index].Groups[1].Value;
            }

            string DesiredMajorRevision = Convert.ToString(version);
            IEnumerable<string> FilteredTags = Tags.Where(tag=> tag.Split('.', StringSplitOptions.None)[1] == DesiredMajorRevision);
            IEnumerable<int> NormalizedTags = FilteredTags.Select(tag=> NormalizeVersion(tag));
            int LatestIndex = Array.IndexOf(NormalizedTags.ToArray(), NormalizedTags.Max());
            string LatestVersion = FilteredTags.ElementAt(LatestIndex);

            return new Tuple<string, Exception>("v" + LatestVersion, null);
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

        private HttpResponse MakeRequest(Uri uri)
        {
            var Request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };

            var CancellationTokenSource = new CancellationTokenSource(10000);
            HttpResponseMessage Response = HttpClient.SendAsync(Request, CancellationTokenSource.Token).GetAwaiter().GetResult();
            Stream ResponseStream = Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

            byte[] Buffer = null;
            if (ResponseStream != null)
            {
                Buffer = new byte[ResponseStream.Length];
                using (var StreamReader = new StreamReader(ResponseStream))
                {
                    ResponseStream.Read(Buffer, 0, (int)ResponseStream.Length);
                }
            }

            return new HttpResponse(Response.StatusCode, Response.Headers, Buffer);
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
                string Output = Path.Combine(TempFolder.FullName, Name);

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
        #endregion
    }
}

/*
    List edgedrivers by major revisions
    https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver?comp=list&prefix=100

    List chromedriver by major revisions
    https://chromedriver.storage.googleapis.com/LATEST_RELEASE_XX where XX is the number

    List geckodriver by major revisions
    https://github.com/mozilla/geckodriver/releases?q=0.25&expanded=false
*/