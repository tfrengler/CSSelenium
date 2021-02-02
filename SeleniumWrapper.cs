using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace TFrengler.Selenium
{
    public enum Browser
    {
        EDGE    = 0,
        FIREFOX = 1,
        CHROME  = 2
    }

    public sealed class SeleniumWrapper : IDisposable
    {
        public Browser Browser {get; private set;}
        public RemoteWebDriver Webdriver {get; private set;}
        
        //public object Tools {get; private set;}
        public ElementLocator GetElements {get; private set;}

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
            GetElements = new ElementLocator(Webdriver);

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