using OpenQA.Selenium;

namespace TFrengler.Selenium
{
    public sealed class ElementLocator
    {
        private readonly ISearchContext Context;

        public ElementLocator(ISearchContext webdriver)
        {
            Context = webdriver;
        }

        public ElementLocator Within(IWebElement context)
        {
            return new ElementLocator(context);
        }

        private IWebElement GetByAttributeAndOperator(string attribute, string value, char searchOperator, string elementType = null)
        {
            return Context.FindElement(By.CssSelector($"{elementType ?? ""}[{attribute}{searchOperator}='{value}']"));
        }

        public IWebElement ByTitle(string title, string elementType = null)
        {
            return GetByAttributeAndOperator("title", title, '=', elementType);
        }
        public IWebElement ById(string id, string elementType = null)
        {
            return GetByAttributeAndOperator("id", id, '=', elementType);
        }
        public IWebElement ByClass(string className, string elementType = null)
        {
            return GetByAttributeAndOperator("className", className, '=', elementType);
        }
        public IWebElement ByName(string name, string elementType = null)
        {
            return GetByAttributeAndOperator("name", name, '=', elementType);
        }
        public IWebElement ByTextEquals(string text, string elementType = null)
        {
            return Context.FindElement(By.XPath($"//{elementType ?? "*"}[(normalize-space(text())=\"{text}\"]"));
        }
        public IWebElement ByTextContains(string text, string elementType = null)
        {
            return Context.FindElement(By.XPath($"//{elementType ?? "*"}[contains(normalize-space(.),\"{text}\")]"));
        }
        public IWebElement ByInputType(string type, string elementType = null)
        {
            return GetByAttributeAndOperator("type", type, '=', elementType);
        }
        public IWebElement ByValue(string value, string elementType = null)
        {
            return GetByAttributeAndOperator("value", value, '=', elementType);
        }
        public IWebElement ByAttributeStartsWith(string attribute, string value, string elementType = null)
        {
            return GetByAttributeAndOperator("value", value, '^', elementType);
        }
        public IWebElement ByAttributeEndsWith(string attribute, string value, string elementType = null)
        {
            return GetByAttributeAndOperator("value", value, '$', elementType);
        }
        public IWebElement ByAttributeContains(string attribute, string value, string elementType = null)
        {
            return GetByAttributeAndOperator("value", value, '*', elementType);
        }
    }
}