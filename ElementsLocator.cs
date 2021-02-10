using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace TFrengler.Selenium
{
    public sealed class ElementsLocator
    {
        private readonly ISearchContext Context;
        private string XPathAxis;

        public ElementsLocator(ISearchContext context)
        {
            Context = context;
            XPathAxis = "//";
        }

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