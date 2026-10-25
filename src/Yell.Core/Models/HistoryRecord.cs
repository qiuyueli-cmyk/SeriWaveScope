namespace Yell.Core.Models
{
    /// <summary>
    /// SQLite 历史表的一行记录。
    /// </summary>
    public sealed class HistoryRecord
    {
        public long Id { get; init; }

        public DateTime Timestamp { get; init; }

        public string Direction { get; init; } = string.Empty;

        public string RawData { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;

        /// <summary>
        /// 解析成功时的物理量；TX、系统消息等为 null。
        /// </summary>
        public double? NumericValue { get; init; }

        public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}
