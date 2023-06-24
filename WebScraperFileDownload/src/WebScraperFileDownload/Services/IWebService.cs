using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebScraperFileDownload.Services
{
    public interface IWebService
    {
        Task<String> GetCsvFileUrlFromXpathAndUri(String xpath, Uri uri);
    }
}