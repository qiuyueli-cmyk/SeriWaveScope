using Yell.Core.Models;

namespace Yell.Core.Contracts
{
    public interface ICsvExportService
    {
        Task ExportLogsAsync(string filePath, IEnumerable<LogEntry> entries);

        Task ExportHistoryAsync(string filePath, IEnumerable<HistoryRecord> records);
    }
}
