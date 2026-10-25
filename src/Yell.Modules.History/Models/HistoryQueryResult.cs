using Yell.Core.Models;

namespace Yell.Modules.History.Models
{
    /// <summary>
    /// 一次历史查询的完整结果：列表 + 波形点 + 是否触及上限。
    /// </summary>
    public sealed class HistoryQueryResult
    {
        public IReadOnlyList<HistoryRecord> Records { get; init; } = Array.Empty<HistoryRecord>();

        public IReadOnlyList<HistoryPoint> Points { get; init; } = Array.Empty<HistoryPoint>();

        public bool WasLimited { get; init; }
    }
}
