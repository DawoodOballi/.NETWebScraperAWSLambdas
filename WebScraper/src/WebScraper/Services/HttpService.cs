using System;
using Microsoft.Extensions.DependencyInjection;

namespace WebScraper.Services
{
    public class HttpService : IHttpService
    {
        private IHttpClientFactory _httpClientFactory;
        public HttpService(IHttpClientFactory httpClientFactory)
        {
           ConfigureServices(httpClientFactory); 
        }    
        public async Task<String> QueryWebsite(Uri url)
        {
            var client = _httpClientFactory.CreateClient("webscraper");
            var response = await client.GetStringAsync(url);
            return response;
        }
        
        private void ConfigureServices(IHttpClientFactory httpClientFactory = null)
        {
            Startup.ConfigureServices();
            _httpClientFactory = httpClientFactory ?? Startup.Services.GetRequiredService<IHttpClientFactory>();
        }
    }
}
