﻿using Google.Apis.Gmail.v1;

namespace MailService.Infrastructure.Factories;

public interface IGmailClientFactorySericeAccount
{
    GmailService CreateGmailClient();
}