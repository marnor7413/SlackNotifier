using Google.Apis.Gmail.v1.Data;
using SN.Application.Dtos;

namespace SN.Application.Interfaces;

public interface IGmailPayloadService
{
    Task<List<EmailInfo>> CheckForEmails();
}