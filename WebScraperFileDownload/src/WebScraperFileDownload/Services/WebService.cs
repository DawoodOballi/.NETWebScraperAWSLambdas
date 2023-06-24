using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal.Util;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using WebScraperFileDownload.Helpers;

namespace WebScraperFileDownload.Services
{
    public class WebService : IWebService
    {
		private IHtmlAgilityPackHelper _htmlAgilityPackHelper;
        private ILambdaLogger _logger;

		public WebService(IHtmlAgilityPackHelper htmlAgilityPackHelper, ILambdaLogger logger)
		{
            ConfigureServices(htmlAgilityPackHelper, logger);
		}

        public async Task<String> GetCsvFileUrlFromXpathAndUri(String xpath, Uri uri)
        {
            _logger.LogInformation($"Loading html from website url: {uri.ToString()}");
            HtmlDocument doc = await _htmlAgilityPackHelper.LoadFromWebAsync(uri.ToString());

            _logger.LogInformation($"Retreiving desired node based on Xpath: {xpath}");
            HtmlNodeCollection val = doc.DocumentNode.SelectNodes(xpath);
            HtmlNode? inner = val.Where(x => x.InnerText.Contains("csv")).FirstOrDefault();

            _logger.LogInformation($"Getting the href attribute");
            String href = inner.Attributes["href"].Value;
            String url = uri.AbsoluteUri.Remove(uri.AbsoluteUri.IndexOf(uri.AbsolutePath));
            _logger.LogInformation($"Successfully parsed href attribute and retreived the file enpoint url for download: {href}");
            String result = url+href;
            return result; 
        }

        private void ConfigureServices(IHtmlAgilityPackHelper htmlAgilityPackHelper = null, ILambdaLogger logger = null)
        {
            Startup.ConfigureServices();
            _htmlAgilityPackHelper = htmlAgilityPackHelper ?? Startup.Services.GetRequiredService<IHtmlAgilityPackHelper>();
            _logger = logger ?? Startup.Services.GetRequiredService<ILambdaLogger>();
        }
    }
}