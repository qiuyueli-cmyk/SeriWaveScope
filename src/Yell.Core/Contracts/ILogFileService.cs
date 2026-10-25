namespace Yell.Core.Contracts
{
    public interface ILogFileService
    {
        Task WriteAsync(string direction, string rawData, string parsedValue);
    }
}
