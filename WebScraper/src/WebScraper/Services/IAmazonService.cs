extern alias awssdk;

using System;
using awssdk::Amazon.SimpleEmail.Model;

namespace WebScraper.Services
{
    public interface IAmazonService
    {
        Task<String> CheckVerificationStatusOfIdentitiesAsync(String email);
        Task<SendEmailResponse> SendEmailAsync(String html, String from, List<String> to);
        Task<Boolean> VerifyEmailIdentityAsync(String emailAddress);
    }
}
