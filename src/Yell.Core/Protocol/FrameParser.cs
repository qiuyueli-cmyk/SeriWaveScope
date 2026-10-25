namespace Yell.Core.Protocol
{
    /// <summary>
    /// 断包 / 粘包解析器。
    /// Push 一次可能返回 0 个、1 个或多个完整帧。
    /// </summary>
    public sealed class FrameParser
    {
        private const int MaxBufferSize = 4096;
        private readonly List<byte> _buffer = new();
        private readonly object _sync = new();

        public IReadOnlyList<byte[]> Push(byte[] data)
        {
            var frames = new List<byte[]>();
            if (data is null || data.Length == 0)
            {
                return frames;
            }

            lock (_sync)
            {
                _buffer.AddRange(data);

                if (_buffer.Count > MaxBufferSize)
                {
                    _buffer.Clear();
                    return frames;
                }

                while (true)
                {
                    int headerIndex = _buffer.IndexOf(0xAA);
                    if (headerIndex < 0)
                    {
                        _buffer.Clear();
                        break;
                    }

                    if (headerIndex > 0)
                    {
                        _buffer.RemoveRange(0, headerIndex);
                    }

                    if (_buffer.Count < 2)
                    {
                        break;
                    }

                    int payloadLength = _buffer[1];
                    int frameLength = payloadLength + 2;

                    if (frameLength > MaxBufferSize)
                    {
                        _buffer.RemoveAt(0);
                        continue;
                    }

                    if (_buffer.Count < frameLength)
                    {
                        break;
                    }

                    frames.Add(_buffer.GetRange(0, frameLength).ToArray());
                    _buffer.RemoveRange(0, frameLength);
                }
            }

            return frames;
        }

        public void Reset()
        {
            lock (_sync)
            {
                _buffer.Clear();
            }
        }
    }
}
