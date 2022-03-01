
#r "C:/Dev/Projects/CSSelenium/src/bin/Debug/net5.0/CSSelenium.dll"
#r "C:\Users\thoma\.nuget\packages\selenium.webdriver\4.1.0\lib\net5.0\WebDriver.dll"
#r "C:\Users\thoma\.nuget\packages\mono.posix.netstandard\1.0.0\ref\netstandard2.0\Mono.Posix.NETStandard.dll"

using System.IO;
using System.Reflection;
using System.Linq;
using System;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net;
using System.Xml.Linq;
using TFrengler.CSSelenium;

HttpClient HttpClient;

try {

    HttpClient = new HttpClient();

    var Request = new HttpRequestMessage()
    {
        RequestUri = new Uri("https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/LATEST_STABLE"),
        Method = HttpMethod.Get
    };
    var Response = HttpClient.Send(Request);

    var Stream = Response.Content.ReadAsStream();
    var Buffer = new MemoryStream();
    using (var StreamReader = new StreamReader(Stream))
    {
        Stream.CopyTo(Buffer);
    }

    Console.WriteLine( Encoding.Unicode.GetString(Buffer.ToArray()) );
}
catch(Exception)
{
    throw;
}
finally
{
    Console.WriteLine("All done");
    HttpClient?.Dispose();
}