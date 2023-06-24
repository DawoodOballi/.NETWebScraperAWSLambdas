using System;

namespace WebScraperFileDownload.Services
{
    public interface IHttpService
    {
        // Option 1 to get file name and file data //
        Task<Tuple<Stream, String>> GetStreamAndFileNameFromUrl(String url);
        Task<Byte[]> StreamToBuffer(Stream stream);
        // --------------------------------------- //

        // Option 2 to get file name and file data //
        Task<Tuple<Byte[], String>> GetFileNameAndDataFromUrl(String url);
        // --------------------------------------- //
    }
}
