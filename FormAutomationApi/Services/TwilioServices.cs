using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

public class TwilioService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _messagingServiceSid;

    public TwilioService()
    {
        _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        _authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        _messagingServiceSid = Environment.GetEnvironmentVariable("TWILIO_MESSAGING_SID");

        TwilioClient.Init(_accountSid, _authToken);
    }

    // ✅ MAIN METHOD (your requirement)
    public async Task<string> SendFormLink(string phoneNumber, string formUrl)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            throw new Exception("Phone number is required");

        if (string.IsNullOrEmpty(formUrl))
            throw new Exception("Form URL is required");

        var messageBody = $"Hi, please fill your form here: {formUrl}";

        var options = new CreateMessageOptions(
            new PhoneNumber(phoneNumber))
        {
            MessagingServiceSid = _messagingServiceSid,
            Body = messageBody
        };

       var message= await MessageResource.CreateAsync(options);
        return message.Sid ;

    }
}