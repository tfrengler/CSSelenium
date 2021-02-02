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
        public object Tools {get; private set;}

        #region CONSTRUCTORS

        SeleniumWrapper(Browser browser, Uri remoteURL)
        {
            Init(browser, remoteURL, null, null);
        }
        SeleniumWrapper(Browser browser, Uri remoteURL, string[] browserArguments)
        {
            Init(browser, remoteURL, browserArguments, null);
        }
        SeleniumWrapper(Browser browser, Uri remoteURL, DriverOptions options)
        {
            Init(browser, remoteURL, null, options);
        }

        #endregion

        private SeleniumWrapper Init(Browser browser, Uri remoteURL = null, string[] browserArguments = null, DriverOptions options = null)
        {
            var Options = options ?? CreateDriverOptions(browser, browserArguments);
            Webdriver = new RemoteWebDriver(remoteURL, Options);

            if (remoteURL == null)
                remoteURL = new Uri("");

            if (!remoteURL.IsLoopback)
                Webdriver.FileDetector = new LocalFileDetector();

            Webdriver.Manage().Timeouts().PageLoad = new TimeSpan(0, 0, 120);

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