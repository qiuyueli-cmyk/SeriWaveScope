namespace Yell.Core.Models
{
    /// <summary>
    /// 历史波形上的一个点，Data 层查询后交给 View 绘制。
    /// </summary>
    public sealed record HistoryPoint(DateTime Timestamp, double Value);
}
