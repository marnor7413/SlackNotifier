﻿namespace MailService.Infrastructure.EmailService;

public record FileAttachment(string FileName, string fileType, string Description, string Data)
{
    public byte[] ToByteArray()
    {
        var base64String = Data.Replace("-", "+").Replace("_", "/");

        return Convert.FromBase64String(base64String);
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(FileName)) return false;
        if (string.IsNullOrWhiteSpace(fileType)) return false;
        if (string.IsNullOrWhiteSpace(Data)) return false;

        return true;
    }
}