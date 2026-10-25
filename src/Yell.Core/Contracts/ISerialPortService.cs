using Yell.Core.Models;

namespace Yell.Core.Contracts
{
    /// <summary>
    /// 串口能力的抽象。View / ViewModel 只依赖接口，
    /// 真正使用 SerialPort 的代码全部沉在采集模块内部。
    /// </summary>
    public interface ISerialPortService : IDisposable
    {
        bool IsOpen { get; }

        string? PortName { get; }

        void Open(SerialSettings settings);

        void Close();

        Task SendAsync(byte[] frame, CancellationToken cancellationToken = default);
    }
}
