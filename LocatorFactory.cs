using OpenQA.Selenium;

namespace TFrengler.Selenium
{
    public static class LocatorFactory
    {
        private static By ByAttributeAndOperator(string attribute, string value, char searchOperator, string elementType = null)
        {
            return By.CssSelector($"{elementType ?? ""}[{attribute}{searchOperator}='{value}']");
        }

        public static By ByTitle(string title, string elementType = null)
        {
            return ByAttributeAndOperator("title", title, '=', elementType);
        }

        public static By ById(string id, string elementType = null)
        {
            return ByAttributeAndOperator("id", id, '=', elementType);
        }

        public static By ByClass(string className, string elementType = null)
        {
            return ByAttributeAndOperator("className", className, '=', elementType);
        }

        public static By ByName(string name, string elementType = null)
        {
            return ByAttributeAndOperator("name", name, '=', elementType);
        }

        public static By ByTextEquals(string text, string elementType = null, string axis = null)
        {
            return By.XPath($"{axis ?? ".//"}{elementType ?? "*"}[(normalize-space(text())=\"{text}\"]");
        }

        public static By ByTextContains(string text, string elementType = null, string axis = null)
        {
            return By.XPath($"{axis ?? ".//"}{elementType ?? "*"}[contains(normalize-space(.),\"{text}\")]");
        }

        public static By ByInputType(string type, string elementType = null)
        {
            return ByAttributeAndOperator("type", type, '=', elementType);
        }

        public static By ByValue(string value, string elementType = null)
        {
            return ByAttributeAndOperator("value", value, '=', elementType);
        }

        public static By ByAttributeStartsWith(string attribute, string value, string elementType = null)
        {
            return ByAttributeAndOperator("value", value, '^', elementType);
        }

        public static By ByAttributeEndsWith(string attribute, string value, string elementType = null)
        {
            return ByAttributeAndOperator("value", value, '$', elementType);
        }

        public static By ByAttributeContains(string attribute, string value, string elementType = null)
        {
            return ByAttributeAndOperator("value", value, '*', elementType);
        }

        public static By ByAttributeEquals(string attribute, string value, string elementType = null)
        {
            return ByAttributeAndOperator("value", value, '=', elementType);
        }
    }
}