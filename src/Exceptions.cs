using System;

namespace TFrengler.CSSelenium
{
    public class UnsupportedOSException : Exception
    {
        public UnsupportedOSException()
        {
        }

        public UnsupportedOSException(string message)
            : base(message)
        {
        }

        public UnsupportedOSException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class WebdriverAlreadyRunningException : Exception
    {
        public WebdriverAlreadyRunningException()
        {
        }

        public WebdriverAlreadyRunningException(string message)
            : base(message)
        {
        }

        public WebdriverAlreadyRunningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class UnsupportedWebdriverConfigurationException : Exception
    {
        public UnsupportedWebdriverConfigurationException()
        {
        }

        public UnsupportedWebdriverConfigurationException(string message)
            : base(message)
        {
        }

        public UnsupportedWebdriverConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class UnavailableVersionException : Exception
    {
        public UnavailableVersionException(Browser browser, uint version)
        : base($"No such version available for the {Enum.GetName(typeof(Browser), browser)}-driver: {version}")
        {
        }
    }
}