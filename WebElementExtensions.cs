using OpenQA.Selenium;
using System.Collections.ObjectModel;
using System.Linq;

namespace TFrengler.Selenium
{
    public static class WebElementExtensions
    {
        public static ReadOnlyCollection<IWebElement> GetDirectChildren(this IWebElement element, string elementType = null)
        {
            return element.FindElements(By.XPath($"./child::{elementType ?? "*"}"));
        }

        public static ReadOnlyCollection<IWebElement> GetDescendants(this IWebElement element, string elementType = null)
        {
            return element.FindElements(By.XPath($"./descendant::{elementType ?? "*"}"));
        }

        public static IWebElement GetParent(this IWebElement element)
        {
            return element.FindElement(By.XPath("./parent::*"));
        }

        public static IWebElement GetPreviousSiblingElement(this IWebElement element, string elementType = null)
        {
            ReadOnlyCollection<IWebElement> Siblings = element.FindElements(By.XPath($"./preceding-sibling::{elementType ?? "*"}"));

            if (Siblings.Count == 0)
                throw new NotFoundException("Cannot get previous sibling as this element does not appear to have any");

            return Siblings.Last();
        }

        public static IWebElement GetNextSiblingElement(this IWebElement element, string elementType = null)
        {
            ReadOnlyCollection<IWebElement> Siblings = element.FindElements(By.XPath($"./following-sibling::{elementType ?? "*"}"));

            if (Siblings.Count == 0)
                throw new NotFoundException("Cannot get previous sibling as this element does not appear to have any");

            return Siblings[0];
        }
    }
}