#load "Core.csx"

using TFrengler.Selenium;

// WINDOWS x86
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.CHROME, Platform.WINDOWS, Architecture.x86));
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.FIREFOX, Platform.WINDOWS, Architecture.x86));
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.EDGE, Platform.WINDOWS, Architecture.x86));

// WINDOWS x64
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.CHROME, Platform.WINDOWS, Architecture.x64)); // Expected error: Chrome on Linux only supports x86
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.FIREFOX, Platform.WINDOWS, Architecture.x64));
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.EDGE, Platform.WINDOWS, Architecture.x64));

// LINUX
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.CHROME, Platform.LINUX, Architecture.x64));
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.FIREFOX, Platform.LINUX, Architecture.x64));
// WriteLine(Webdrivers.GetLatestWebdriverBinary(Browser.EDGE, Platform.LINUX, Architecture.x64)); // Expected error: Edge is not available on Linux