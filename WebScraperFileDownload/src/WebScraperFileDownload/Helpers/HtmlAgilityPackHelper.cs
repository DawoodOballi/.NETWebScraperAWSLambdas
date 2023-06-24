using System;
using System.Diagnostics.CodeAnalysis;
using HtmlAgilityPack;

namespace WebScraperFileDownload.Helpers
{
    public class HtmlAgilityPackHelper : IHtmlAgilityPackHelper
    {
        private readonly HtmlWeb htmlWeb;

        public HtmlAgilityPackHelper(HtmlWeb htmlWeb)
        {
            this.htmlWeb = htmlWeb;
        }


        /// <summary>
        /// Excluded from coverage because it was only made to allow for mock testing 
        /// given the LoadFromWebAsync function is non-overridable in the HtmlWeb class
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        public async Task<HtmlDocument> LoadFromWebAsync(string url)
        {
            return await htmlWeb.LoadFromWebAsync(url);
        }
    }
}