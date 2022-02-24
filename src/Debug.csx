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

// string Cookie = File.ReadAllText("C:/Temp/cookie.txt");

var Client = new HttpClient(
    new HttpClientHandler()
    {
        AllowAutoRedirect = true,
        // ClientCertificateOptions = ClientCertificateOption.Manual,
        // ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
        // {
        //     return true;
        // }
    }
);

var Request = new HttpRequestMessage()
{
    Method = HttpMethod.Get,
    RequestUri = new Uri("https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver?comp=list&prefix=98") // new Uri("https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/LATEST_STABLE")
};

Request.Headers.Add("User-Agent", "CSSelenium DriverManager");
// Request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:97.0) Gecko/20100101 Firefox/97.0");
// Request.Headers.Add("Host", "dev.azure.com");
// Request.Headers.Add("Cookie", Cookie);

HttpResponseMessage Response;
StreamReader Reader;
string Contents;

try {
    // Response = await Client.SendAsync(Request);
    // WriteLine("RESPONSE STATUS: " + Enum.GetName(typeof(HttpStatusCode), Response.StatusCode));
    // WriteLine("RESPONSE Content Type: " + Response.Content.Headers.ContentType);
    // WriteLine("RESPONSE Content Length: " + Response.Content.Headers.ContentLength);

    // Reader = new StreamReader(Response.Content.ReadAsStream());
    // Contents = await Reader.ReadToEndAsync();

    var XML = XDocument.Parse("gargle");
    var Elements = XML.Descendants("Name");

    // string DesiredMajorRevision = Convert.ToString(100);
    // var ParsedVersions = Elements.Select(element=> element.Value.Split('/', StringSplitOptions.None)[0]);
    // IEnumerable<int> NormalizedVersions = ParsedVersions.Select(tag=> tag.Split('.', StringSplitOptions.None).Select(part => Convert.ToInt32(part.Trim())).Sum());
    // int LatestIndex = Array.IndexOf(NormalizedVersions.ToArray(), NormalizedVersions.Max());
    // string LatestVersion = ParsedVersions.ElementAt(LatestIndex);

    // foreach(var Current in Elements)
    // {
    //     Console.WriteLine(Current);
    // }

    // Console.WriteLine("--------------");
    // Console.WriteLine(LatestVersion);

    // JsonElement Element = JsonSerializer.Deserialize<JsonElement>(Contents);
    // string Output = JsonSerializer.Serialize(Element, new JsonSerializerOptions()
    // {
    //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //     WriteIndented = true
    // });

    // WriteLine(Contents);
}
catch(Exception error)
{
    throw new Exception("Bad :(", error);
}
finally
{
    Response?.Dispose();
    Reader?.Dispose();
}