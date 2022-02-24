using System;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace TFrengler.CSSelenium
{
    public static class SeleniumFactory
    {
        /// <summary>
        /// Creates a Selenium Webdriver instance for the given browser, using predefined settings (Direct proxy, LocalFileDetector enabled if webdriver URL is not localhost)
        /// </summary>
        /// <param name="browser">The browser you want to create a webdriver instance for. Some prefined settings are used for each browser that may differ from their internal defaults:
        /// <para>FIREFOX: A new, random profile is used which is auto-deleted after use</para>
        /// <para>EDGE: PageLoadStrategy is set to NORMAL</para>
        ///</param>
        /// <param name="remoteURL">The url of the webdriver. If you make use of <see cref="DriverManager"/> then you get this from <see cref="DriverManager.Start"/></param>
        public static IWebDriver Create(Browser browser, Uri remoteURL)
        {
            return Create(browser, remoteURL);
        }

        /// <summary>
        /// Creates a Selenium Webdriver instance for the given browser, using predefined settings (Direct proxy, LocalFileDetector enabled if webdriver URL is not localhost), and allowing you to pass vendor specific arguments to the browser on startup
        /// </summary>
        /// <param name="browser">The browser this instance of Selenium represents</param>
        /// <param name="remoteURL">The url of the webdriver. If you make use of <see cref="DriverManager"/> then you get this from <see cref="DriverManager.Start"/></param>
        /// <param name="browserArguments">An array of arguments to be passed to the browser, such as "--headless" for Chrome for example</param>
        public static IWebDriver Create(Browser browser, Uri remoteURL, string[] browserArguments)
        {
            return Create(browser, remoteURL, browserArguments);
        }

        /// <summary>
        /// Creates a Selenium Webdriver instance for the given browser using the options you pass
        /// </summary>
        /// <param name="remoteURL">The url of the webdriver. If you make use of <see cref="DriverManager"/> then you get this from <see cref="DriverManager.Start"/></param>
        /// <param name="options">An instance of 'OpenQa.Selenium.DriverOptions'. This allows you to customize the start-up of the browser yourself. You are responsible for setting everything up, including browser arguments, proxies, pageloadstrategy, implicit waits etc</param>
        public static IWebDriver Create(Uri remoteURL, DriverOptions options)
        {
            return Create(null, remoteURL, null, options);
        }

        private static IWebDriver Create(Browser? browser, Uri remoteURL, string[] browserArguments = null, DriverOptions options = null)
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
                    else
                    {
                        var Proxy = new Proxy();
                        Proxy.IsAutoDetect = false;
                        Proxy.Kind = ProxyKind.Direct;
                        ChromeOptions.Proxy = Proxy;
                    }

                    return ChromeOptions;

                case Browser.FIREFOX:
                    var FirefoxOptions = new FirefoxOptions();

                    if (browserArguments != null)
                        FirefoxOptions.AddArguments(browserArguments);
                    else
                    {
                        FirefoxOptions.Proxy.Kind = ProxyKind.Direct;
                        FirefoxOptions.Profile = new FirefoxProfile() {DeleteAfterUse = true};
                    }

                    // Workaround for issue in Selenium 3.141 (https://github.com/SeleniumHQ/selenium/issues/4816)
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    return FirefoxOptions;

                case Browser.EDGE:
                    // Edge has no options for adding arguments atm
                    var EdgeOptions = new EdgeOptions();
                    EdgeOptions.PageLoadStrategy = PageLoadStrategy.Normal;

                    return EdgeOptions;

                default:
                    throw new NotImplementedException("Fatal error - BROWSER enum not an expected value: " + browser);
            };
        }
    }
}