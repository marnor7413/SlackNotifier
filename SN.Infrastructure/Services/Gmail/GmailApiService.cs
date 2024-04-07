using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MailService.Infrastructure.Factories;
using SN.Application.Interfaces;

namespace SN.Infrastructure.Services.Gmail;

public class GmailApiService : IGmailFetchService
{
    private readonly IGmailServiceFactory gmailServiceFactory;
    private GmailService gmailService;

    private const string FilterUnreadEmailsOnly = "is:unread";
    private const string InboxFolder = "INBOX";

    public GmailApiService(IGmailServiceFactory gmailServiceFactory)
    {
        this.gmailServiceFactory = gmailServiceFactory;
    }
   
    public async Task<List<Message>> GetListOfMessages()
    {
        List<Message> emailListResponse;
        gmailService = await gmailServiceFactory.GetService();
        var emailListRequest = gmailService.Users.Messages.List(GmailServiceFactory.AuthenticatedUser);
        emailListRequest.LabelIds = InboxFolder;
        emailListRequest.IncludeSpamTrash = false;
        emailListRequest.Q = FilterUnreadEmailsOnly;

        try
        {
            var response = await emailListRequest.ExecuteAsync();
            emailListResponse = response.Messages?.ToList() ?? Enumerable.Empty<Message>().ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] An unknown error occured. Exception message: {ex.Message}");
            throw;
        }

        return emailListResponse;
    }

    public async Task<Message> DownloadEmail(string emailId)
    {
        Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] Fetching gmail messages.");
        var emailRequest = gmailService.Users.Messages.Get(GmailServiceFactory.AuthenticatedUser, emailId);
        var message = await emailRequest.ExecuteAsync();
        Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] Message received.");
        
        return message;
    }

    public async Task MoveMessageToTrash(string emailId)
    {
        await gmailService.Users.Messages
                            .Trash(GmailServiceFactory.AuthenticatedUser, emailId)
                            .ExecuteAsync();
    }

    public async Task ToggleMessageToRead(string emailId)
    {
        var modifyRequest = new ModifyMessageRequest
        {
            RemoveLabelIds = new List<string> { "UNREAD" },
            AddLabelIds = null 
        };
        await gmailService.Users.Messages
            .Modify(modifyRequest, GmailServiceFactory.AuthenticatedUser, emailId)
            .ExecuteAsync();
    }

    public async Task<MessagePartBody> DownloadAttachment(string messageId, string attachmentId)
    {
        return await gmailService.Users.Messages.Attachments
                 .Get(GmailServiceFactory.AuthenticatedUser, messageId, attachmentId)
                 .ExecuteAsync();
    }
}