using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Chrome;

namespace TFrengler.Selenium
{
    public struct DriverData
    {
        public string Browser;
        public ushort DefaultPort;
        public string ExecutableName;
    }

    public sealed class BrowserDriverManager : IDisposable
    {
        private readonly Process[] DriverProcesses;
        private readonly DriverData[] Drivers;
        private readonly DirectoryInfo FileLocation;

        public BrowserDriverManager(DirectoryInfo fileLocation)
        {
            DriverProcesses = new Process[3];
            FileLocation = fileLocation;
            Drivers = new DriverData[]
            {
                new DriverData
                {
                    Browser = "Microsoft Edge",
                    DefaultPort = 9515,
                    ExecutableName = "msedgedriver"
                },
                new DriverData
                {
                    Browser = "Mozilla Firefox",
                    DefaultPort = 4444,
                    ExecutableName = "geckodriver"
                },
                new DriverData
                {
                    Browser = "Google Chrome",
                    DefaultPort = 9515,
                    ExecutableName = "msedgedriver"
                }
            };

            if (!fileLocation.Exists)
                throw new Exception("Unable to instantiate BrowserDriver. Driver location directory does not exist: " + fileLocation.FullName);
        }

        public bool Start(Browser browser, bool killExisting = false)
        {
            Process DriverProcess;
            lock(DriverProcesses.SyncRoot)
            {
                DriverProcess = DriverProcesses[(int)browser];
            }

            if (DriverProcess != null)
            {
                if (!killExisting)
                    return false;
                else
                    Kill(browser);
            }

            string DriverName;
            lock(Drivers.SyncRoot)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    DriverName = Drivers[(int)browser].ExecutableName + ".exe";
                else
                    DriverName = Drivers[(int)browser].ExecutableName;
            }

            string PathToDriver = FileLocation.FullName + "/" + DriverName;
            if (File.Exists(PathToDriver))
                throw new Exception($"Cannot start browser driver: executable does not exist ({PathToDriver})");

            DriverProcess = Process.Start(PathToDriver);
            lock(DriverProcesses.SyncRoot)
            {
                DriverProcesses[(int)browser] = DriverProcess;
            }

            return true;
        }

        public bool Kill(Browser browser)
        {
            Process DriverProcess;
            lock(DriverProcesses.SyncRoot)
            {
                DriverProcess = DriverProcesses[(int)browser];
            }

            if (DriverProcess == null)
                return false;

            DriverProcess.Kill(true);
            DriverProcess.WaitForExit();

            return true;
        }

        public void Dispose()
        {
            var test = ChromeDriverService.CreateDefaultService("");
            test.Port = 3333;
            test.Start();

            foreach(int Browser in Enum.GetValues(typeof(Browser)))
                Kill((Browser)Browser);
        }
    }
}