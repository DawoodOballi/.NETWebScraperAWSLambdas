extern alias awssdk;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using awssdk::Amazon.SimpleEmail;
using awssdk::Amazon.SimpleEmail.Model;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using QuickType;
using WebScraper.Services;

namespace WebScraper.Tests;

public class FunctionHandlerTests
{
    private WebScraperEvent _event;
    private TestLambdaContext context;
    private Function function;
    private GetIdentityVerificationAttributesRequest identityVerificationAttributesRequest;
    private GetIdentityVerificationAttributesResponse identityVerificationAttributesResponse;
    private ListIdentitiesRequest listIdentitiesRequest;
    private ListIdentitiesResponse listIdentitiesResponse;
    private VerifyEmailIdentityRequest verifyEmailIdentityRequest;
    private VerifyEmailIdentityResponse verifyEmailIdentityResponse;
    private SendEmailRequest sendEmailRequest;
    private SendEmailResponse sendEmailResponse;
    private String html;

    [SetUp]
    public void Setup()
    {
        html = @"<html><body>
            <a href=""foo.bar"" class=""blap"">blip</a>
            Hello
            </body></html>";

        _event = new() {
            EventId = "12345567890",
            From = "hello@world.com",
            To = new List<string> {
                "dawidoiajwd@example.com"
            }.ToArray(),
            Websiteurl = new Uri("https://www.example.com"),
            Xpath = "/div"
        };

        context = new();
        function = new();
        identityVerificationAttributesRequest = new() {
            Identities = new List<string> {
                "dawidoiajwd@example.com"
            }
        };

        identityVerificationAttributesResponse = new() {
            HttpStatusCode = HttpStatusCode.OK,
            VerificationAttributes = new Dictionary<string, IdentityVerificationAttributes> {
                { "dawidoiajwd@example.com", new IdentityVerificationAttributes() { VerificationStatus = new VerificationStatus("Success") }},
                { _event.From, new IdentityVerificationAttributes() { VerificationStatus = new VerificationStatus("Success") }}
            }
        };

        listIdentitiesRequest = new();

        listIdentitiesResponse = new() {
            HttpStatusCode = HttpStatusCode.OK,
            Identities = new List<string>() {
                "dawidoiajwd@example.com",
                _event.From
            }
        };

        verifyEmailIdentityRequest = new() {
            EmailAddress = _event.From
        };

        verifyEmailIdentityResponse = new() {
            HttpStatusCode = HttpStatusCode.OK
        };

        sendEmailResponse = new() {
            HttpStatusCode = HttpStatusCode.OK
        };

        sendEmailRequest = new() {
            Destination = new Destination() {
                ToAddresses = _event.To.ToList()
            },
            Source = _event.From,
            Message = new Message() {
                Subject = new Content("This is the subject of the email"),
                Body = new Body() {
                    Text = new Content() {
                        Charset = "UTF-8",
                        Data = "This is some plain text for the email body"
                    },
                    Html = new Content() {
                        Charset = "UTF-8",
                        Data = html
                    }
                }
            }
        };
    }


