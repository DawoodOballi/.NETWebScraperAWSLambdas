extern alias awssdk;
extern alias awssdkcore;

using System.Net;
using NUnit.Framework;
using Moq;
using awssdk::Amazon;
using awssdk::Amazon.SimpleEmail;
using awssdk::Amazon.SimpleEmail.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using QuickType;
using Moq.Protected;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using WebScraper.Services;

namespace WebScraper.Tests;

public class ServicesTests
{
    private WebScraperEvent? _event;
    private IAmazonService amazonService;
    private IHttpService httpService;
    private TestLambdaContext? context;
    private Function? function;

    [SetUp]
    public void Setup()
    {
        _event = new WebScraperEvent() {
            EventId = "12345567890",
            From = "hello@world.com",
            To = new List<string> {
                "dawidoiajwd@example.com"
            }.ToArray(),
            Websiteurl = new Uri("https://www.example.com"),
            Xpath = "/div"
        };
        context = new TestLambdaContext();
        function = new Function();
    }

   
    [Test]
    public void CheckVerificationStatusOfIdentitiesAsync_ReturnsSuccess_WhenTheEmailIdentitiesDoExistAndHaveBeenVerified()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Loose);
        // var REGION = RegionEndpoint.GetBySystemName("eu-west-2");
        // var sesService = new AmazonSimpleEmailServiceClient(REGION);
        var identityVerificationAttributesRequest = new GetIdentityVerificationAttributesRequest() {
            Identities = new List<string> {
                "dawidoiajwd@example.com"
            }
        };

        var identityVerificationAttributesResponse = new GetIdentityVerificationAttributesResponse() {
            HttpStatusCode = HttpStatusCode.OK,
            VerificationAttributes = new Dictionary<string, IdentityVerificationAttributes> {
                { "dawidoiajwd@example.com", new IdentityVerificationAttributes() { VerificationStatus = new VerificationStatus("Success") }}
            }
        };

        var listIdentitiesRequest = new ListIdentitiesRequest() {
            IdentityType = IdentityType.EmailAddress
        };

        var listIdentitiesResponse = new ListIdentitiesResponse() {
            HttpStatusCode = HttpStatusCode.OK,
            Identities = new List<string> {
                "dawidoiajwd@example.com"
            }
        };
        mockAmazonSesClient.Setup(f => f.ListIdentities(It.IsAny<ListIdentitiesRequest>())).Returns(listIdentitiesResponse);
        mockAmazonSesClient.Setup(f => f.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), default)).ReturnsAsync(identityVerificationAttributesResponse);
        amazonService = new AmazonService(mockAmazonSesClient.Object, identityVerificationAttributesRequest, null, null, listIdentitiesRequest);


        // Act
        var result = amazonService.CheckVerificationStatusOfIdentitiesAsync("dawidoiajwd@example.com").Result;


        // Assert
        Assert.That(result, Is.EqualTo("Success"));
    }

    [Test]
    public void CheckVerificationStatusOfIdentitiesAsync_ReturnsPending_WhenTheEmailIdentitiesDoExistAndHaveNotBeenVerified()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Loose);
        // var REGION = RegionEndpoint.GetBySystemName("eu-west-2");
        // var sesService = new AmazonSimpleEmailServiceClient(REGION);
        var identityVerificationAttributesRequest = new GetIdentityVerificationAttributesRequest() {
            Identities = new List<string> {
                "dawidoiajwd@example.com"
            }
        };

        var identityVerificationAttributesResponse = new GetIdentityVerificationAttributesResponse() {
            HttpStatusCode = HttpStatusCode.OK,
            VerificationAttributes = new Dictionary<string, IdentityVerificationAttributes> {
                { "dawidoiajwd@example.com", new IdentityVerificationAttributes() { VerificationStatus = new VerificationStatus("Pending") }}
            }
        };

        var listIdentitiesRequest = new ListIdentitiesRequest() {
            IdentityType = IdentityType.EmailAddress
        };

        var listIdentitiesResponse = new ListIdentitiesResponse() {
            HttpStatusCode = HttpStatusCode.OK,
            Identities = new List<string> {
                "dawidoiajwd@example.com"
            }
        };
        mockAmazonSesClient.Setup(f => f.ListIdentities(It.IsAny<ListIdentitiesRequest>())).Returns(listIdentitiesResponse);
        mockAmazonSesClient.Setup(f => f.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), default)).ReturnsAsync(identityVerificationAttributesResponse);
        amazonService = new AmazonService(mockAmazonSesClient.Object, identityVerificationAttributesRequest, null, null, listIdentitiesRequest);


        // Act
        var result = amazonService.CheckVerificationStatusOfIdentitiesAsync("dawidoiajwd@example.com").Result;


        // Assert
        Assert.That(result, Is.EqualTo("Pending"));
    }

    [Test]
    public void CheckVerificationStatusOfIdentitiesAsync_ThrowsAmazonSESException_WhenTheEmailIdentitiesDontExist()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Loose);

        var listIdentitiesRequest = new ListIdentitiesRequest() {
            IdentityType = IdentityType.EmailAddress
        };

        mockAmazonSesClient.Setup(f => f.ListIdentities(It.IsAny<ListIdentitiesRequest>()))
            .Returns(new ListIdentitiesResponse() { 
                Identities = new List<string>() {
                    "hello@world.com"
                }
            });

        amazonService = new AmazonService(mockAmazonSesClient.Object, null, null, null, listIdentitiesRequest);


        // Act
        var resultAction = () => amazonService.CheckVerificationStatusOfIdentitiesAsync("example@email.com");


        // Assert
        Assert.That(resultAction, 
            Throws.Exception.TypeOf<AmazonSimpleEmailServiceException>().With.Message
            .EqualTo($"The email identity 'example@email.com' does not exist, The email identity must be a verified email or domain"));
    }

    [Test]
    public void QueryWebsite_ThrowsHttpRequestException_WhenUriIsInvalid()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Loose);
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockhttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var uri = new Uri("https://www.dawdawdw.com/");
        
        mockhttpClientFactory.Setup(r => r.CreateClient("webscraper"))
            .Returns(new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = uri
            });

        httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == uri),
            ItExpr.IsAny<CancellationToken>()
        ).ThrowsAsync(new HttpRequestException());

        httpService = new HttpService(mockhttpClientFactory.Object);


        // Act
        var resultAction = async () => await httpService.QueryWebsite(uri);

        // Assert
        Assert.That(resultAction, Throws.Exception.TypeOf<HttpRequestException>());
    }

    [Test]
    public void QueryWebsite_ThrowsInvalidOperationException_WhenUriIsEmptyString()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Loose);
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockhttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        Uri uri = null;
        try
        {
            uri = new Uri("");
        }
        catch
        {
            // Do nothing
        }
        
        mockhttpClientFactory.Setup(r => r.CreateClient("webscraper"))
            .Returns(new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = uri
            });

        httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == uri),
            ItExpr.IsAny<CancellationToken>()
        ).ThrowsAsync(new InvalidOperationException());

        httpService = new HttpService(mockhttpClientFactory.Object);


        // Act
        var resultAction = async () => await httpService.QueryWebsite(uri);

        // Assert
        Assert.That(resultAction, Throws.Exception.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task QueryWebsite_ReturnsHtmlString_WhenUriIsValid()
    {
        // Arrange
        string html = @"<html><body>
            <a href=""foo.bar"" class=""blap"">blip</a>
            Hello
            </body></html>";
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockhttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var uri = new Uri("https://www.dawdawdw.com/");
        
        mockhttpClientFactory.Setup(r => r.CreateClient("webscraper"))
            .Returns(new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = uri
            });

        httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == uri),
            ItExpr.IsAny<CancellationToken>()
        ).ReturnsAsync(new HttpResponseMessage() {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(html)
        })
        .Verifiable();

        httpService = new HttpService(mockhttpClientFactory.Object);


        // Act
        var result = await httpService.QueryWebsite(uri);

        // Assert
        Assert.That(result, Is.EqualTo(html));
    }

    [Test]
    public async Task SendEmailAsync_ReturnsSendEmailResponse_WithHttpStatusCodeOK()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Strict);
        string html = @"<html><body>
            <a href=""foo.bar"" class=""blap"">blip</a>
            Hello
            </body></html>";
        
        var sendEmailRequest = new SendEmailRequest() {
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

        mockAmazonSesClient.Setup(f => f.SendEmailAsync(It.IsAny<SendEmailRequest>(), default)).ReturnsAsync(new SendEmailResponse() {HttpStatusCode = HttpStatusCode.OK, ContentLength = 90, MessageId = "2847105710271082708127480127"});
        amazonService = new AmazonService(mockAmazonSesClient.Object, null, sendEmailRequest, null, null);


        // Act
        var result = await amazonService.SendEmailAsync(html, _event.From, _event.To.ToList());

        // Assert
        Assert.That(result.HttpStatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result.ContentLength, Is.EqualTo(90));
        Assert.That(result.MessageId, Is.EqualTo("2847105710271082708127480127"));
    }

    [Test]
    public void SendEmailAsync_ThrowsMessageRejectedException_WhenEmailId_s_NotValid()
    {
        // Arrange
        var emails = new List<String>();
        emails.Add(_event.From);
        emails.AddRange(_event.To);
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Strict);
        string html = @"<html><body>
            <a href=""foo.bar"" class=""blap"">blip</a>
            Hello
            </body></html>";
        
        var sendEmailRequest = new SendEmailRequest() {
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

        mockAmazonSesClient.Setup(f => f.SendEmailAsync(It.IsAny<SendEmailRequest>(), default)).ThrowsAsync(new MessageRejectedException($"Email address is not verified. The following identites failed the check in region EU-WEST-2: {String.Join(", ", emails)}"));
        amazonService = new AmazonService(mockAmazonSesClient.Object, null, sendEmailRequest, null, null);


        // Act
        var resultAction = () => amazonService.SendEmailAsync(html, _event.From, _event.To.ToList());

        // Assert
        Assert.That(resultAction, Throws.Exception.TypeOf<MessageRejectedException>().With.Message.EqualTo($"Email address is not verified. The following identites failed the check in region EU-WEST-2: {String.Join(", ", emails)}"));
    }

    [Test]
    public void SendEmailAsync_ThrowsMessageRejectedException_WhenHttpStatusCodeInResponseIsNotOK()
    {
        // Arrange
        var emails = new List<String>();
        emails.Add(_event.From);
        emails.AddRange(_event.To);
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Strict);
        string html = @"<html><body>
            <a href=""foo.bar"" class=""blap"">blip</a>
            Hello
            </body></html>";
        
        var sendEmailRequest = new SendEmailRequest() {
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

        mockAmazonSesClient.Setup(f => f.SendEmailAsync(It.IsAny<SendEmailRequest>(), default)).ReturnsAsync(new SendEmailResponse() { HttpStatusCode = HttpStatusCode.Forbidden});
        amazonService = new AmazonService(mockAmazonSesClient.Object, null, sendEmailRequest, null, null);


        // Act
        var resultAction = () => amazonService.SendEmailAsync(html, _event.From, _event.To.ToList());

        // Assert
        Assert.That(resultAction, Throws.Exception.TypeOf<MessageRejectedException>().With.Message.EqualTo($"Failed to Send email. Status Code: {HttpStatusCode.Forbidden}"));
    }

    [Test]
    public async Task VerifyEmailIdentityAsync_ReturnsTrue_WhenEmailIdentityExists()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Strict);

        mockAmazonSesClient.Setup(f => f.VerifyEmailIdentityAsync(It.IsAny<VerifyEmailIdentityRequest>(), default)).ReturnsAsync(new VerifyEmailIdentityResponse {HttpStatusCode = HttpStatusCode.OK});
        amazonService = new AmazonService(mockAmazonSesClient.Object, null, null, null, null);


        // Act
        var result = await amazonService.VerifyEmailIdentityAsync(_event.From);

        // Assert
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public async Task VerifyEmailIdentityAsync_ReturnsFalse_WhenHttpStatusCodeInResponseIsNotOK()
    {
        // Arrange
        var mockAmazonSesClient = new Mock<IAmazonSimpleEmailService>(MockBehavior.Strict);
        mockAmazonSesClient.Setup(f => f.VerifyEmailIdentityAsync(It.IsAny<VerifyEmailIdentityRequest>(), default)).ReturnsAsync(new VerifyEmailIdentityResponse {HttpStatusCode = HttpStatusCode.Forbidden});
        amazonService = new AmazonService(mockAmazonSesClient.Object, null, null, null, null);


        // Act
        var result = await amazonService.VerifyEmailIdentityAsync(_event.From);

        // Assert
        Assert.That(result, Is.EqualTo(false));
    }
}
