namespace Yell.Modules.Acquisition.Models
{
    /// <summary>
    /// 一个本机串口，以及它的设备描述和是否为蓝牙虚拟串口。
    /// </summary>
    public sealed class PortInfo
    {
        public string PortName { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public bool IsBluetooth { get; init; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Description)
                ? PortName
                : $"{PortName} · {Description}";
    }
}
