using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;

namespace TFrengler.Selenium
{
    public struct DriverData
    {
        public string Browser;
        public ushort DefaultPort;
        public string ExecutableName;
    }

    public sealed class WebdriverManager : IDisposable
    {
        private readonly DirectoryInfo FileLocation;
        private readonly DriverService[] DriverServices;
        private readonly string[] DriverNames; 

        public WebdriverManager(DirectoryInfo fileLocation)
        {
            DriverNames = new string[3] { "msedgedriver","geckodriver","chromedriver" };
            DriverServices = new DriverService[3];
            FileLocation = fileLocation;

            if (!FileLocation.Exists)
                throw new Exception("Unable to instantiate BrowserDriver. Directory with drivers does not exist: " + fileLocation.FullName);
        }

        public Uri Start(Browser browser, ushort port)
        {
            string DriverName;
            lock(DriverNames.SyncRoot)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    DriverName = DriverNames[(int)browser] + ".exe";
                else
                    DriverName = DriverNames[(int)browser];
            }

            string PathToDriver = FileLocation.FullName + "/" + DriverName;
            if (File.Exists(PathToDriver))
                throw new Exception($"Cannot start browser driver: executable does not exist ({PathToDriver})");

            DriverService Service = browser switch
            {
                Browser.CHROME => ChromeDriverService.CreateDefaultService(PathToDriver),
                Browser.FIREFOX => FirefoxDriverService.CreateDefaultService(PathToDriver),
                Browser.EDGE => EdgeDriverService.CreateDefaultService(PathToDriver),
                _ => throw new NotImplementedException()
            };

            if (port > 0) Service.Port = port;
            Service.Start();

            return Service.ServiceUrl;
        }

        public bool IsRunning(Browser browser)
        {
            lock(DriverServices.SyncRoot)
            {
                DriverService Service = DriverServices[(int)browser];
                if (Service == null) return false;
                return Service.IsRunning;
            }
        }

        public void Stop(Browser browser)
        {
            lock(DriverServices.SyncRoot)
            {
                DriverService Service = DriverServices[(int)browser];
                if (Service != null) Service.Dispose();
            }
        }

        public void Dispose()
        {
            Stop(Browser.EDGE);
            Stop(Browser.FIREFOX);
            Stop(Browser.CHROME);
        }
    }
}