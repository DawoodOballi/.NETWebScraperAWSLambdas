extern alias awssdk;
extern alias awssdkcore;

using System.Net;
using Amazon.Lambda.Core;
using awssdk::Amazon.SimpleEmail;
using awssdk::Amazon.SimpleEmail.Model;
using awssdkcore::Amazon.Runtime.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuickType;
using WebScraper.Services;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WebScraper;

public class Function
{
    // private readonly RegionEndpoint REGION = RegionEndpoint.GetBySystemName("eu-west-2");
    private IAmazonService _amazonService;
    private IHttpService _httpService;

    public Function() => ConfigureServices();
    

    /// <summary>
    /// This constructor is being used for testing purposes since the Lmabda requires a parameterless constructor for it to work
    /// </summary>
    /// <param name="sesService"></param>
    /// <param name="identityVerificationAttributesRequest"></param>
    /// <param name="sendEmailRequest"></param>
    /// <param name="verifyEmailIdentityRequest"></param>
    public Function(IAmazonService amazonService, IHttpService httpService) 
    {
        ConfigureServices(amazonService, httpService);
    }
    


    public async Task<WebScraperEvent> FunctionHandler(WebScraperEvent input, ILambdaContext context)
    {
        try {
            var emailsToVerify = new List<String>
            {
                input.From
            };
            emailsToVerify.AddRange(input.To);

            emailsToVerify.ForEach((e) => {
                String status = _amazonService.CheckVerificationStatusOfIdentitiesAsync(e).Result;
                context.Logger.LogCritical($"The email: {e} has status of {status}");
                if(status != "Success")
                {
                    var success = _amazonService.VerifyEmailIdentityAsync(e).Result;

                    if(!success)
                        throw new AmazonSimpleEmailServiceException($"Could not send verification email to {e}");

                    context.Logger.LogInformation($"A verification email has been sent to {e}");
                }
            });

            String _event = JsonConvert.SerializeObject(input);
            Object? jsonEvent = JsonConvert.DeserializeObject(_event);

            context.Logger.LogCritical("Event: \n" + jsonEvent);

            String html = await _httpService.QueryWebsite(input.Websiteurl);

            context.Logger.LogInformation("Sending email using Amazon SES...");
            var response = await _amazonService.SendEmailAsync(html, input.From, input.To.ToList());
            context.Logger.LogInformation("The email was sent successfully.");
        }
        catch (MessageRejectedException ex) { context.Logger.LogError(ex.Message); throw; }
        catch (AmazonSimpleEmailServiceException ex) { context.Logger.LogError(ex.Message); throw; }
        catch (HttpRequestException ex) { context.Logger.LogError(ex.Message); throw; }
        catch (InvalidOperationException ex) { context.Logger.LogError(ex.Message); throw; }
        catch (AggregateException ex) { 
            context.Logger.LogError(ex.InnerException.Message);
            throw ex.InnerException;
        }
        return input;
    }

    
    private void ConfigureServices(IAmazonService amazonService = null, 
    IHttpService httpService = null)
    {
        Startup.ConfigureServices();
        _amazonService = amazonService ?? Startup.Services.GetRequiredService<IAmazonService>();
        _httpService = httpService ?? Startup.Services.GetRequiredService<IHttpService>();
    }
}