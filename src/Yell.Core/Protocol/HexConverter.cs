using System.Globalization;

namespace Yell.Core.Protocol
{
    /// <summary>
    /// HEX 字符串与字节数组互转。
    /// </summary>
    public static class HexConverter
    {
        public static byte[] HexStringToBytes(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return Array.Empty<byte>();
            }

            string clean = hex
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", string.Empty);

            if (clean.Length % 2 != 0)
            {
                throw new FormatException("HEX 字符个数必须为偶数。");
            }

            var bytes = new byte[clean.Length / 2];
            for (int i = 0; i < clean.Length; i += 2)
            {
                if (!byte.TryParse(clean.AsSpan(i, 2), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out bytes[i / 2]))
                {
                    throw new FormatException($"存在非法 HEX 字符：{clean.Substring(i, 2)}。");
                }
            }

            return bytes;
        }

        public static string BytesToHexString(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(" ", bytes.Select(b => b.ToString("X2")));
        }
    }
}
