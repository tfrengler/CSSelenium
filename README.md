# CSSelenium

A simple C# wrapper for Selenium, designed to get you quickly up and running, with a minimum of fuss.

This might be for you if:
1. You are a beginner, and/or aren't super technical, and just need to get Selenium up and running.
1. You don't know (or care) much about how Selenium is started or how to get the webdrivers going.
1. You don't want to do the boilerplate code yourself, and your plan for Selenium doesn't rely on highly specialized or advanced management of Selenium and the webdrivers.
1. You really don't mind not having 100% control over how Selenium instantiation works or how the webdrivers are managed.

**A bit of background...**

My name is Thomas and I'm a fairly experienced automation tester who has created half a dozen frameworks (often from scratch) powered by Selenium for a variety of browser-based projects. I was getting tired of repeatedly writing the same boilerplate code for Selenium over and over again. Since I have personally never worked on a project that required more than "have the automation tests interact with one or two browser types" I decided to make a library for myself that I could reuse, and thought that perhaps it might be useful for someone else.

**So what does it do?**
- Abstracts away getting an instance of RemoteWebDriver, which is the primary interface for interacting with the browser.
- Abstracts away the finer details of starting and stopping the webdriver executables for a given browser. The webdriver manager uses the DriverService-classes under the hood.
- Aside from hiding the finer details of the instantiation, you get full access to the RemoteWebDriver via the wrapper.
- Supports both local and remote webdriver usage. Both of these "modes" are achieved purely via the RemoteWebDriver-class. I specially chose not to use the local browser-driver classes to keep things simple.
- Offers support for Chrome, Firefox, Edge and IE. Anything else and you'll have to write your own implementation, sorry.
- As a bonus, I included a fairly small suite of tools for performing common actions, and dealing with some common trouble scenarios. Most of these are aimed at beginners or those who may not be that technical. Some of them may be useful to experienced people as well, or just to get ideas on how to do similar things yourself.

**Disclaimers**
- Constructive feedback is always welcome, though keep in mind this library was written by me, primarily for use by me, and thus it adheres very much to my principles of software architecture. 
- This library is provided "as is". I have no roadmap for future features, and bugs will only be fixed when or if I have time for it.

## Compatibility/requirements

