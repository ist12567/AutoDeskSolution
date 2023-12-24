using ConsoleApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

var accessToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY0RE9XMnJoOE9tbjNpdk1NU0xlNGQ2VHEwUSIsInBpLmF0bSI6ImFzc2MifQ.eyJzY29wZSI6WyJkYXRhOnJlYWQiXSwiY2xpZW50X2lkIjoiRlJvYkFMczUzSzQzZkxQN2FkY1lYc0VFT0xHcWxhVzAiLCJpc3MiOiJodHRwczovL2RldmVsb3Blci5hcGkuYXV0b2Rlc2suY29tIiwiYXVkIjoiaHR0cHM6Ly9hdXRvZGVzay5jb20iLCJqdGkiOiJOaW4xTVlaMkJCN2tPc0RxdnVPTnFjSkFyTVVMVG5tN1pZbll0dHhDaUdIY0ljU2ViSEhERmR1RDZPY242NE5nIiwiZXhwIjoxNjk5OTg0NDY5fQ.h4Ad1QE6b8iQSPi0CnESN6gM8-zvjeJxEBk5lT4UTP7uHBdIeRdh48RDvFJwgkJ2j2mJUxw-O6PxPf-dgnZaJzfKfX3cSphZPKYRVCN7Xrg4xeupmPkoHlbc9teK_ZHhNwgE_Bt-JsGlu2qGMim3DxRWjC3j9oQrUSAkX2lESozCjjrDckHZSgcHjzOSPHLiP9n-GqAhjmLRT34gY8dW_drj_9NF8VTEcaWBhC6NnOS1WdOuZRuZFUy45C8BYaEE36C0Qgzwy2lX0ETdFHAtJqeEoYtWjPAmfNG7OK1_YgFofMuP5FBtR-hLxI7_BwCZiHVKJWYGVbQr_9nofZlGPA";
var urn = "dXJuOmFkc2sud2lwcHJvZDpmcy5maWxlOnZmLk9tRkVKNXFEU2F5Mi1sMWNXT20zM0E_dmVyc2lvbj0x";
var derivativeUrn = "urn:adsk.viewing:fs.file:dXJuOmFkc2sud2lwcHJvZDpmcy5maWxlOnZmLk9tRkVKNXFEU2F5Mi1sMWNXT20zM0E_dmVyc2lvbj0x/output/Resource/3D View/{3D} 960621/{3D}1.png";
List<Derivatives.Resource> resources = await Derivatives.ExtractSVFAsync(urn, accessToken);
string downloadsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
foreach (Derivatives.Resource resource in resources)
{
    using (WebClient client = new())
    {
        // Combine the downloads folder with the file name
        string localDownloadPath = Path.Combine(downloadsFolder, resource.FileName);

        // Download the file
        client.DownloadFile(resource.RemotePath, localDownloadPath);
    }
}




var restSharpClient = new RestClient("https://developer.api.autodesk.com");
RestRequest request = new RestRequest("/modelderivative/v2/designdata/dXJuOmFkc2sud2lwcHJvZDpmcy5maWxlOnZmLk9tRkVKNXFEU2F5Mi1sMWNXT20zM0E_dmVyc2lvbj0x/manifest?scopes=b360project.c2e8e467-e9cf-4086-a869-47d4376d63a5,O2tenant.43968303", Method.Get);
request.AddHeader("Authorization", "Bearer " + accessToken);
var response = await restSharpClient.ExecuteAsync(request);
var result = JsonConvert.DeserializeObject<dynamic>(response.Content!);


List<string> urns = new List<string>();
JObject jsonObject = JObject.Parse(result.ToString());
FindUrnOfMimeType(jsonObject, "application/autodesk-svf", urns);



restSharpClient = new RestClient("https://developer.api.autodesk.com");
request = new RestRequest("/modelderivative/v2/designdata/{urn}/manifest/{derivativeUrn}/signedcookies", Method.Get);
request.AddHeader("Authorization", "Bearer " + accessToken);
request.AddUrlSegment("urn", urn);
request.AddUrlSegment("derivativeUrn", derivativeUrn);
response = await restSharpClient.ExecuteAsync(request);

var cloudFrontPolicyName = "CloudFront-Policy";
var cloudFrontKeyPairIdName = "CloudFront-Key-Pair-Id";
var cloudFrontSignatureName = "CloudFront-Signature";

var cloudFrontCookies = response.Headers
                        .Where(x => x.Name == "Set-Cookie")
                        .Select(x => x.Value)
                        .Cast<string>()
                        .ToList();

var cloudFrontPolicy = cloudFrontCookies.Where(value => value.Contains(cloudFrontPolicyName)).FirstOrDefault()?.Trim().Substring(cloudFrontPolicyName.Length + 1).Split(";").FirstOrDefault();
var cloudFrontKeyPairId = cloudFrontCookies.Where(value => value.Contains(cloudFrontKeyPairIdName)).FirstOrDefault()?.Trim().Substring(cloudFrontKeyPairIdName.Length + 1).Split(";").FirstOrDefault();
var cloudFrontSignature = cloudFrontCookies.Where(value => value.Contains(cloudFrontSignatureName)).FirstOrDefault()?.Trim().Substring(cloudFrontSignatureName.Length + 1).Split(";").FirstOrDefault();

result = JsonConvert.DeserializeObject<dynamic>(response.Content!);
var downloadURL = $"{result.url}?Key-Pair-Id={cloudFrontKeyPairId}&Signature={cloudFrontSignature}&Policy={cloudFrontPolicy}";

System.Diagnostics.Trace.WriteLine(downloadURL);

RestRequest requestDownload = new RestRequest(downloadURL, RestSharp.Method.Get);
System.IO.Stream downloadStream = await restSharpClient.DownloadStreamAsync(requestDownload);
string name = derivativeUrn.Substring(derivativeUrn.LastIndexOf('/') + 1);
using (var memoryStream = new MemoryStream())
{
    downloadStream.CopyTo(memoryStream);
    File.WriteAllBytes(name, memoryStream.ToArray());
}


void FindUrnOfMimeType(JToken token, string mimeType, List<string> urns)
{
    if (token is JProperty)
    {
        if ((token as JProperty).Name == "mime" && token.ToString().Contains(mimeType))
        {
            urns.Add(token.Parent["urn"].ToString());
        }
    }

    foreach (JToken child in token.Children())
    {
        FindUrnOfMimeType(child, mimeType, urns);
    }
}