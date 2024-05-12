using SN.Application.Builders;

namespace SN.Application.Interfaces;

public interface ISlackBlockBuilder
{
    StringContent Build();
    void Clear();
    ISlackBlockBuilder FromSender(string sender);
    ISlackBlockBuilder ToChannel(string channel);
    ISlackBlockBuilder WithDivider();
    ISlackBlockBuilder WithHeaderTitle(string header);
    ISlackBlockBuilder WithMessageBody(string messageBody);
    ISlackBlockBuilder WithRelatedFile(string fileName, string fileId);
    ISlackBlockBuilder WithRelatedFiles(Dictionary<string, string> fileNames);
    ISlackBlockBuilder WithSendDate(string date);
    ISlackBlockBuilder WithSubject(string subject);
}