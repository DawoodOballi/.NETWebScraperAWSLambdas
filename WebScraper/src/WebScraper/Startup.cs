extern alias awssdk;
extern alias awssdkcore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using awssdk::Amazon;
using awssdk::Amazon.SimpleEmail;
using awssdk::Amazon.SimpleEmail.Model;
using Microsoft.Extensions.DependencyInjection;
using WebScraper.Services;

namespace WebScraper
{
    internal static class Startup
    {
        public static IServiceProvider? Services { get; private set; } 

        public static void ConfigureServices()
        {

            AWSConfigs.AWSRegion = "eu-west-2";
            var services = new ServiceCollection();

            services.AddHttpClient<IHttpService, HttpService>("webscraper");
            services.AddSingleton<IAmazonSimpleEmailService, AmazonSimpleEmailServiceClient>(p => new AmazonSimpleEmailServiceClient(RegionEndpoint.GetBySystemName("eu-west-2")));
            services.AddScoped<SendEmailRequest>();
            services.AddScoped<VerifyEmailIdentityRequest>();
            services.AddScoped<GetIdentityVerificationAttributesRequest>();
            services.AddScoped<ListIdentitiesRequest>();
            services.AddScoped<IAmazonService, AmazonService>();
            services.AddScoped<IHttpService, HttpService>();

            Services = services.BuildServiceProvider();
        }    
    }
}