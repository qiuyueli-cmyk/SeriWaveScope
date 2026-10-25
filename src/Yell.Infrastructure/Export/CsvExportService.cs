using System.IO;
using System.Text;
using Yell.Core.Contracts;
using Yell.Core.Models;

namespace Yell.Infrastructure.Export
{
    /// <summary>
    /// CSV 导出使用 UTF-8 BOM，Excel 直接打开不会乱码。
    /// </summary>
    public sealed class CsvExportService : ICsvExportService
    {
        public async Task ExportLogsAsync(string filePath, IEnumerable<LogEntry> entries)
        {
            var snapshot = entries.ToList();

            await using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
            await writer.WriteLineAsync("时间戳,数据类型,HEX 字符串,测量值");

            foreach (LogEntry item in snapshot)
            {
                string line = string.Join(",",
                    Escape(item.Time),
                    Escape(item.Direction),
                    Escape(item.RawData),
                    Escape(item.Value));
                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync();
        }

        public async Task ExportHistoryAsync(string filePath, IEnumerable<HistoryRecord> records)
        {
            var snapshot = records.ToList();

            await using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
            await writer.WriteLineAsync("完整时间戳,方向,HEX 字符串,测量值");

            foreach (HistoryRecord item in snapshot)
            {
                string line = string.Join(",",
                    Escape(item.TimestampText),
                    Escape(item.Direction),
                    Escape(item.RawData),
                    Escape(item.Value));
                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync();
        }

        private static string Escape(string? value)
        {
            string text = value ?? string.Empty;

            if (text.Contains(',') || text.Contains('"') ||
                text.Contains('\r') || text.Contains('\n'))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
