using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using WebScraperFileDownload.Services;
using WebScraperFileDownload.Helpers;
using Amazon.Lambda.Core;

namespace WebScraperFileDownload
{
    internal static class Startup
    {
        public static IServiceProvider Services { get; private set; } 

        public static void ConfigureServices()
        {

            AWSConfigs.AWSRegion = "eu-west-2";
            var services = new ServiceCollection();

            services.AddHttpClient<IHttpService, HttpService>("webscraperfiledownload");
            services.AddScoped<IAmazonS3, AmazonS3Client>(p => new AmazonS3Client(RegionEndpoint.GetBySystemName("eu-west-2")));
            services.AddScoped<IAmazonService, AmazonService>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IWebService, WebService>();
            services.AddScoped<ILambdaLogger, LambdaLoggerHelper>();
            services.AddScoped<IHtmlAgilityPackHelper, HtmlAgilityPackHelper>();
            services.AddScoped<HtmlWeb>();
            services.AddScoped<PutObjectRequest>();

            Services = services.BuildServiceProvider();
        }    
    }
}