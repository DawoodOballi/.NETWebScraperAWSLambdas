using System;

namespace WebScraper.Services
{
    public interface IHttpService
    {
        Task<String> QueryWebsite(Uri url);
    }
}