1. Selenium **v3.141** (https://selenium-release.storage.googleapis.com/index.html?path=3.141/)
1. Written in **.NET Core 3.1**

Dependencies:
1. System.Text.Encoding.CodePages.dll (4.7.1)
1. Newtonsoft.Json.dll (10.0.3)
1. WebDriver.dll (3.141.0)
1. Webdriver.Support.dll (3.141.0)

Dependencies are all included in releases. If you build this yourself, well I presume you know what you are doing in that case and how to handle the files. The latter two DLL's are Selenium's so if your project already includes those then you can skip them.

The whole thing _might_ work for earlier versions, and possibly .NET 5.0. However the Firefox driver has a [bug related to codepages](https://github.com/SeleniumHQ/selenium/issues/4816), and requires a very specific version of **System.Text.Encoding.CodePages** that I am not sure is supported by other .NET versions. If you aren't automating Firefox this isn't relevant then.


## Getting started

There are two principal classes to work with: **SeleniumWrapper** and **WebdriverManager**. Remember what I said earlier about "local" and "remote" mode? If you are not running the webdriver-executables locally then you don't have to care about **WebdriverManager**. Since I guess that most people's basic usage is running the browser and webdriver on the same machine as the tests that's the example we'll go with:

```c#
    // Need to tell the webdriver manager the directory the webdriver executables live in
    var WebdriverExecutablesLocation = new DirectoryInfo("C:\somepath\");
    var Manager = new WebdriverManager(WebdriverExecutablesLocation);

    // Once you start the webdriver you get the uri that it is running on
    Uri WebdriverURL = Manager.Start(Browser.CHROME);
    var Selenium = new SeleniumWrapper(Browser.CHROME, WebdriverURL);

    Selenium.Webdriver.Navigate().GoToUrl("https://www.google.com");

    // Using the built in tools to get elements
    IWebElement SearchBar = Selenium.GetElement.ByTitle("search", "input");
    SearchBar.SendKeys("Selenium website");
    Selenium.GetElement.ByTitle("Google search", "input").Click();

    // Don't forget to clean up after yourself! Especially the manager, otherwise you will have orphaned webdriver-processes hanging around.
    Selenium.Webdriver.Quit();
    Manager.Stop(Browser.CHROME);
```

Again, if you are running the webdrivers somewhere else (presumably via Selenium Standalone Server) you can ignore the **WebdriverManager** entirely. Just pass in the URL of your webdriver-server/location to **SeleniumWrapper()**.

## Technical overview (classes, public methods, properties etc)

### Namespaces
- **TFrengler.Selenium**
- **TFrengler.Selenium.Extensions**

### Global data

**Browser:** _enum_
```c#
{
    EDGE    = 0,
    FIREFOX = 1,
    CHROME  = 2,
    IE11    = 3
}
```

It's worth noting that Edge is the legacy version (https://support.microsoft.com/en-us/microsoft-edge/what-is-microsoft-edge-legacy-3e779e55-4c55-08e6-ecc8-2333768c0fb0). Selenium v3 does not offer support for the new edge version, as far as I know anyway.

Also worth noting is that IE is quirky and can be hard to get to cooperate. And it requires more work than simply starting the driver and interfacing with it via Selenium: https://github.com/SeleniumHQ/selenium/wiki/InternetExplorerDriver#required-configuration

---

### public sealed class _SeleniumWrapper_ : IDisposable

The primary interface for interacting with browsers, which you do via the public property **Webdriver**.
Don't forget to clean up by calling *Webdriver.Quit()* when you are done using this instance, otherwise the browser may remain open.
_NOTE:_ **Dispose()** also calls Webdriver.Quit()

**CONSTRUCTORS:**
```c#
public SeleniumWrapper(Browser browser, Uri remoteURL)
public SeleniumWrapper(Browser browser, Uri remoteURL, string[] browserArguments)
public SeleniumWrapper(Uri remoteURL, DriverOptions options)
```
The three constructors offer you a few choices about the Selenium-instance's configuration.<br/>
The first constructor is the most simple to use, just requiring to know which browser you want to interact with and the URL to the webdriver it should use to communicate with that browser.
The second allows you to pass a list of command-line arguments to the browser itself, like **--headless** for Chromium-engine browsers if you wanted it to start without a GUI.<br/>
_NOTE:_ Don't forget to add command-line qualifiers in front of the switches (such as -- for the Chromium-engine ones).

The last one is the most customizable. In case you really need more more control you can create your own instance of your respective browser's sub-class of DriverOptions. This will allow you to add a proxy server, deal with browser extensions, configure logging etc. Keep in mind that each browser's DriverOptions-subclass has different options. Edge for example doesn't allow you to pass command-line options (at least not as of the time I am writing this, with Selenium at version 3.141). I suspect this option is mostly for people who are well versed in Selenium and therefore less likely to be using my library anyway. But in any case, it's there as an option.

**PROPERTIES:**
```c#
public Browser Browser {get; private set;}
public RemoteWebDriver Webdriver {get; private set;}
public SeleniumTools Tools {get; private set;}
public ElementLocator GetElement {get; private set;}
public ElementsLocator GetElements {get; private set;}
```
See further below for information about **Tools**, **GetElement** and **GetElements**.

**METHODS**

```c#
public void Dispose()
```
Quits the webdriver-instance if it's still alive

---

### public sealed class _WebdriverManager_ : IDisposable

This class is for managing the webdrivers, effectively wrapping Selenium's **DriverService**-classes. It's not entirely for managing the lifecycle since you as the consumer still has to start and stop them via the provided methods. What this class does is hide away the details of how the drivers are managed. All you have to is tell the class on instantiation which folder they live in, and then you can start and stop them yourself. _NOTE:_ **Dispose()** also tries to kill all the webdrivers.

A few things to note. As is hopefully clear this class is meant for the basic use case where the machine that runs the tests (via SeleniumWrapper.Webdriver) also interacts with the browser (via the webdrivers). It also only allows you to start one instance of each driver. It should be noted that a single webdriver executable can easily keep up with communication to half a dozen browsers and Selenium-instances - if you really want (or for some reason need) multiple instances then you need to implement a system for managing the drivers yourself.
Although intended as a singleton there's nothing preventing you from making multiple instances of this and thus starting browser drivers multiple times. I leave that at your discretion.

_NOTE:_ I have tried to make this as threadsafe as possible, but I haven't thoroughly tested this.

**CONSTRUCTORS:**
```c#
public WebdriverManager(DirectoryInfo fileLocation)
```
Argument **fileLocation** should point to the directory where the webdrivers are located.
_NOTE:_ Don't rename the executables! This library, as well as the internal **DriverService**-class it wraps, expects the original file names (chromedriver, geckodriver etc)

**PROPERTIES:**
* _None_

**METHODS:**

```c#
public Uri Start(Browser browser, bool killExisting = false, ushort port = 0)
```
If you call this with **killExisting** set to **false** - and the driver has already been started - an exception will be thrown.<br/>
If you call this with **killExisting** set to **true** - and the driver has already been started - it will attempt to kill the existing driver first.<br/>
Argument **port** is optional, and the default 0 means it will chose a random, free port to run on. If the port cannot be bound an exception will be thrown.

```c#
public bool IsRunning(Browser browser)
```
Returns **true** if the webdriver is running (the wrapped DriverService has been instantiated and start has been called on it without exceptions being thrown)

```c#
public bool Stop(Browser browser)
```
Should be self-explanatory. Once a browser has been stopped, it can be started again using the same instance.<br/>
Returns a boolean to indicate whether the browser driver was stopped, or not. It is therefore safe to call this on a browser that hasn't been started.

```c#
public void Dispose()
```
This will attempt to kill all running webdrivers

## BONUS: Tools and helpers

These are the classes **LocatorFactory**, **SeleniumTools**, **WebdriverExtensions** and **ElementLocator/ElementsLocator**
The short version:
- LocatorFactory: a static class for creating **locators** (Selenium By-class instances) that are used by Selenium's **FindElement()** and **FindElements()** methods.
- SeleniumTools: a class with specialized (and opinionated) tools that I have personally used a lot over the years. Exposed via **SeleniumWrapper.Tools**
- WebdriverExtensions: adds a few extension methods to IWebElement, mostly for getting elements related to the current element (such as **GetDirectChildren()**, **GetParent()**, **GetNextSiblingElement()** etc). To use these you must include the namespace **TFrengler.Selenium.Extensions**
- ElementLocator/ElementsLocator: classes with convenience methods for fetching elements via single attributes, such as **ById()**, **ByClass**. Exposed via **SeleniumWrapper.GetElement** and **SeleniumWrapper.GetElements**

# TODO:

I know I said there's no roadmap but there might still be things I'd like to change/add when I get the chance:
- The option to auto-download/update webdrivers<br/>
I once saw this done by another Selenium framework and it was awesome. I'm pretty sure this could be pulled off but it's a bigger deal than just adding a few new methods.
- Logging
Don't want to make it complicated. I'll probably do this via an event that you can hook into and then you can decide what to do with the log messages.
