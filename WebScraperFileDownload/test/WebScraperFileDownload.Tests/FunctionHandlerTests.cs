
using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.S3.Model;
using Moq;
using NUnit.Framework;
using WebScraperFileDownload.Services;
using QuickType;
using System.Net.Sockets;
using Newtonsoft.Json;
using Amazon.S3;

namespace WebScraperFileDownload.Tests;

public class FunctionHandlerTests
{
    private Function _sutFunction;
    private TestLambdaContext context;
    private WebScraperFileDownloadEvent _event;
    private string bucketName;
    private string csvUrl;
    private string fileName;

    [SetUp]
    public void Setup()
    {
        _sutFunction = new();
        context = new();

        _event = new() {
            EventId = "File Download Event Id",
            Xpath = "/div",
            Websiteurl = new Uri("https://wwww.example.com/path/to/file/page/"),
            Fullxpath = "/div/a",
            FilePrefixPattern = "yyyyMMdd_HHmmss:Z"
        };

        bucketName = "test-bucket";
        csvUrl = "https://wwww.example.com/path/to/file/page/1241?1234format=csv";
        fileName = "example_";
    }

    
    [Test]
    public async Task FunctionHandler_ReturnsHttpStatusCodeOK()
    {
        // Arrange
        var expected = HttpStatusCode.OK;
        var data = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 34, 86, 84};
        var mockAmazonService = new Mock<IAmazonService>(MockBehavior.Strict);
        var mockWebService = new Mock<IWebService>(MockBehavior.Strict);
        var mockHttpService = new Mock<IHttpService>(MockBehavior.Strict);

        mockAmazonService.Setup(s => s.GetBucketNameFromEnvironmentVariable(It.IsAny<String>())).Returns(bucketName);
        mockWebService.Setup(s => s.GetCsvFileUrlFromXpathAndUri(It.IsAny<String>(), It.IsAny<Uri>())).ReturnsAsync(csvUrl);
        mockHttpService.Setup(s => s.GetFileNameAndDataFromUrl(It.IsAny<String>())).ReturnsAsync(Tuple.Create(data, fileName));
        mockAmazonService.Setup(s => 
            s.UploadToS3Bucket(data, fileName, bucketName, _event.FilePrefixPattern))
            .ReturnsAsync(new PutObjectResponse() { HttpStatusCode = HttpStatusCode.OK });

        _sutFunction = new Function(mockAmazonService.Object, mockHttpService.Object, mockWebService.Object);
        
