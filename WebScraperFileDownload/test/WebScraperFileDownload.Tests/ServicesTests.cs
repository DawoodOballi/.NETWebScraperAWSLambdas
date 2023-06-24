using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Amazon.S3.Model;
using HtmlAgilityPack;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using QuickType;
using WebScraperFileDownload.Helpers;
using WebScraperFileDownload.Services;

namespace WebScraperFileDownload.Tests
{
    public class ServicesTests
    {
        private TestLambdaContext context;
        private WebScraperFileDownloadEvent _event;
        private string bucketName;
        private string csvUrl;
        private string fileName;
        private string key;

        [SetUp]
        public void Setup()
        {
            context = new();

            _event = new() {
                EventId = "File Download Event Id",
                Xpath = @"//div/button[contains(@class,""2.2inner"")]",
                Websiteurl = new Uri("https://wwww.example.com/path/to/file/page/"),
                Fullxpath = "/div/a",
                FilePrefixPattern = "yyyyMMdd_HHmmss:Z"
            };

            bucketName = "test-bucket";
            csvUrl = "https://wwww.example.com/path/to/file/page/1241?1234format=csv";
            fileName = "example_";
            key = "example_"+DateTime.Now.ToString(_event.FilePrefixPattern)+"_.csv";
        }

        [Test]
        public void AmazonService_GetBucketNameFromEnvironmentVariable_ReturnsBucketName_WhenEnvironmentVariableIsSet()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var _sut = new AmazonService(null, null, mockLogger.Object);
            Environment.SetEnvironmentVariable("test", bucketName);

            // Act
            var result = _sut.GetBucketNameFromEnvironmentVariable("test");


            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(bucketName, result);
            Environment.SetEnvironmentVariable("test", null);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Reading Environment Variables")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "All environment variables have been retreieved")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(2));
        }

        [Test]
        public void AmazonService_GetBucketNameFromEnvironmentVariable_ReturnsNull_WhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var _sut = new AmazonService(null, null, mockLogger.Object);

            // Act
            var result = _sut.GetBucketNameFromEnvironmentVariable("test");


