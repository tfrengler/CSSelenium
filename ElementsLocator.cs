using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace TFrengler.Selenium
{
    /// <summary>
    /// Helper class with methods for finding multiple elements, based on attribute and/or tag-name
    /// </summary>
    public sealed class ElementsLocator
    {
        private readonly ISearchContext Context;
        private string XPathAxis;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">The context to confine the search within. This can be an element, or the webdriver. By default it's the webdriver, meaning we search the entire document. Note that the XPath axis using methods are set to searching the entire document regardless!</param>
        public ElementsLocator(ISearchContext context)
        {
            Context = context;
            XPathAxis = "//";
        }

        /// <summary>
        /// Returns a new instance of ElementLocator, but confines the search to within a given element
        /// </summary>
        /// <param name="element">The element you want to search for elements within</param>
        public ElementsLocator Within(IWebElement context)
        {
            return new ElementsLocator(context) { XPathAxis = ".//" };
        }

        public ReadOnlyCollection<IWebElement> ByTagName(string elementType)
        {
            return Context.FindElements(By.TagName(elementType));
        }

        public ReadOnlyCollection<IWebElement> ByTitle(string title, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByTitle(title, elementType));
        }

        public ReadOnlyCollection<IWebElement> ById(string id, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ById(id, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByClass(string className, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByClass(className, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByName(string name, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByName(name, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByTextEquals(string text, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByTextEquals(text, elementType, XPathAxis));
        }

        public ReadOnlyCollection<IWebElement> ByTextContains(string text, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByTextContains(text, elementType, XPathAxis));
        }

        public ReadOnlyCollection<IWebElement> ByInputType(string type, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByInputType(type, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByValue(string value, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByValue(value, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByAttributeEquals(string attribute, string value, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByAttributeEquals(attribute, value, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByAttributeStartsWith(string attribute, string value, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByAttributeStartsWith(attribute, value, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByAttributeEndsWith(string attribute, string value, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByAttributeEndsWith(attribute, value, elementType));
        }

        public ReadOnlyCollection<IWebElement> ByAttributeContains(string attribute, string value, string elementType = null)
        {
            return Context.FindElements(LocatorFactory.ByAttributeContains(attribute, value, elementType));
        }
    }
}