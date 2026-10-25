using System.IO.Ports;

namespace Yell.Core.Models
{
    /// <summary>
    /// 打开串口所需的完整参数。
    /// </summary>
    public sealed class SerialSettings
    {
        public string PortName { get; init; } = string.Empty;

        public int BaudRate { get; init; } = 115200;

        public Parity Parity { get; init; } = Parity.None;

        public int DataBits { get; init; } = 8;

        public StopBits StopBits { get; init; } = StopBits.One;

        public string Description =>
            $"{PortName} · {BaudRate} · {DataBits}-{Parity}-{StopBits}";
    }
}
