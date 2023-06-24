using System;
using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;

namespace WebScraperFileDownload.Helpers
{
    public class LambdaLoggerHelper : ILambdaLogger
    {
        [ExcludeFromCodeCoverage]
        public void Log(string message)
        {
            
        }

        [ExcludeFromCodeCoverage]
        public void LogLine(string message)
        {
            
        }
    }
}