            // Assert
            Assert.IsNull(result);
            Assert.AreNotEqual(bucketName, result);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Reading Environment Variables")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "All environment variables have been retreieved")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(2));
        }

        [Test]
        public async Task AmazonService_UploadToS3Bucket_ReturnsPutObjectResponse_WithHttpStatusCodeOK()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var encoding = new UTF8Encoding();
            var buffer = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 34, 86, 84};
            PutObjectResponse putObjectResponse = new()
            {
                HttpStatusCode = HttpStatusCode.OK
            };

            PutObjectRequest putObjectRequest = new()
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = encoding.GetString(buffer)
            };

            var mockAmazonS3 = new Mock<IAmazonS3>(MockBehavior.Strict);

            mockAmazonS3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default)).ReturnsAsync(putObjectResponse);

            var _sut = new AmazonService(mockAmazonS3.Object, putObjectRequest, mockLogger.Object);

            // Act
            var result = await _sut.UploadToS3Bucket(buffer, fileName, bucketName, _event.FilePrefixPattern);


            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(putObjectResponse.HttpStatusCode, result.HttpStatusCode);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Setting up the PutObjectRequest to pass for upload")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Finished setting up the PutObjectRequest to pass for upload")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Uploading file to {bucketName}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Successfully uploaded file {fileName+DateTime.Now.ToString(_event.FilePrefixPattern)+".csv"} to {bucketName}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(4));
        }

        [Test]
        public void AmazonService_UploadToS3Bucket_ThrowsAmazonS3Exception_WhenBucketDoesNotExist()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var encoding = new UTF8Encoding();
            var exceptionMessage = "The bucket you are attempting to access must be addressed using the specified endpoint. PLease send all future requests to this endpoint";
            var exception = new AmazonS3Exception(exceptionMessage);
            var buffer = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 34, 86, 84};

            PutObjectRequest putObjectRequest = new()
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = encoding.GetString(buffer)
            };

            var mockAmazonS3 = new Mock<IAmazonS3>(MockBehavior.Strict);

            mockAmazonS3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default)).Throws(exception);

            var _sut = new AmazonService(mockAmazonS3.Object, putObjectRequest, mockLogger.Object);

            // Act
            var resultAction = () => _sut.UploadToS3Bucket(buffer, fileName, bucketName, _event.FilePrefixPattern);


            // Assert
            Assert.That(resultAction, Throws.Exception.TypeOf<AmazonS3Exception>().With.Message.EqualTo(exceptionMessage));
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Setting up the PutObjectRequest to pass for upload")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Finished setting up the PutObjectRequest to pass for upload")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Uploading file to {bucketName}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == "The bucket you are trying to upload to does not exist. -> Exception Type: \n " 
                + exception.GetType() + "\nMessage: " + exceptionMessage)), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(3));
            mockLogger.Verify(
                x => x.LogError(It.IsAny<string>()), 
                Times.Exactly(1));
        }

                [Test]
        public void AmazonService_UploadToS3Bucket_ThrowsArgumentNullException_WhenBucketEnvVariableIsNotSet()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var encoding = new UTF8Encoding();
            var exceptionMessage = "BucketName is a required property and must be set before making this call. (Parameter 'PutObjectRequest.BucketName')";
            var exception = new ArgumentException(exceptionMessage);
            var buffer = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 34, 86, 84};

            PutObjectRequest putObjectRequest = new()
            {
                BucketName = null,
                Key = key,
                ContentBody = encoding.GetString(buffer)
            };

            var mockAmazonS3 = new Mock<IAmazonS3>(MockBehavior.Strict);

            mockAmazonS3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default)).Throws(exception);

            var _sut = new AmazonService(mockAmazonS3.Object, putObjectRequest, mockLogger.Object);

            // Act
            var resultAction = () => _sut.UploadToS3Bucket(buffer, fileName, bucketName, _event.FilePrefixPattern);


            // Assert
            Assert.That(resultAction, Throws.Exception.TypeOf<ArgumentException>().With.Message.EqualTo(exceptionMessage));
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Setting up the PutObjectRequest to pass for upload")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Finished setting up the PutObjectRequest to pass for upload")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Uploading file to {bucketName}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == "The bucket value cannot be null. -> Exception Type: \n " 
                + exception.GetType() + "\nMessage: " + exceptionMessage)), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(3));
            mockLogger.Verify(
                x => x.LogError(It.IsAny<string>()), 
                Times.Exactly(1));
        }

        [Test]
        public async Task HttpService_GetFileNameAndDataFromUrl_ReturnsFileNameAndDataBufferInTuple()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var mockhttpClientFactory = new Mock<IHttpClientFactory>();
            var buffer = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 191, 189, 34, 86, 84};
            var encodedBuffer = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            
            var response = new HttpResponseMessage() {
                StatusCode = HttpStatusCode.OK,
                ReasonPhrase = "OK",
                Content = new StringContent(encodedBuffer)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"){
                CharSet = "utf-8"
            };
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "\"example_.csv\""
            };


            mockhttpClientFactory.Setup(r => r.CreateClient("webscraperfiledownload")).Returns(new HttpClient(mockHttpMessageHandler.Object));

            mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri(csvUrl) && x.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            ).ReturnsAsync(response)
            .Verifiable();

            var _sut = new HttpService(mockhttpClientFactory.Object, mockLogger.Object);

            // Act
            var result = await _sut.GetFileNameAndDataFromUrl(csvUrl);


            // Assert
            Assert.That(result, Is.EqualTo(Tuple.Create(buffer, fileName)));
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Creating Http Client")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Sending a get request to {csvUrl}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Extracting file name from {response.Content.Headers.ContentDisposition.FileName}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Reading the response content into buffer")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Successfully retreived data and stored into buffer")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(5));
        }

        [Test]
        public void HttpService_GetFileNameAndDataFromUrl_ThrowsHttpRequestExceptionWhenUrlIsInvalid()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var mockhttpClientFactory = new Mock<IHttpClientFactory>();

            mockhttpClientFactory.Setup(r => r.CreateClient("webscraperfiledownload")).Returns(new HttpClient(mockHttpMessageHandler.Object));

            mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri(csvUrl) && x.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            ).ThrowsAsync(new HttpRequestException())
            .Verifiable();

            var _sut = new HttpService(mockhttpClientFactory.Object, mockLogger.Object);

            // Act
            var resultAction = () => _sut.GetFileNameAndDataFromUrl(csvUrl);


            // Assert
            Assert.That(resultAction, Throws.Exception.TypeOf<HttpRequestException>());
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Creating Http Client")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Sending a get request to {csvUrl}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(2));
        }

        [Test]
        public void HttpService_GetFileNameAndDataFromUrl_ThrowsInvalidOperationExceptionWhenUrlIsInvalid()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var mockhttpClientFactory = new Mock<IHttpClientFactory>();

            mockhttpClientFactory.Setup(r => r.CreateClient("webscraperfiledownload")).Returns(new HttpClient(mockHttpMessageHandler.Object));

            mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri(csvUrl) && x.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            ).ThrowsAsync(new InvalidOperationException())
            .Verifiable();

            var _sut = new HttpService(mockhttpClientFactory.Object, mockLogger.Object);

            // Act
            var resultAction = () => _sut.GetFileNameAndDataFromUrl(csvUrl);


            // Assert
            Assert.That(resultAction, Throws.Exception.TypeOf<InvalidOperationException>());
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Creating Http Client")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Sending a get request to {csvUrl}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(2));
        }

        [Test]
        public void WebService_GetCsvFileUrlFromXpathAndUri_ThrowsHttpRequestExceptionWithInnerSocketException_WhenUrlIsInvalid()
        {
            // Arrange
            var mockLogger = new Mock<ILambdaLogger>();
            var mockHtmlAgilityPackHelper = new Mock<IHtmlAgilityPackHelper>();
            var exceptionMessage = $"No such host is known. ({_event.Websiteurl.ToString()}:443)";
            
            
            mockHtmlAgilityPackHelper.Setup(r => r.LoadFromWebAsync(_event.Websiteurl.ToString()))
                .ThrowsAsync(new HttpRequestException(exceptionMessage, new SocketException())
            );

            var _sut = new WebService(mockHtmlAgilityPackHelper.Object, mockLogger.Object);

            // Act
            var resultAction = () => _sut.GetCsvFileUrlFromXpathAndUri(_event.Xpath, _event.Websiteurl);


            // Assert
            Assert.That(resultAction, 
                Throws.Exception.TypeOf<HttpRequestException>().With.Message.EqualTo(exceptionMessage)
                .And.InnerException.TypeOf<SocketException>());
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Loading html from website url: {_event.Websiteurl.ToString()}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(1));
        }

        [Test]
        public async Task WebService_GetCsvFileUrlFromXpathAndUri_ReturnsCsvFileUrl_WhenUrlIsValid()
        {
            // Arrange
            // https://stackoverflow.com/questions/5146061/c-sharp-multiline-string-with-html#:~:text=Use%20double%20quotes%20instead%20of%20escaping%20them.
            var html = @"<!DOCTYPE html>
                        <html>
                            <head>
                            <!-- head definitions go here -->
                            </head>
                            <body>
                                <div class=""1"">
                                    <div class=""2inner"">
                                        Lorem Ipsum is simply dummy text of the printing and typesetting industry.
                                        Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer 
                                        took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, 
                                        but also the leap into electronic typesetting, remaining essentially unchanged. 
                                        It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, 
                                        and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.2inner"" href=""/path/to/file/page/11111?11111format=csv"">dwdawdaw.csv</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.2inner"" href=""/path/to/file/page/22222?22222format=xlsx"">gesgegse.xlsx</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.2inner"" href=""/path/to/file/page/33333?33333format=csv"">dwdawdaw.csv</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.2inner"" href=""/path/to/file/page/44444?44444format=xlsx"">gesgegse.xlsx</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.3inner"" href=""/path/to/file/page/55555?55555format=csv"">dwdawdaw.csv</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.3inner"" href=""/path/to/file/page/66666?66666format=xlsx"">gesgegse.xlsx</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.3inner"" href=""/path/to/file/page/77777?77777format=csv"">dwdawdaw.csv</button>
                                    </div>
                                    <div class=""2.1inner"">
                                        <button class=""2.3inner"" href=""/path/to/file/page/88888?88888format=xlsx"">gesgegse.xlsx</button>
                                    </div>
                                </div>
                            </body>
                        </html>";
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var mockLogger = new Mock<ILambdaLogger>();
            var mockHtmlAgilityPackHelper = new Mock<IHtmlAgilityPackHelper>();
            var exceptionMessage = $"No such host is known. ({_event.Websiteurl.ToString()}:443)";
            
            
            mockHtmlAgilityPackHelper.Setup(r => r.LoadFromWebAsync(_event.Websiteurl.ToString())).ReturnsAsync(htmlDocument);

            var _sut = new WebService(mockHtmlAgilityPackHelper.Object, mockLogger.Object);

            // Act
            var result = await _sut.GetCsvFileUrlFromXpathAndUri(_event.Xpath, _event.Websiteurl);


            // Assert
            Assert.That(result, Is.EqualTo("https://wwww.example.com/path/to/file/page/11111?11111format=csv"));
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Loading html from website url: {_event.Websiteurl.ToString()}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Retreiving desired node based on Xpath: {_event.Xpath}")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Getting the href attribute")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"Successfully parsed href attribute and retreived the file enpoint url for download: /path/to/file/page/11111?11111format=csv")), 
                Times.Once);
            mockLogger.Verify(
                x => x.LogInformation(It.IsAny<string>()), 
                Times.Exactly(4));
        }
    }
}
