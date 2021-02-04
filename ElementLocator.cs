using OpenQA.Selenium;

namespace TFrengler.Selenium
{
    public sealed class ElementLocator
    {
        private readonly ISearchContext Context;

        public ElementLocator(ISearchContext context)
        {
            Context = context;
        }

        public ElementLocator Within(IWebElement context)
        {
            return new ElementLocator(context);
        }

        public IWebElement ByTagName(string elementType)
        {
            return Context.FindElement(By.TagName(elementType));
        }

        public IWebElement ByTitle(string title, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByTitle(title, elementType));
        }

        public IWebElement ById(string id, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ById(id, elementType));
        }

        public IWebElement ByClass(string className, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByClass(className, elementType));
        }

        public IWebElement ByName(string name, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByName(name, elementType));
        }

        public IWebElement ByTextEquals(string text, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByTextEquals(text, elementType));
        }

        public IWebElement ByTextContains(string text, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByTextContains(text, elementType));
        }

        public IWebElement ByInputType(string type, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByInputType(type, elementType));
        }

        public IWebElement ByValue(string value, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByValue(value, elementType));
        }

        public IWebElement ByAttributeEquals(string attribute, string value, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByAttributeEquals(attribute, value, elementType));
        }

        public IWebElement ByAttributeStartsWith(string attribute, string value, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByAttributeStartsWith(attribute, value, elementType));
        }

        public IWebElement ByAttributeEndsWith(string attribute, string value, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByAttributeEndsWith(attribute, value, elementType));
        }

        public IWebElement ByAttributeContains(string attribute, string value, string elementType = null)
        {
            return Context.FindElement(LocatorFactory.ByAttributeContains(attribute, value, elementType));
        }
    }
}