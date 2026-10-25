namespace Yell.Core.Protocol
{
    /// <summary>
    /// 业务协议解析：帧头 AA，第 3 个字节为原始采样值，
    /// 物理量 = 原始值 / 10.0。
    /// </summary>
    public static class MeasurementParser
    {
        public static bool TryParse(byte[]? frame, out double value)
        {
            value = 0;

            if (frame is null || frame.Length < 3 || frame[0] != 0xAA)
            {
                return false;
            }

            int payloadLength = frame[1];
            if (payloadLength < 1 || frame.Length < payloadLength + 2)
            {
                return false;
            }

            value = frame[2] / 10.0;
            return true;
        }
    }
}
