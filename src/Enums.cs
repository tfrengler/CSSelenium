namespace TFrengler.CSSelenium
{
    /// <summary>List of supported browser drivers</summary>
    public enum Browser
    {
        EDGE        = 0,
        FIREFOX     = 1,
        CHROME      = 2
    }

    /// <summary>List of supported driver platforms</summary>
    public enum Platform
    {
        WINDOWS, LINUX
    }

    /// <summary>List of supported driver architectures</summary>
    public enum Architecture
    {
        x64, x86
    }
}