        // Act
        var result = await _sutFunction.FunctionHandler(_event, context);


        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FunctionHandler_CatchesHttpRequestExceptionWithInnerSocketException_WhenUrlIsInvalid_ThrownByGetCsvFileUrlFromXpathAndUri()
    {
        // Arrange
        var mockAmazonService = new Mock<IAmazonService>(MockBehavior.Strict);
        var mockWebService = new Mock<IWebService>(MockBehavior.Strict);
        var mockHttpService = new Mock<IHttpService>(MockBehavior.Strict);
        var mockLambdaLogger = new Mock<ILambdaLogger>();
        var exceptionMessage = $"No such host is known. ({_event.Websiteurl.ToString()}:443)";

        mockAmazonService.Setup(s => s.GetBucketNameFromEnvironmentVariable(It.IsAny<String>())).Returns(bucketName);
        mockWebService.Setup(s => s.GetCsvFileUrlFromXpathAndUri(It.IsAny<String>(), It.IsAny<Uri>()))
            .ThrowsAsync(new HttpRequestException(exceptionMessage, new SocketException()));

        context.Logger = mockLambdaLogger.Object;

        _sutFunction = new Function(mockAmazonService.Object, mockHttpService.Object, mockWebService.Object);


        // Act / Assert
        var exc = Assert.CatchAsync<HttpRequestException>(() => _sutFunction.FunctionHandler(_event, context));
        Assert.That(exc, 
            Is.TypeOf<HttpRequestException>().With.Message
            .EqualTo(exceptionMessage).With.InnerException.TypeOf<SocketException>());
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event)))), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exceptionMessage)), 
            Times.Once);
    }

    [Test]
    public void FunctionHandler_CatchesHttpRequestException_WhenUrlIsInvalid_ThrownByGetFileNameAndDataFromUrl()
    {
        // Arrange
        var mockAmazonService = new Mock<IAmazonService>();
        var mockWebService = new Mock<IWebService>();
        var mockHttpService = new Mock<IHttpService>();
        var mockLambdaLogger = new Mock<ILambdaLogger>();
        var exceptionMessage = $"No such host is known. ({_event.Websiteurl.ToString()}:443)";

        mockAmazonService.Setup(s => s.GetBucketNameFromEnvironmentVariable(It.IsAny<String>())).Returns(bucketName);
        mockWebService.Setup(s => s.GetCsvFileUrlFromXpathAndUri(It.IsAny<String>(), It.IsAny<Uri>())).ReturnsAsync(csvUrl);
        mockHttpService.Setup(s => s.GetFileNameAndDataFromUrl(It.IsAny<String>())).ThrowsAsync(new HttpRequestException(exceptionMessage));

        context.Logger = mockLambdaLogger.Object;

        _sutFunction = new Function(mockAmazonService.Object, mockHttpService.Object, mockWebService.Object);


        // Act / Assert
        var exc = Assert.CatchAsync<HttpRequestException>(() => _sutFunction.FunctionHandler(_event, context));
        Assert.That(exc, 
            Is.TypeOf<HttpRequestException>().With.Message.EqualTo(exceptionMessage));
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event)))), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exceptionMessage)), 
            Times.Once);
    }


    [Test]
    public void FunctionHandler_CatchesInvalidOperationException_WhenUrlIsInvalid_ThrownByGetFileNameAndDataFromUrl()
    {
        // Arrange
        var mockAmazonService = new Mock<IAmazonService>();
        var mockWebService = new Mock<IWebService>();
        var mockHttpService = new Mock<IHttpService>();
        var mockLambdaLogger = new Mock<ILambdaLogger>();
        var exception = new InvalidOperationException();

        mockAmazonService.Setup(s => s.GetBucketNameFromEnvironmentVariable(It.IsAny<String>())).Returns(bucketName);
        mockWebService.Setup(s => s.GetCsvFileUrlFromXpathAndUri(It.IsAny<String>(), It.IsAny<Uri>())).ReturnsAsync(csvUrl);
        mockHttpService.Setup(s => s.GetFileNameAndDataFromUrl(It.IsAny<String>())).ThrowsAsync(exception);

        context.Logger = mockLambdaLogger.Object;

        _sutFunction = new Function(mockAmazonService.Object, mockHttpService.Object, mockWebService.Object);


        // Act / Assert
        var exc = Assert.CatchAsync<InvalidOperationException>(() => _sutFunction.FunctionHandler(_event, context));
        Assert.That(exc, 
            Is.TypeOf<InvalidOperationException>());
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event)))), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exception.Message)), 
            Times.Once);
    }

    [Test]
    public void FunctionHandler_CatchesAmazonS3Exception_WhenBucketDoesNotExist_ThrownByUploadToS3Bucket()
    {
        // Arrange
        var mockAmazonService = new Mock<IAmazonService>();
        var mockWebService = new Mock<IWebService>();
        var mockHttpService = new Mock<IHttpService>();
        var mockLambdaLogger = new Mock<ILambdaLogger>();
        var exceptionMessage = "The bucket you are attempting to access must be addressed using the specified endpoint. PLease send all future requests to this endpoint";
        var data = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 34, 86, 84};

        mockAmazonService.Setup(s => s.GetBucketNameFromEnvironmentVariable(It.IsAny<String>())).Returns(bucketName);
        mockWebService.Setup(s => s.GetCsvFileUrlFromXpathAndUri(It.IsAny<String>(), It.IsAny<Uri>())).ReturnsAsync(csvUrl);
        mockHttpService.Setup(s => s.GetFileNameAndDataFromUrl(It.IsAny<String>())).ReturnsAsync(Tuple.Create(data, fileName));
        mockAmazonService.Setup(s => s.UploadToS3Bucket(data, fileName, bucketName, _event.FilePrefixPattern))
            .Throws(new AmazonS3Exception(exceptionMessage));

        context.Logger = mockLambdaLogger.Object;

        _sutFunction = new Function(mockAmazonService.Object, mockHttpService.Object, mockWebService.Object);


        // Act / Assert
        var exc = Assert.CatchAsync<AmazonS3Exception>(() => _sutFunction.FunctionHandler(_event, context));
        Assert.That(exc, 
            Is.TypeOf<AmazonS3Exception>().With.Message.EqualTo(exceptionMessage));
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event)))), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exceptionMessage)), 
            Times.Once);
    }

    [Test]
    public void FunctionHandler_CatchesArgumentNullException_WhenBucketEnvVariableIsNotSet_ThrownByUploadToS3Bucket()
    {
        // Arrange
        var mockAmazonService = new Mock<IAmazonService>();
        var mockWebService = new Mock<IWebService>();
        var mockHttpService = new Mock<IHttpService>();
        var mockLambdaLogger = new Mock<ILambdaLogger>();
        var exceptionMessage = "BucketName is a required property and must be set before making this call. (Parameter 'PutObjectRequest.BucketName')";
        var data = new Byte[] {111, 123, 42, 22, 53, 1, 35, 64, 84 ,231, 34, 86, 84};
        var ex = new ArgumentNullException();

        mockAmazonService.Setup(s => s.GetBucketNameFromEnvironmentVariable(It.IsAny<String>())).Returns((String)null);
        mockWebService.Setup(s => s.GetCsvFileUrlFromXpathAndUri(It.IsAny<String>(), It.IsAny<Uri>())).ReturnsAsync(csvUrl);
        mockHttpService.Setup(s => s.GetFileNameAndDataFromUrl(It.IsAny<String>())).ReturnsAsync(Tuple.Create(data, fileName));
        mockAmazonService.Setup(s => s.UploadToS3Bucket(data, fileName, null, _event.FilePrefixPattern))
            .Throws(new ArgumentNullException(exceptionMessage));

        context.Logger = mockLambdaLogger.Object;

        _sutFunction = new Function(mockAmazonService.Object, mockHttpService.Object, mockWebService.Object);


        // Act / Assert
        var exc = Assert.CatchAsync<ArgumentNullException>(() => _sutFunction.FunctionHandler(_event, context));
        Assert.That(exc, 
            Is.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo(exceptionMessage));
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event)))), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == $"Value cannot be null. (Parameter '{exceptionMessage}')")), 
            Times.Once);
    }
}