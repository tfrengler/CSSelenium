using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace TFrengler.Selenium
{
    public static class WebElementExtensions
    {
        public static ReadOnlyCollection<IWebElement> GetDirectChildren(this IWebElement element)
        {
            return element.FindElements(By.XPath(".//child::*"));
        }

        public static ReadOnlyCollection<IWebElement> GetDescendants(this IWebElement element)
        {
            return element.FindElements(By.XPath(".//descendant::*"));
        }

        public static IWebElement GetParent(this IWebElement element)
        {
            return element.FindElement(By.XPath(".//parent::*"));
        }

        public static IWebElement GetPreviousSiblingElement(this IWebElement element)
        {
            return element.FindElement(By.XPath(".//preceding-sibling::*"));
        }

        public static IWebElement GetNextSiblingElement(this IWebElement element)
        {
            return element.FindElement(By.XPath(".//following-sibling::*"));
        }
    }
}