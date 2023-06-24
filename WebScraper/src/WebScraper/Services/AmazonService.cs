extern alias awssdk;

using System;
using System.Net;
using awssdk::Amazon.SimpleEmail;
using awssdk::Amazon.SimpleEmail.Model;
using Microsoft.Extensions.DependencyInjection;

namespace WebScraper.Services
{
    public class AmazonService : IAmazonService
    {
		private IAmazonSimpleEmailService _amazonSimpleEmailService;
		private SendEmailRequest _sendEmailRequest;
		private GetIdentityVerificationAttributesRequest _getIdentityVerificationAttributesRequest;
		private ListIdentitiesRequest _listIdentitiesRequest;
		private VerifyEmailIdentityRequest _verifyEmailIdentityRequest;

		public AmazonService(
			IAmazonSimpleEmailService amazonSimpleEmailService,
			GetIdentityVerificationAttributesRequest getIdentityVerificationAttributesRequest,
			SendEmailRequest sendEmailRequest,
			VerifyEmailIdentityRequest verifyEmailIdentityRequest,
			ListIdentitiesRequest listIdentitiesRequest)
		{
            ConfigureServices(amazonSimpleEmailService, getIdentityVerificationAttributesRequest, sendEmailRequest, verifyEmailIdentityRequest, listIdentitiesRequest);
		}

        public async Task<string> CheckVerificationStatusOfIdentitiesAsync(string email)
        {
            _listIdentitiesRequest.IdentityType = IdentityType.EmailAddress;
            var identites = _amazonSimpleEmailService.ListIdentities(_listIdentitiesRequest);
            if(!identites.Identities.Contains(email))
                throw new AmazonSimpleEmailServiceException($"The email identity '{email}' does not exist, The email identity must be a verified email or domain");    
            _getIdentityVerificationAttributesRequest.Identities = new List<String> { email };

            var response = await _amazonSimpleEmailService.GetIdentityVerificationAttributesAsync(_getIdentityVerificationAttributesRequest);

            var responseAttributes = response.VerificationAttributes[email];

            return responseAttributes.VerificationStatus.Value;
        }

        public async Task<SendEmailResponse> SendEmailAsync(string html, string from, List<string> to)
        {
            _sendEmailRequest.Source = from;
            _sendEmailRequest.Destination = new Destination {
                ToAddresses = to
            };
            _sendEmailRequest.Message = new Message {
                Subject = new Content("Testing Amazon SES through the API"),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = html
                    },
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = "Testing Amazon SES through the API"
                    }
                }
            };
            var response = await _amazonSimpleEmailService.SendEmailAsync(_sendEmailRequest);
            if(response.HttpStatusCode != HttpStatusCode.OK)
                throw new MessageRejectedException($"Failed to Send email. Status Code: {response.HttpStatusCode}");
            return response;
        }

        public async Task<bool> VerifyEmailIdentityAsync(string emailAddress)
        {
            _verifyEmailIdentityRequest.EmailAddress = emailAddress;
            var response = await _amazonSimpleEmailService.VerifyEmailIdentityAsync(_verifyEmailIdentityRequest);
            Boolean success = response.HttpStatusCode == HttpStatusCode.OK;
            return success;
        }


        private void ConfigureServices(IAmazonSimpleEmailService sesService = null, 
        GetIdentityVerificationAttributesRequest identityVerificationAttributesRequest = null,
        SendEmailRequest sendEmailRequest = null,
        VerifyEmailIdentityRequest verifyEmailIdentityRequest = null,
        ListIdentitiesRequest listIdentitiesRequest = null)
        {
            Startup.ConfigureServices();
            _amazonSimpleEmailService = sesService ?? Startup.Services.GetRequiredService<IAmazonSimpleEmailService>();
            _getIdentityVerificationAttributesRequest = identityVerificationAttributesRequest ?? Startup.Services.GetRequiredService<GetIdentityVerificationAttributesRequest>();
            _sendEmailRequest = sendEmailRequest ?? Startup.Services.GetRequiredService<SendEmailRequest>();
            _verifyEmailIdentityRequest = verifyEmailIdentityRequest ?? Startup.Services.GetRequiredService<VerifyEmailIdentityRequest>();
            _listIdentitiesRequest = listIdentitiesRequest ?? Startup.Services.GetRequiredService<ListIdentitiesRequest>();
        }
    }
}
