using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;

namespace TFrengler.Selenium
{
    public enum Browser
    {
        EDGE    = 0,
        FIREFOX = 1,
        CHROME  = 2,
        IE      = 3
    }

    public sealed class SeleniumWrapper : IDisposable
    {
        public Browser Browser {get; private set;}
        public RemoteWebDriver Webdriver {get; private set;}
        public SeleniumTools Tools {get; private set;}
        public ElementLocator GetElement {get; private set;}
        public ElementsLocator GetElements {get; private set;}

        #region CONSTRUCTORS

        public SeleniumWrapper(Browser browser, Uri remoteURL)
        {
            Create(browser, remoteURL);
        }

        public SeleniumWrapper(Browser browser, Uri remoteURL, string[] browserArguments)
        {
            Create(browser, remoteURL, browserArguments);
        }

        public SeleniumWrapper(Browser browser, Uri remoteURL, DriverOptions options)
        {
            Create(browser, remoteURL, null, options);
        }

        #endregion

        private SeleniumWrapper Create(Browser browser, Uri remoteURL, string[] browserArguments = null, DriverOptions options = null)
        {
            var Options = options ?? CreateDriverOptions(browser, browserArguments);
            Webdriver = new RemoteWebDriver(remoteURL, Options);

            GetElement = new ElementLocator(Webdriver);
            GetElements = new ElementsLocator(Webdriver);
            Tools = new SeleniumTools(Webdriver);

            if (!remoteURL.IsLoopback)
                Webdriver.FileDetector = new LocalFileDetector();

            return this;
        }

        private DriverOptions CreateDriverOptions(Browser browser, string[] browserArguments = null)
        {
            switch (browser)
            {
                case Browser.CHROME:

                    var ChromeOptions = new ChromeOptions();
                    if (browserArguments != null)
                        ChromeOptions.AddArguments(browserArguments);

                    return ChromeOptions;

                case Browser.FIREFOX:
                    var FirefoxOptions = new FirefoxOptions();
                    if (browserArguments != null)
                        FirefoxOptions.AddArguments(browserArguments);
                    FirefoxOptions.Profile = new FirefoxProfile();

                    return FirefoxOptions;

                case Browser.EDGE:
                    // TODO(thomas): Edge has no options for adding arguments atm
                    var EdgeOptions = new EdgeOptions();
                    EdgeOptions.PageLoadStrategy = PageLoadStrategy.Normal;

                    return EdgeOptions;

                case Browser.IE:
                    var IEOptions = new InternetExplorerOptions();
                    if (browserArguments != null)
                    {
                        IEOptions.ForceCreateProcessApi = true;
                        IEOptions.BrowserCommandLineArguments = string.Join(" ", browserArguments);
                    }

                    return IEOptions;

                default:
                    throw new NotImplementedException();
            };
        }

        public void Dispose()
        {
            if (Webdriver != null) Webdriver.Quit();
        }
    }
}