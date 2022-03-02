# CSSelenium

A simple C# bootstrapper for Selenium, designed to get you quickly up and running, with a minimum of fuss.

**NEW VERSION**
Version 2 has been released which is a major refactor of the code. The biggest change is that all the extra, bonus tools and utility functionality has been removed.
It was simply too much to try and maintain and test. I also wanted this library to focus purely on what it was created for: as a bootstrapper for Selenium.

Another big change is that the driver manager can now update webdrivers to specific versions, and not just the latest.
The supported Selenium version is now the new 4+ which means no more support for 3 and the NET framework used has been updated from NET Core 3.1 to NET 5.0.
The SeleniumWrapper has disappeared and been replaced by the static class SeleniumFactory, which has the static method Create() to get a RemoteWebdriver-instance.

This might be for you if:
1. You are a beginner, and/or aren't super technical, and just need to get Selenium up and running.
1. You don't know (or care) much about how Selenium is started or how to get the webdrivers going.
1. You don't want to do the boilerplate code yourself, and your plan for Selenium doesn't rely on highly specialized or advanced management of Selenium and the webdrivers.

**So what does it do?**
- Abstracts away getting an instance of RemoteWebDriver, which is the primary interface for interacting with the browser.
- Abstracts away the finer details of starting and stopping the webdriver executables for a given browser. The webdriver manager uses the DriverService-classes under the hood.
- Aside from hiding the finer details of the instantiation, you get direct access to Selenium's RemoteWebDriver. That's what the factory is for.
- Supports both local and remote webdriver usage. Both of these "modes" are achieved purely via the RemoteWebDriver-class. I specially chose not to use the local browser-driver classes to keep things simple.
- Offers support for Chrome, Firefox, Edge on Windows and Linux. I am not open to write support for any other browsers or platforms at the moment, sorry

**Known issues**

Firefox on Linux may throw an error related to profiles (cannot be loaded or is inaccessible). It seems to have something to do with the profile.ini file being in the snap/mozilla/... folder but selenium tries to find it in the local/bin or usr/bin folders. I haven't been able to find a fix for this yet.

**Disclaimers**
- Constructive feedback is always welcome, though keep in mind this library was written by me, primarily for use by me, and thus it adheres very much to my principles of software architecture.
- This library is provided "as is". I have no roadmap for future features, and bugs will only be fixed when or if I have time for it.

## Compatibility/requirements

1. Selenium **v4.1.0**
1. Written in **.NET 5.0**

Dependencies:
1. Selenium.WebDriver, version 4.1.0
1. Selenium.Support, version 4.1.0
1. Mono.Posix.NETStandard, version 1.0.0
1. SharpCompress, version 0.30.1

Dependencies are all included in releases. If you build this yourself, well I presume you know what you are doing in that case and how to handle the files

## Getting started

There are two principal classes to work with: **SeleniumFactory** and **DriverManager**. Remember what I said earlier about "local" and "remote" mode? If you are not running the webdriver-executables locally then you don't have to care about **DriverManager**. Since I guess that most people's basic usage is running the browser and webdriver on the same machine as the tests that's the example we'll go with:

```c#
    // Need to tell the webdriver manager the directory the webdriver executables live in
    var WebdriverExecutablesLocation = new DirectoryInfo(@"C:\somepath\");
    var Manager = new DriverManager(WebdriverExecutablesLocation);

    // Once you start the webdriver you get the uri that it is running on
    Uri WebdriverURL = Manager.Start(Browser.CHROME);
    OpenQA.Selenium.Remote.RemoteWebdriver Webdriver = SeleniumFactory.Create(Browser.CHROME, WebdriverURL);

    // Don't forget to clean up after yourself! Especially the manager, otherwise you will have orphaned webdriver-processes hanging around.
    Selenium.Webdriver.Quit();
    Manager.Stop(Browser.CHROME);
```

## BONUS: Updating driver binaries

Auto downloading/updating webdrivers can be done like this. Update() has a final argument where you can pass the desired version, but is optional.
If you leave that argument out it simple updates to the latest version available for the given browser.

```c#

    // Update to the newest version
    UpdateResponse UpdateResult = DriverManager.Update(Browser.CHROME, Platform.WINDOWS, Architecture.x86);

    Console.WriteLine("Updated?: " + UpdateResult.Updated);
    Console.WriteLine("Old version: " + UpdateResult.OldVersion);
    Console.WriteLine("New version: " + UpdateResult.NewVersion);

```

Keep in mind that for specific versions it attempts to update to the latest, stable release of the major revision
that matches the version you passed. You can't update to minor revisions. You get an exception if no releases exist
for the version you passed.

```c#

    // Update to a specific version (96)
    UpdateResponse UpdateResult = DriverManager.Update(Browser.CHROME, Platform.WINDOWS, Architecture.x86, 96);
    // This will try to update to the latest stable release of the 96.X.X.X revision

    Console.WriteLine("Updated?: " + UpdateResult.Updated);
    Console.WriteLine("Old version: " + UpdateResult.OldVersion);
    Console.WriteLine("New version: " + UpdateResult.NewVersion);

```