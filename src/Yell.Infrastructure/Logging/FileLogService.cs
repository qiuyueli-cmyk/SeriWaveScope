using System.Diagnostics;
using System.IO;
using System.Text;
using Yell.Core.Contracts;

namespace Yell.Infrastructure.Logging
{
    /// <summary>
    /// 按天滚动的本地日志：Logs/yyyy-MM-dd.log。
    /// </summary>
    public sealed class FileLogService : ILogFileService
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly string _logFolder = Path.Combine(AppContext.BaseDirectory, "Logs");

        public async Task WriteAsync(string direction, string rawData, string parsedValue)
        {
            try
            {
                string displayValue = string.IsNullOrWhiteSpace(parsedValue) ? "-" : parsedValue;
                Directory.CreateDirectory(_logFolder);

                string filePath = Path.Combine(_logFolder, $"{DateTime.Now:yyyy-MM-dd}.log");
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] | {direction} | {rawData} | {displayValue}{Environment.NewLine}";

                await _writeLock.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(filePath, line, Encoding.UTF8);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileLog] 写入失败: {ex.Message}");
            }
        }
    }
}
