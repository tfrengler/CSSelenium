using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace TFrengler.Selenium
{
    public sealed class SeleniumTools
    {
        private readonly RemoteWebDriver WebDriver;

        public SeleniumTools(RemoteWebDriver webdriver)
        {
            WebDriver = webdriver;
        }

        public string DownloadFile(Uri FileURL, FileStream output)
        {
            string FileName = Guid.NewGuid().ToString();
            ICookieJar ExistingCookies = WebDriver.Manage().Cookies;

            HttpWebRequest AttachmentDownloadRequest = (HttpWebRequest)WebRequest.Create(FileURL);
            AttachmentDownloadRequest.Method = "GET";
            AttachmentDownloadRequest.Accept = "*/*"; // image/bmp,image/jpeg,image/png,image/webp
            AttachmentDownloadRequest.AllowAutoRedirect = false;
            AttachmentDownloadRequest.UserAgent = (string)WebDriver.ExecuteScript("return navigator.userAgent;");
            AttachmentDownloadRequest.CookieContainer = new CookieContainer();

            // We need to imitate all the cookies from the application otherwise our request will be refused
            foreach (var CurrentCookie in ExistingCookies.AllCookies)
                AttachmentDownloadRequest.CookieContainer.Add(new System.Net.Cookie(CurrentCookie.Name, CurrentCookie.Value) { Domain = FileURL.Host });

            HttpWebResponse AttachmentDownloadResponse = (HttpWebResponse)AttachmentDownloadRequest.GetResponse();
            if (AttachmentDownloadResponse.StatusCode != HttpStatusCode.OK)
            {
                AttachmentDownloadResponse.Dispose();
                output.Dispose();
                throw new Exception("Unable to download file, request status is not '200 OK': " + AttachmentDownloadResponse.StatusCode);
            }

            AttachmentDownloadResponse.GetResponseStream().CopyTo(output);

            // CLEANUP
            output.Flush();
            output.Dispose();
            AttachmentDownloadResponse.Dispose();

            return FileName;
        }

        public void ScrollToElement(IWebElement element)
        {
            var Actions = new Actions(WebDriver);
            Actions.MoveToElement(element);
        }

        public void ClickElementUsingJS(IWebElement element)
        {
            WebDriver.ExecuteScript("arguments[0].click()", new object[] { element });
        }

        /// <summary>
        /// Helper method to deal with dropdown-selections that are dynamically populated
        /// </summary>
        /// <param name="dropdownSelector">The dropdown-selector used to grab the select-element you want to interact with</param>
        /// <param name="option">The text of the option you want to select</param>
        /// <param name="context">The context (element) within which to search for the dropdown-selector</param>
        /// <param name="timeout">An optional timeout for how long to try selecting the option. Defaults to 10 seconds</param>
        public void DropdownSelect(By dropdownSelector, string option, ISearchContext context = null, TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = TimeSpan.FromSeconds(10.0f);

            if (context == null)
                context = WebDriver;

            bool Success = StandardWait((webdriver) =>
            {
                var DropDownElement = new SelectElement(context.FindElement(dropdownSelector));
                DropDownElement.SelectByText(option);
                Thread.Sleep(100);

                if (DropDownElement.SelectedOption.Text == option)
                    return true;

                return false;
            }, timeout);

            if (!Success)
                throw new Exception($"Timed out trying to select option '{option}' in dropdown ({timeout.Value.TotalSeconds} seconds)");
        }

        /// <summary>
        /// A timeout-method which wraps WebDriverWait and is usually good enough for most situations. Ignores most common exceptions related to elements not being found, disabled, non-interactable etc. Returns a boolean allowing you to decide how to react to the eventual success or failure.
        /// </summary>
        /// <param name="waitFunction">The lambda function to continously execute until it returns either true or false (the former signifying success)</param>
        /// <param name="timeout">The amount of time to wait before timing out. Optional, defaults to 10 seconds</param>
        /// <returns>True if the wait function didn't time out, and false otherwise</returns>
        public bool StandardWait(Func<IWebDriver, bool> waitFunction, TimeSpan? timeout = null)
        {
            if (timeout == null) timeout = TimeSpan.FromSeconds(10.0d);

            var Wait = new WebDriverWait(WebDriver, (TimeSpan)timeout);
            Wait.IgnoreExceptionTypes(new Type[] { typeof(StaleElementReferenceException), typeof(NotFoundException), typeof(NoSuchElementException), typeof(InvalidElementStateException) });

            try
            {
                Wait.Until(waitFunction);
                return true;
            }
            catch(WebDriverTimeoutException)
            {
                return false;
            }
        }

        public void ClearCookies()
        {
            WebDriver.Manage().Cookies.DeleteAllCookies();
        }

        /// <summary>
        /// A somewhat dumb, brute force helper method that keeps trying to click an element until no exceptions are thrown
        /// </summary>
        /// <param name="element">The selector for the element you want to click</param>
        /// <param name="timeout">The amount of time to try before timing out. Optional, defaults to 10 seconds</param>
        public void ClickElementRepeatedly(By element, TimeSpan? timeout = null)
        {
            if (timeout == null) timeout = TimeSpan.FromSeconds(10.0d);

            var Wait = new WebDriverWait(WebDriver, (TimeSpan)timeout);
            Wait.IgnoreExceptionTypes(new Type[] { typeof(StaleElementReferenceException), typeof(NotFoundException), typeof(NoSuchElementException), typeof(InvalidElementStateException), typeof(ElementClickInterceptedException) });

            Wait.Until((webdriver) =>
            {
                webdriver.FindElement(element).Click();
                return true;
            });
        }

        /// <summary>
        /// Helper method for repeatedly clicking an element until a certain condition is met
        /// </summary>
        /// <param name="element">The selector for the element you want to click</param>
        /// <param name="waitCondition">A lambda function that should return true if the success criteria are met, false otherwise</param>
        /// <param name="timeout">The amount of time to try before timing out. Optional, defaults to 10 seconds</param>
        public void ClickElementUntil(By element, Func<IWebDriver, bool> waitCondition, TimeSpan? timeout = null)
        {
            if (timeout == null) timeout = TimeSpan.FromSeconds(10.0d);

            var Wait = new WebDriverWait(WebDriver, (TimeSpan)timeout);
            Wait.PollingInterval = TimeSpan.FromSeconds(1.0d);
            Wait.IgnoreExceptionTypes(new Type[] { typeof(StaleElementReferenceException), typeof(NotFoundException), typeof(NoSuchElementException), typeof(InvalidElementStateException), typeof(ElementClickInterceptedException) });

            Wait.Until((webdriver) =>
            {
                webdriver.FindElement(element).Click();
                return waitCondition(webdriver);
            });
        }
    }
}