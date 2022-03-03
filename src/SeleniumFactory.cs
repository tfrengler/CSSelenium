using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace TFrengler.CSSelenium
{
    /// <summary>The bootstrapper class for creating a Selenium webdriver-instance representing a specific browser</summary>
    public static class SeleniumFactory
    {
        /// <summary>
        /// Creates a Selenium Webdriver instance for the given browser, using predefined settings (Direct proxy, LocalFileDetector enabled if webdriver URL is not localhost)
        /// </summary>
        /// <param name="browser">The browser you want to create a webdriver instance for. Some prefined settings are used for each browser that may differ from their internal defaults:
        /// <para>FIREFOX: A new, random profile is used which is auto-deleted after use</para>
        ///</param>
        /// <param name="remoteURL">The url of the webdriver. If you make use of <see cref="DriverManager"/> then you get this from <see cref="DriverManager.Start"/></param>
        public static IWebDriver Create(Browser browser, Uri remoteURL)
        {
            return CreateWebdriver(browser, remoteURL);
        }

        /// <summary>
        /// Creates a Selenium Webdriver instance for the given browser - using predefined settings (Direct proxy, LocalFileDetector enabled if webdriver URL is not localhost) - and allows you to pass vendor specific arguments to the browser on startup
        /// </summary>
        /// <param name="browser">The browser you want to create a webdriver instance for. Some prefined settings are used for each browser that may differ from their internal defaults:
        /// <para>FIREFOX: A new, random profile is used which is auto-deleted after use</para>
        /// </param>
        /// <param name="remoteURL">The url of the webdriver. If you make use of <see cref="DriverManager"/> then you get this from <see cref="DriverManager.Start"/></param>
        /// <param name="browserArguments">An array of arguments to be passed to the browser, such as "--headless" for Chrome for example</param>
        public static IWebDriver Create(Browser browser, Uri remoteURL, string[] browserArguments)
        {
            return CreateWebdriver(browser, remoteURL, browserArguments);
        }

        /// <summary>
        /// Creates a Selenium Webdriver instance for the given browser using the options and driver URL you pass. No predefined settings are used. This is the most customizable option.
        /// </summary>
        /// <param name="remoteURL">The url of the webdriver. If you make use of <see cref="DriverManager"/> then you get this from <see cref="DriverManager.Start"/></param>
        /// <param name="options">An instance of 'OpenQa.Selenium.DriverOptions'. This allows you to customize the start-up of the browser yourself. You are able to configure everything, including browser arguments, proxy, PageLoadStrategy, implicit waits etc</param>
        public static IWebDriver Create(Uri remoteURL, DriverOptions options)
        {
            return CreateWebdriver(null, remoteURL, null, options);
        }

        private static IWebDriver CreateWebdriver(Browser? browser, Uri remoteURL, string[] browserArguments = null, DriverOptions options = null)
        {
            var Options = options ?? CreateDriverOptions(browser.Value, browserArguments);
            IWebDriver Webdriver = new RemoteWebDriver(remoteURL, Options);

            if (!remoteURL.IsLoopback)
                ((RemoteWebDriver)Webdriver).FileDetector = new LocalFileDetector();

            return Webdriver;
        }

        private static DriverOptions CreateDriverOptions(Browser browser, string[] browserArguments = null)
        {
            switch (browser)
            {
                case Browser.CHROME:
                    var ChromeOptions = new ChromeOptions();

                    if (browserArguments != null)
                        ChromeOptions.AddArguments(browserArguments);

                    ChromeOptions.Proxy = new Proxy()
                    {
                        IsAutoDetect = false,
                        Kind = ProxyKind.Direct
                    };

                    return ChromeOptions;

                case Browser.FIREFOX:
                    var FirefoxOptions = new FirefoxOptions();

                    if (browserArguments != null)
                        FirefoxOptions.AddArguments(browserArguments);

                    FirefoxOptions.Proxy = new Proxy()
                    {
                        IsAutoDetect = false,
                        Kind = ProxyKind.Direct
                    };

                    FirefoxOptions.Profile = new FirefoxProfile() {DeleteAfterUse = true};

                    return FirefoxOptions;

                case Browser.EDGE:
                    var EdgeOptions = new EdgeOptions();

                    if (browserArguments != null)
                        EdgeOptions.AddArguments(browserArguments);

                    EdgeOptions.Proxy = new Proxy()
                    {
                        IsAutoDetect = false,
                        Kind = ProxyKind.Direct
                    };

                    return EdgeOptions;

                default:
                    throw new NotImplementedException("Fatal error - BROWSER enum not an expected value: " + browser);
            };
        }
    }
}