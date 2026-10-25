using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using Yell.Modules.Acquisition.Models;

namespace Yell.Modules.Acquisition.Services
{
    /// <summary>
    /// 通过 WMI 查询串口的设备描述和 PNP 标识。
    /// 蓝牙虚拟串口的 PNPDeviceID 以 BTHENUM 开头，
    /// 界面上默认隐藏它们，避免把虚拟口当成物理设备。
    /// </summary>
    public sealed class PortDiscoveryService
    {
        public IReadOnlyList<PortInfo> GetPorts()
        {
            var devices = new Dictionary<string, (string Description, bool IsBluetooth)>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

                foreach (ManagementBaseObject device in searcher.Get())
                {
                    string name = device["Name"] as string ?? string.Empty;
                    string pnpId = device["PNPDeviceID"] as string ?? string.Empty;

                    int start = name.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
                    int end = start < 0 ? -1 : name.IndexOf(')', start);
                    if (start < 0 || end < 0)
                    {
                        continue;
                    }

                    string portName = name.Substring(start + 1, end - start - 1).Trim();
                    string description = name.Substring(0, start).Trim();

                    bool isBluetooth =
                        pnpId.StartsWith("BTHENUM", StringComparison.OrdinalIgnoreCase) ||
                        description.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                        description.Contains("蓝牙");

                    devices[portName] = (description, isBluetooth);
                }
            }
            catch (Exception ex)
            {
                // WMI 不可用时退化为只显示端口名，不影响主流程
                Debug.WriteLine($"[Ports] WMI 查询失败: {ex.Message}");
            }

            var result = new List<PortInfo>();
            foreach (string portName in SerialPort.GetPortNames())
            {
                devices.TryGetValue(portName, out var info);
                result.Add(new PortInfo
                {
                    PortName = portName,
                    Description = string.IsNullOrWhiteSpace(info.Description) ? "串口设备" : info.Description,
                    IsBluetooth = info.IsBluetooth
                });
            }

            return result
                .OrderBy(p => GetPortNumber(p.PortName))
                .ToList();
        }

        private static int GetPortNumber(string portName)
        {
            if (portName.Length > 3 &&
                int.TryParse(portName.AsSpan(3), out int number))
            {
                return number;
            }

            return int.MaxValue;
        }
    }
}
