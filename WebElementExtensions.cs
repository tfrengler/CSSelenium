using OpenQA.Selenium;
using System.Collections.ObjectModel;
using System.Linq;

namespace TFrengler.Selenium.Extensions
{
    /// <summary>
    /// Extensions to IWebElement to help finding elements that are related to the current element
    /// </summary>
    public static class WebElementExtensions
    {
        /// <summary>
        /// Returns all direct child elements of the current element
        /// </summary>
        /// <param name="elementType">Optional, the tagname of the elements you want to return</param>
        public static ReadOnlyCollection<IWebElement> GetDirectChildren(this IWebElement element, string elementType = null)
        {
            return element.FindElements(By.XPath($"./child::{elementType ?? "*"}"));
        }

        /// <summary>
        /// Returns all child elements (direct and nested) of the current element
        /// </summary>
        /// <param name="elementType">Optional, the tagname of the elements you want to return</param>
        public static ReadOnlyCollection<IWebElement> GetDescendants(this IWebElement element, string elementType = null)
        {
            return element.FindElements(By.XPath($"./descendant::{elementType ?? "*"}"));
        }

        /// <summary>
        /// Returns the parent element of the current element
        /// </summary>
        public static IWebElement GetParent(this IWebElement element)
        {
            return element.FindElement(By.XPath("./parent::*"));
        }

        /// <summary>
        /// Returns the previous adjacent element of the current element. If you pass 'elementType' it might not be the immediate neighbour. Instead it returns the first one it encounters.
        /// </summary>
        /// <param name="elementType">Optional, the tagname of the element you want to return</param>
        public static IWebElement GetPreviousSiblingElement(this IWebElement element, string elementType = null)
        {
            ReadOnlyCollection<IWebElement> Siblings = element.FindElements(By.XPath($"./preceding-sibling::{elementType ?? "*"}"));

            if (Siblings.Count == 0)
                throw new NotFoundException("Cannot get previous sibling as this element does not appear to have any");

            return Siblings.Last();
        }

        /// <summary>
        /// Returns the next adjacent element of the current element. If you pass 'elementType' it might not be the immediate neighbour. Instead it returns the first one it encounters.
        /// </summary>
        /// <param name="elementType">Optional, the tagname of the element you want to return</param>
        public static IWebElement GetNextSiblingElement(this IWebElement element, string elementType = null)
        {
            ReadOnlyCollection<IWebElement> Siblings = element.FindElements(By.XPath($"./following-sibling::{elementType ?? "*"}"));

            if (Siblings.Count == 0)
                throw new NotFoundException("Cannot get previous sibling as this element does not appear to have any");

            return Siblings[0];
        }
    }
}