    [Test]
    public async Task FunctionHandler_ReturnsTheInputInReadableJsonFormat()
    {
        // Arrange
        var mockLambdaLogger =  new Mock<ILambdaLogger>();
        var mockAmazonService = new Mock<IAmazonService>();
        var mockHttpService = new Mock<IHttpService>();
        Object? jsonEvent = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event));

        context.Logger = mockLambdaLogger.Object;
        mockAmazonService.Setup(s => s.CheckVerificationStatusOfIdentitiesAsync(It.IsAny<String>())).ReturnsAsync("Success");
        mockAmazonService.Setup(s => s.VerifyEmailIdentityAsync(It.IsAny<String>())).ReturnsAsync(true); // Not actually needed since the method call before it setup to return 'Success'
        mockHttpService.Setup(s => s.QueryWebsite(It.IsAny<Uri>())).ReturnsAsync(html);
        mockAmazonService.Setup(s => s.SendEmailAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<List<String>>())).ReturnsAsync(sendEmailResponse);


        function = new Function(mockAmazonService.Object, mockHttpService.Object);
        // Act
        var result = await function.FunctionHandler(_event, context);
        
        // Assert
        Assert.That(result, Is.EqualTo(_event));
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.From} has status of Success")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.To[0]} has status of Success")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + jsonEvent)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Sending email using Amazon SES...")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "The email was sent successfully.")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.IsAny<String>()), Times.AtMost(2));
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.IsAny<String>()), Times.Exactly(3));
    }

    [Test]
    public async Task FunctionHandler_CatchesAggregateException_WithAmazonSESExceptionAsInnerException()
    {
        // Arrange
        var mockLambdaLogger =  new Mock<ILambdaLogger>();
        var mockAmazonService = new Mock<IAmazonService>();
        var mockHttpService = new Mock<IHttpService>();
        var exceptionMessage = $"The email identity '{_event.From}' does not exist, The email identity must be a verified email or domain";

        mockAmazonService.Setup(s => s.CheckVerificationStatusOfIdentitiesAsync(It.IsAny<String>())).ThrowsAsync(new AmazonSimpleEmailServiceException(exceptionMessage));

        context.Logger = mockLambdaLogger.Object;
        function = new Function(mockAmazonService.Object, mockHttpService.Object);
        
        // Act / Assert
        var exc = Assert.CatchAsync<AmazonSimpleEmailServiceException>(() => function.FunctionHandler(_event, context));
        Assert.That(exc, 
            Is.TypeOf<AmazonSimpleEmailServiceException>().With.Message
            .EqualTo(exceptionMessage));
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exceptionMessage)), 
            Times.Once);
    }

    [Test]
    public void FunctionHandler_ThrowsAmazonSESExceptionAsInnerException_WhenVerifyEmailIdentityAsync_ReturnsHttpStatusCode_NotEqualToOK()
    {
        // Arrange
        var mockLambdaLogger =  new Mock<ILambdaLogger>();
        var mockAmazonService = new Mock<IAmazonService>();
        var exceptionMessage = $"Could not send verification email to {_event.From}";

        mockAmazonService.Setup(s => s.CheckVerificationStatusOfIdentitiesAsync(It.IsAny<String>())).ReturnsAsync("Pending");
        mockAmazonService.Setup(s => s.VerifyEmailIdentityAsync(It.IsAny<String>())).ReturnsAsync(false);
        
        context.Logger = mockLambdaLogger.Object;
        function = new Function(mockAmazonService.Object, null);
        
        // Act / Assert
        var ex = Assert.ThrowsAsync<AmazonSimpleEmailServiceException>(() => function.FunctionHandler(_event, context));
        Assert.That(ex.Message, Is.EqualTo(exceptionMessage));
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exceptionMessage)), 
            Times.Once);
    }

    [Test]
    public void FunctionHandler_CatchesInvalidOperationException_WhenUriIsEmptyString()
    {
        // Arrange
        var mockLambdaLogger =  new Mock<ILambdaLogger>();
        var mockAmazonService = new Mock<IAmazonService>();
        var mockHttpService = new Mock<IHttpService>();
        Object? jsonEvent = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event));

        Uri uri = null;
        try
        {
            uri = new Uri("");
        }
        catch
        {
            // Do nothing
        }

        mockAmazonService.Setup(s => s.CheckVerificationStatusOfIdentitiesAsync(It.IsAny<String>())).ReturnsAsync("Success");
        mockHttpService.Setup(s => s.QueryWebsite(It.IsAny<Uri>())).ThrowsAsync(new InvalidOperationException());
        
        context.Logger = mockLambdaLogger.Object;
        function = new Function(mockAmazonService.Object, mockHttpService.Object);
        
        // Act / Assert
        var ex = Assert.CatchAsync(async () => await function.FunctionHandler(_event, context));
        Assert.That(ex, Is.TypeOf<InvalidOperationException>());
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == new InvalidOperationException().Message)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.From} has status of Success")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.To[0]} has status of Success")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + jsonEvent)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.IsAny<String>()), Times.Exactly(0));
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.IsAny<String>()), Times.Exactly(3));
    }

    [Test]
    public void FunctionHandler_CatchesHttpRequestException_WhenUriIsInvalid()
    {
        // Arrange
        var mockLambdaLogger =  new Mock<ILambdaLogger>();
        var mockAmazonService = new Mock<IAmazonService>();
        var mockHttpService = new Mock<IHttpService>();

        _event.Websiteurl = new Uri("https://wwww.example.com");
        Object? jsonEvent = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event));

        mockAmazonService.Setup(s => s.CheckVerificationStatusOfIdentitiesAsync(It.IsAny<String>())).ReturnsAsync("Success");
        mockHttpService.Setup(s => s.QueryWebsite(_event.Websiteurl)).ThrowsAsync(new HttpRequestException());
        
        context.Logger = mockLambdaLogger.Object;
        function = new Function(mockAmazonService.Object, mockHttpService.Object);
        
        // Act / Assert
        var ex = Assert.CatchAsync(async () => await function.FunctionHandler(_event, context));
        Assert.That(ex, Is.TypeOf<HttpRequestException>());
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == new HttpRequestException().Message)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.From} has status of Success")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.To[0]} has status of Success")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + jsonEvent)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.IsAny<String>()), Times.Exactly(0));
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.IsAny<String>()), Times.Exactly(3));
    }

    [Test]
    public void FunctionHandler_CatchesMessageRejectedException_WhenEmailIds_AreNotVerified()
    {
        // Arrange
        var mockLambdaLogger =  new Mock<ILambdaLogger>();
        var mockAmazonService = new Mock<IAmazonService>();
        var mockHttpService = new Mock<IHttpService>();
        var emails = new List<String>();
        emails.Add(_event.From);
        emails.AddRange(_event.To); 
        var exceptionMessage = $"Email address is not verified. The following identites failed the check in region EU-WEST-2: {String.Join(", ", emails)}";

        _event.Websiteurl = new Uri("https://wwww.example.com");
        Object? jsonEvent = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(_event));

        mockAmazonService.Setup(s => s.CheckVerificationStatusOfIdentitiesAsync(It.IsAny<String>())).ReturnsAsync("Pending");
        mockAmazonService.Setup(s => s.VerifyEmailIdentityAsync(It.IsAny<String>())).ReturnsAsync(true);
        mockHttpService.Setup(s => s.QueryWebsite(_event.Websiteurl)).ReturnsAsync(html);
        mockAmazonService.Setup(s => s.SendEmailAsync(html, _event.From, _event.To.ToList())).ThrowsAsync(new MessageRejectedException(exceptionMessage));

        
        context.Logger = mockLambdaLogger.Object;
        function = new Function(mockAmazonService.Object, mockHttpService.Object);
        
        // Act / Assert
        var ex = Assert.CatchAsync(async () => await function.FunctionHandler(_event, context));
        Assert.That(ex, Is.TypeOf<MessageRejectedException>().With.Message.EqualTo(exceptionMessage));
        mockLambdaLogger.Verify(
            x => x.LogError(It.Is<string>((v, t) =>  v.ToString() == exceptionMessage)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.From} has status of Pending")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == $"The email: {_event.To[0]} has status of Pending")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"A verification email has been sent to {_event.From}")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == $"A verification email has been sent to {_event.To[0]}")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.Is<string>((v, t) =>  v.ToString() == "Event: \n" + jsonEvent)), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.Is<string>((v, t) =>  v.ToString() == "Sending email using Amazon SES...")), 
            Times.Once);
        mockLambdaLogger.Verify(
            x => x.LogInformation(It.IsAny<String>()), Times.Exactly(3));
        mockLambdaLogger.Verify(
            x => x.LogCritical(It.IsAny<String>()), Times.Exactly(3));
    }
}
