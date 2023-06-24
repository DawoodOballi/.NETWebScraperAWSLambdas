using System;
using HtmlAgilityPack;

namespace WebScraperFileDownload.Helpers
{
    public interface IHtmlAgilityPackHelper
    {
        Task<HtmlDocument> LoadFromWebAsync(String url);
    }
}
