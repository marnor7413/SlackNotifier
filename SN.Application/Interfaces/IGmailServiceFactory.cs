﻿using Google.Apis.Gmail.v1;

namespace MailService.Infrastructure.Factories;

public interface IGmailServiceFactory
{
    Task<GmailService> GetService();
}