
#r "C:/Dev/Projects/CSSelenium/src/bin/Debug/net5.0/CSSelenium.dll"
#r "C:\Users\thoma\.nuget\packages\selenium.webdriver\4.1.0\lib\net5.0\WebDriver.dll"
#r "C:\Users\thoma\.nuget\packages\mono.posix.netstandard\1.0.0\ref\netstandard2.0\Mono.Posix.NETStandard.dll"
#r "C:\Users\thoma\.nuget\packages\sharpcompress\0.30.1\lib\net5.0\SharpCompress.dll"

using System.IO;
using System.Reflection;
using System.Linq;
using System;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net;
using System.Xml.Linq;
using TFrengler.CSSelenium;

DriverManager DriverManager;

try {
    DriverManager = new DriverManager( new DirectoryInfo("C:/Temp/DownloadedWebdrivers/") );
    // DriverManager.Reset();

    UpdateResponse UpdateResult;

    DriverManager.Start(Browser.EDGE);
    Console.WriteLine(DriverManager.IsRunning(Browser.EDGE));

    // Console.WriteLine("-------CHROME / WINDOWS-------");
    // UpdateResult = DriverManager.Update(Browser.CHROME, Platform.WINDOWS, Architecture.x86, 0);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // UpdateResult = DriverManager.Update(Browser.CHROME, Platform.WINDOWS, Architecture.x86, 96);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // Console.WriteLine("-------FIREFOX / WINDOWS-------");
    // UpdateResult = DriverManager.Update(Browser.FIREFOX, Platform.WINDOWS, Architecture.x86, 0);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // Console.WriteLine("-------EDGE / WINDOWS-------");
    // UpdateResult = DriverManager.Update(Browser.EDGE, Platform.WINDOWS, Architecture.x86, 94);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // Console.WriteLine("-------CHROME / LINUX-------");
    // UpdateResult = DriverManager.Update(Browser.CHROME, Platform.LINUX, Architecture.x64, 0);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // Console.WriteLine("-------FIREFOX / LINUX-------");
    // UpdateResult = DriverManager.Update(Browser.FIREFOX, Platform.LINUX, Architecture.x86, 0);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // Console.WriteLine("-------EDGE / LINUX-------");
    // UpdateResult = DriverManager.Update(Browser.EDGE, Platform.LINUX, Architecture.x86, 94);
    // Console.WriteLine("Updated: " + UpdateResult.Updated);
    // Console.WriteLine("OldVersion: " + UpdateResult.OldVersion);
    // Console.WriteLine("NewVersion: " + UpdateResult.NewVersion);

    // DriverManager.Start(Browser.EDGE);
    // Console.WriteLine(DriverManager.IsRunning(Browser.EDGE));
}
catch(Exception)
{
    throw;
}
finally
{
    Console.WriteLine("All done");
    DriverManager?.Dispose();
}