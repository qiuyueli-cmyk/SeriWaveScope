namespace Yell.Core.Protocol
{
    /// <summary>
    /// 协议封包：AA + 负载长度 + 负载。
    /// </summary>
    public static class FrameBuilder
    {
        public static byte[] Build(byte[] payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (payload.Length > byte.MaxValue)
            {
                throw new ArgumentException("单次负载不能超过 255 字节。", nameof(payload));
            }

            var frame = new byte[payload.Length + 2];
            frame[0] = 0xAA;
            frame[1] = (byte)payload.Length;
            Array.Copy(payload, 0, frame, 2, payload.Length);
            return frame;
        }

        public static byte[] Build(byte payload) => Build(new[] { payload });
    }
}
