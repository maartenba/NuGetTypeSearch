using System.Net.Http;

namespace NuGetTypeSearch
{
    public static class SharedHttpClient 
    {
        public static readonly HttpClient Instance = new HttpClient(new HttpClientHandler 
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
    }
}