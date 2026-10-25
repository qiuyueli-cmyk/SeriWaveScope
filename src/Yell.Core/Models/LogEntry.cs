namespace Yell.Core.Models
{
    /// <summary>
    /// 一条通讯日志：时间 + 方向 + 原始报文 + 解析值。
    /// 只描述数据，不包含任何界面逻辑。
    /// </summary>
    public sealed record LogEntry(
        string Time,
        string Direction,
        string RawData,
        string Value);
}
