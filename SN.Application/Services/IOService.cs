using SN.Application.Interfaces;

namespace SN.Application.Services;

public class IOService : IIOService
{
    public string ReadFileFromDisk(string path, string filename)
    {
        var filePath = Path.Combine(path, filename);
        var fileData = File.ReadAllText(filePath);
       
        return fileData;
    }
}
