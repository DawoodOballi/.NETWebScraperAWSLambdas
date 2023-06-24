using System.IO.Enumeration;
using System.Net.Http;
using Amazon.S3;
using Amazon.Lambda.Core;
using HtmlAgilityPack;
using Newtonsoft.Json;
using QuickType;
using Amazon.S3.Model;
using WebScraperFileDownload.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Diagnostics.CodeAnalysis;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WebScraperFileDownload;

public class Function
{
    private IAmazonService _amazonService;
    private IHttpService _httpService;
    private IWebService _webService;

    public Function() => ConfigureServices();

    public Function(IAmazonService amazonService, IHttpService httpService, IWebService webService)
    {
        ConfigureServices(amazonService, httpService, webService);
    }

    [ExcludeFromCodeCoverage]
    private void ConfigureServices(IAmazonService amazonService = null, 
        IHttpService httpService = null,
        IWebService webService = null)
    {
        Startup.ConfigureServices();
        _amazonService = amazonService ?? Startup.Services.GetRequiredService<IAmazonService>();
        _httpService = httpService ?? Startup.Services.GetRequiredService<IHttpService>();
        _webService = webService ?? Startup.Services.GetRequiredService<IWebService>();
    }

    public async Task<HttpStatusCode> FunctionHandler(WebScraperFileDownloadEvent _event, ILambdaContext context)
    {
        PutObjectResponse response = null;
        try
        {
            String? BUCKET_NAME = _amazonService.GetBucketNameFromEnvironmentVariable("BUCKET_NAME");
            context.Logger.LogInformation("Event: \n" + JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event)));
            String url = await _webService.GetCsvFileUrlFromXpathAndUri(_event.Xpath, _event.Websiteurl);


            // Option 1 to get file name and data //
                /*    
                    var stream_file = await _httpService.GetStreamAndFileNameFromUrl(url);
                    Byte[] buffer;
                    using (Stream stream = stream_file.Item1)
                    {   
                        buffer = await _httpService.StreamToBuffer(stream);
                    }
                */
            //------------------------------------//


            // Option 2 to get file name and data //
            var resultTuple = await _httpService.GetFileNameAndDataFromUrl(url);
            //------------------------------------//
            
            // Using Option 1 //
                /* 
                    response = await _amazonService.UploadToS3Bucket(buffer, stream_file.Item2, BUCKET_NAME, _event.FilePrefixPattern); 
                */
            // -------------- //

            // Using option 2
            response = await _amazonService.UploadToS3Bucket(resultTuple.Item1, resultTuple.Item2, BUCKET_NAME, _event.FilePrefixPattern);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.Message);
            throw;
        }
        return response.HttpStatusCode;
    }
}
