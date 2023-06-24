using System;
using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;

namespace WebScraperFileDownload.Services
{
    public class HttpService : IHttpService
    {
        private IHttpClientFactory _httpClientFactory;
        private ILambdaLogger _logger;

        public HttpService(IHttpClientFactory httpClientFactory, ILambdaLogger logger)
        {
           ConfigureServices(httpClientFactory, logger); 
        }
        

        [ExcludeFromCodeCoverage]
        // Option 1 to get file name and file data //
        public async Task<Tuple<Stream, String>> GetStreamAndFileNameFromUrl(String url)
        {
            HttpClient client = _httpClientFactory.CreateClient("webscraperfiledownload");
            HttpResponseMessage res = await client.GetAsync(url);
            String downloadedFileName = res.Content.Headers.ContentDisposition.FileName.Replace("\"", "").Replace(".csv", "");
            Stream result = await res.Content.ReadAsStreamAsync();
            return Tuple.Create(result, downloadedFileName);
        }

        [ExcludeFromCodeCoverage]
        // This function is from https://stackoverflow.com/questions/221925/creating-a-byte-array-from-a-stream.
        public async Task<Byte[]> StreamToBuffer(Stream stream)
        {
            Byte[] buffer = new Byte[16*1024];
            using (MemoryStream ms = new())
            {
                Int32 read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        // ------------------------------------------ //


        // Option 2 to get file name and file data //
        public async Task<Tuple<Byte[], String>> GetFileNameAndDataFromUrl(String url)
        {
            _logger.LogInformation("Creating Http Client");
            HttpClient client = _httpClientFactory.CreateClient("webscraperfiledownload");

            _logger.LogInformation($"Sending a get request to {url}");
            HttpResponseMessage res = await client.GetAsync(url);

            _logger.LogInformation($"Extracting file name from {res.Content.Headers.ContentDisposition.FileName}");
            String downloadedFileName = res.Content.Headers.ContentDisposition.FileName.Replace("\"", "").Replace(".csv", "");

            _logger.LogInformation($"Reading the response content into buffer");
            Byte[] result = await res.Content.ReadAsByteArrayAsync();
            _logger.LogInformation($"Successfully retreived data and stored into buffer");

            return Tuple.Create(result, downloadedFileName);
        }
        // --------------------------------------- //

        [ExcludeFromCodeCoverage]
        private void ConfigureServices(IHttpClientFactory httpClientFactory = null, ILambdaLogger logger = null)
        {
            Startup.ConfigureServices();
            _httpClientFactory = httpClientFactory ?? Startup.Services.GetRequiredService<IHttpClientFactory>();
            _logger = logger ?? Startup.Services.GetRequiredService<ILambdaLogger>();
        }
    }
}
