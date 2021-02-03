using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;

namespace TFrengler.Selenium
{
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

        public Uri Start(Browser browser, bool killExisting = false, ushort port = 0)
        {
            string DriverName;
            lock(DriverNames.SyncRoot)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    DriverName = DriverNames[(int)browser] + ".exe";
                else
                    DriverName = DriverNames[(int)browser];
            }

            DriverService Service;
            lock(DriverServices.SyncRoot)
            {
                Service = DriverServices[(int)browser];
            }

            if (!killExisting && Service != null)
            {
                Dispose();
                throw new Exception("Unable to start browser driver as it appears to already be running");
            }

            if (killExisting && Service != null)
                Stop(browser);

            var DriverExecutable = new FileInfo(FileLocation.FullName + "/" + DriverName);
            if (!DriverExecutable.Exists)
                throw new Exception($"Cannot start browser driver - executable does not exist ({DriverExecutable.FullName})");

            if (DriverExecutable.IsReadOnly)
                throw new Exception($"Cannot start browser driver - executable is read-only ({DriverExecutable.FullName})");

            Service = browser switch
            {
                Browser.CHROME => ChromeDriverService.CreateDefaultService(FileLocation.FullName),
                Browser.FIREFOX => FirefoxDriverService.CreateDefaultService(FileLocation.FullName),
                Browser.EDGE => EdgeDriverService.CreateDefaultService(FileLocation.FullName),
                _ => throw new NotImplementedException()
            };

            if (port > 0) Service.Port = port;
            Service.Start();

            lock(DriverServices.SyncRoot)
            {
                DriverServices[(int)browser] = Service;
            }

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

        public bool Stop(Browser browser)
        {
            lock(DriverServices.SyncRoot)
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

        public void Dispose()
        {
            Stop(Browser.EDGE);
            Stop(Browser.FIREFOX);
            Stop(Browser.CHROME);
        }
    }
}