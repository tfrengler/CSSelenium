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
- Abstracts away the finer details of starting and stopping the webdriver executables for a given browser.
- Aside from hiding the details of the instantiation, you get full access to the Selenium-object (RemoteWebDriver-class). This library really is just a thin wrapper around the core webdriver.
- Offers a fairly small suite of tools for performing common actions, and dealing with some common trouble scenarios. Most of these are aimed at beginners or those who are super technical for whatever reason. Some of them may be useful to experienced people as well.
- Supports both local and remote webdriver usage. Both of these "modes" are achieved purely via RemoteWebDriver. I specially chose not to use the local browser-driver classes to keep things simple. More on what this means later for those who don't know.
- Offers support for Chrome, Firefox, Edge and IE. Anything else and you'll have to write your own implementation, sorry.

**Disclaimers**
- This code was written by me, primarily for use by me, and thus it adheres very much to my principles of software architecture. Basically: if you don't like the way it works or how I wrote this then I would thank you kindly to simply walk away rather than offer negativity. Constructive feedback is always welcome of course.
- This library is provided "as is". I have no roadmap for future features, and bugs will only be fixed when and if I feel like it. I don't say this to be mean, but my life is simply too busy to consistently maintain this. I hope you understand.

## Compatibility/requirements

1. Selenium **v3.141** (https://selenium-release.storage.googleapis.com/index.html?path=3.141/)
1. Written in **.NET Core 3.1**

Might work for earlier versions, and possibly .NET 5.0. However the Firefox driver has a [bug related to codepages](https://github.com/SeleniumHQ/selenium/issues/4816), and requires a very specific version of **System.Text.Encoding.CodePages** that I am not sure is supported by other .NET versions. If you aren't automating Firefox this isn't relevant then.


## Getting started

There are two principal classes to work with: **SeleniumWrapper** and **WebdriverManager**. Remember what I said earlier about "local" and "remote" modes? If you are not running the webdriver-executables locally then you don't have to care about **WebdriverManager**. Since I guess that most people's basic usage is running the browser and webdriver on the same machine as the tests that's the example we'll go with:

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

### Global data

**Browser:** _enum_
```c#
{
    EDGE    = 0,
    FIREFOX = 1,
    CHROME  = 2,
    IE      = 3
}
```

It's worth noting that Edge is the legacy version (https://support.microsoft.com/en-us/microsoft-edge/what-is-microsoft-edge-legacy-3e779e55-4c55-08e6-ecc8-2333768c0fb0). Selenium v3 does not offer support for the new edge version.

---

### public sealed class _SeleniumWrapper_ : IDisposable

The primary interface for interacting with browsers, which you do via the public property **Webdriver**.
Don't forget to clean up by calling *Webdriver.Quit()* when you are done using this instance, otherwise the browser may remain open.
_NOTE:_ **Dispose()** also calls Webdriver.Quit()

**CONSTRUCTORS:**
```c#
public SeleniumWrapper(Browser browser, Uri remoteURL)
public SeleniumWrapper(Browser browser, Uri remoteURL, string[] browserArguments)
public SeleniumWrapper(Browser browser, Uri remoteURL, DriverOptions options)
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

Since this library is also aimed at beginners (or experienced people who like a helping hand) I have included a few optional tools. These are pretty lightweight so even if you don't need them there's very little overhead.

These tools come in four flavours:
1. **Element-fetcing**: implemented by **ElementLocator** and **ElementsLocator** - and exposed via **SeleniumWrapper.GetElement** and **SeleniumWrapper.GetElements** respectively - are classes that simplify fetching HTML-elements without having to create selectors yourself. The former fetches single elements, while the latter - unsurprisingly - fetches multiple.
1. **Utility-methods**: implemented by **SeleniumTools** - and exposed via **SeleniumWrapper.Tools** - this is the most opinionated part of this library. It offers a range of methods that could possibly help you out. These are based on frequent challenges I have come up against in my automation career.
1. **Locator-creation**: implemented by the static class **LocatorFactory** it can generate element locators (The **By**-class from Selenium) which are used across a range of different methods in Selenium. ElementLocator and ElementsLocator both make use of this to generate the locators used to fetch elements.
1.**Element-relationships**: implemented as extension methods by **WebElementExtensions**, these methods aim to help with getting elements that are related to an element. These include getting the child elements, parent element, next and previous sibling.

Do note that these tools are not meant to offer high performance or display best practices. They are here to **help** with and **simplify** certain actions that you frequently do with Selenium.

### public sealed class _SeleniumTools_

Tries to offer a range of methods that could possibly help with certain situations, or abstract away certain actions that are done often.

**CONSTRUCTORS:**
```c#
public SeleniumTools(RemoteWebDriver webdriver)
```
Since this is dependency injected into SeleniumWrapper you are discouraged from instantiating this yourself. Of course if you really want to there's no stopping you.

**PROPERTIES:**
- _None_

**METHODS:**

```c#
public string DownloadFile(Uri fileURL, FileStream output)
```

This one should be fairly self-explanatory. It attempts to download a file into the stream provided. If the link does not return 200 OK an exception will be thrown. The same if the response stream cannot be read. It makes no judgement about what is returned from the URL and will blindly copy the response to the file.

Downloading files programmatically is already tricky business, and this is simply my attempt at making a somewhat generic approach. It does this by doing an HTTP-request to the URL, emulating whatever cookies are available on the current page. In a lot of cases that might be enough, but there are plenty of places it won't work. Be warned.

```c#
public void ScrollToElement(IWebElement element)
```

Attempts to scroll to the element, which may not always succeed (scrolling to elements is finicky). It does so using Selenium's **Action**-class.

```c#
public void ClickElementUsingJS(IWebElement element)
```

Clicking on an element, but using Javascript to invoke the element's click-event as opposed to simulate clicking on it with your mouse. There are situations where this can be useful.

```c#
public void DropdownSelect(By dropdownSelector, string option, ISearchContext context = null, TimeSpan? timeout = null)
```

Dynamically loaded dropdowns can be a pain to deal with. This attempts to take a more robust (if not exactly elegant) approach. It will ignore all exceptions within the timeout while trying to fetch the dropdown, selecting the option and verifying that the selected option is the one you chose.

You have to pass it the locator of the dropdown, and optionally the context to search within (ie. another element) if that can't be achieved via the locator itself. Defaults to trying for 10 seconds.

```c#
public bool StandardWait(Func<IWebDriver, bool> waitFunction, TimeSpan? timeout = null)
```

Perhaps for more experienced users, this is a timeout-method which wraps Selenium's **WebDriverWait** and is "usually" good enough for most situations. Ignores most common exceptions related to elements not being found, disabled, non-interactable etc. Returns a boolean allowing you to decide how to react to the eventual success or failure.

You are only required to supply the wait condition yourself, which is a lambda-function that should **true** on success, and **false** on failure. Defaults to 10 seconds.

```c#
public void ClickElementRepeatedly(By element, TimeSpan? timeout = null)
```

A somewhat dumb, brute force helper method that keeps trying to click an element until no exceptions are thrown. Defaults to 10 seconds.

```c#
public void ClickElementUntil(By element, Func<IWebDriver, bool> waitCondition, TimeSpan? timeout = null)
```

Similar to above, except this one keeps trying until a certain condition (which you supply via a lambda-function) is met. Defaults to 10 seconds.

---

### public sealed class _ElementLocator_

Helper class that tries to simplify fetching HTML-elements via Selenium from the browser.

**CONSTRUCTORS:**
```c#
public ElementLocator(ISearchContext webdriver)
```
Since this is dependency injected into SeleniumWrapper you are discouraged from instantiating this yourself. Of course if you really want to there's no stopping you.

**PROPERTIES:**
- _None_

**METHODS:**
```c#
public IWebElement ByTagName(string elementType);
public IWebElement ByTitle(string title, string elementType = null);
public IWebElement ById(string id, string elementType = null);
public IWebElement ByClass(string className, string elementType = null);
public IWebElement ByName(string name, string elementType = null);
public IWebElement ByTextEquals(string text, string elementType = null);
public IWebElement ByTextContains(string text, string elementType = null);
public IWebElement ByInputType(string type, string elementType = null);
public IWebElement ByValue(string value, string elementType = null);
public IWebElement ByAttributeEquals(string attribute, string value, string elementType = null);
public IWebElement ByAttributeStartsWith(string attribute, string value, string elementType = null);
public IWebElement ByAttributeEndsWith(string attribute, string value, string elementType = null);
public IWebElement ByAttributeContains(string attribute, string value, string elementType = null);
```
All of the above methods do the same thing: find and return an HTML-element. The only difference is which attribute/element-type they target in their search.
Argument **elementType** is for all methods (with the exception of **ByTagName()**) optional. This allows you to restrict the search for elements to a specific tag, ie. "input", "div" etc.

```c#
public ElementLocator Within(IWebElement context);
```
This is a bit special. They returns a new instance **ElementLocator** that is configured to only search for elements within (descendants of) the element you pass to it.
Here's an example that might make its use clearer:

```c#
    // Get an element first
    IWebElement BulletList = SeleniumWrapper.GetElement.ById("MyFancyList", "ul");
    
    // Now I want to constrain my search to within this element
    ReadOnlyCollection<IWebElement> ListElements = SeleniumWrapper.GetElements.Within(BulletList).ByTagName("li");
```

Not all webpages are well made in terms of their markup (some are even actively hostile to automation testers...). You may come across scenarios where elements are not easily identifiable when searching globally, and sometimes limiting the search to within some element/container/wrapper can help you out.

_NOTE:_ Yes, I am aware this incurs the overhead of an extra call to Selenium via **FindElement(s)** and is definitely less performant than constructing an Xpath- or CSSSelector-locators that can do this in one go. Remember these methods are about easy of use, not performance.

---

### public sealed class _ElementsLocator_

This is a mirror image of **ElementLocator**, with exactly the same methods. They simply return multiple elements in the form of an **ReadOnlyCollection<IWebElement>** instance, instead of a single **IWebElement** instance.<br/>
_NOTE:_ This can also be used to check for elements. Since attempting to fetch a single element throws an exception if it can't be found, an alternative to using try/catch is to try fetching multiple. Then you simply check if the **Count**-property of the returned ReadOnlyCollection is greater than 0.

### public static class _WebElementExtensions_

Extensions to **IWebElement** designed to help getting elements that are related to the current element. These are all implemented using Xpath axis'.

```c#
public static ReadOnlyCollection<IWebElement> GetDirectChildren(this IWebElement element, string elementType = null);
public static ReadOnlyCollection<IWebElement> GetDescendants(this IWebElement element, string elementType = null);
public static IWebElement GetParent(this IWebElement element);
public static IWebElement GetPreviousSiblingElement(this IWebElement element);
public static IWebElement GetNextSiblingElement(this IWebElement element);
```

All of these should be pretty self-explanatory. Some of you may wonder what the difference is between **children** and **descendants**. To keep it short: children are elements that are _directly_ nested within another element. Descendants are ALL elements nested within an element, even if they are nested within other sub-elements.

By way of example:
```html
    <section>
        <div>First</div>
        <div>
            <p>Second</p>
        </div>
        <div>
            <p>
                Third
                <span>Fourth</span>
            </p>
        <div>
    </section>
```

If you call **GetDirectChildren()** on **section** you'll only get the **divs**. If you call **GetDescendants()** you'll get both the **divs**, **p's** and the **span**.
