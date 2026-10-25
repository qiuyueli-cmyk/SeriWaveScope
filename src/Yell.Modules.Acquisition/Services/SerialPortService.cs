using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using Prism.Events;
using Yell.Core.Contracts;
using Yell.Core.Events;
using Yell.Core.Models;
using Yell.Core.Protocol;
using Timer = System.Threading.Timer;

namespace Yell.Modules.Acquisition.Services
{
    /// <summary>
    /// 串口服务：负责打开/关闭、断包粘包解析、异步发送，
    /// 并通过 EventAggregator 把结果广播给其他模块。
    /// </summary>
    public sealed class SerialPortService : ISerialPortService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly FrameParser _frameParser = new();
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly Timer _parserResetTimer;
        private SerialPort? _port;
        private string? _portName;
        private bool _disposed;

        public SerialPortService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // 500ms 没有新字节就丢弃残包，避免半帧长期占用缓冲区
            _parserResetTimer = new Timer(
                _ => _frameParser.Reset(),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
        }

        public bool IsOpen => _port?.IsOpen == true;

        public string? PortName => _portName;

        public void Open(SerialSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            Close();

            var port = new SerialPort(
                settings.PortName,
                settings.BaudRate,
                settings.Parity,
                settings.DataBits,
                settings.StopBits)
            {
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };

            port.DataReceived += OnDataReceived;

            try
            {
                port.Open();
            }
            catch
            {
                port.DataReceived -= OnDataReceived;
                port.Dispose();
                throw;
            }

            _port = port;
            _portName = settings.PortName;
            _frameParser.Reset();

            _eventAggregator.GetEvent<ConnectionStateChangedEvent>().Publish(true);
            _eventAggregator.GetEvent<SystemMessageEvent>()
                .Publish($"串口已连接：{settings.Description}");
        }

        public void Close()
        {
            var port = _port;
            _port = null;
            _portName = null;
            _frameParser.Reset();

            if (port is null)
            {
                return;
            }

            port.DataReceived -= OnDataReceived;
            try
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Serial] 关闭异常: {ex.Message}");
            }
            finally
            {
                port.Dispose();
            }

            _eventAggregator.GetEvent<ConnectionStateChangedEvent>().Publish(false);
            _eventAggregator.GetEvent<SystemMessageEvent>().Publish("串口已断开。");
        }

        public async Task SendAsync(byte[] frame, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(frame);

            SerialPort port = _port ?? throw new InvalidOperationException("串口未连接。");

            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                if (!port.IsOpen)
                {
                    throw new InvalidOperationException("串口已断开，请重新连接。");
                }

                await port.BaseStream.WriteAsync(frame.AsMemory(), cancellationToken);
                await port.BaseStream.FlushAsync(cancellationToken);

                _eventAggregator.GetEvent<PacketSentEvent>().Publish(frame.ToArray());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new IOException($"串口写入失败：{ex.Message}", ex);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort? port = _port;
            if (port is null)
            {
                return;
            }

            try
            {
                _parserResetTimer.Change(500, Timeout.Infinite);

                int bytesToRead = port.BytesToRead;
                if (bytesToRead <= 0)
                {
                    return;
                }

                var buffer = new byte[bytesToRead];
                int read = port.Read(buffer, 0, bytesToRead);
                if (read < bytesToRead)
                {
                    Array.Resize(ref buffer, read);
                }

                IReadOnlyList<byte[]> frames = _frameParser.Push(buffer);
                foreach (byte[] frame in frames)
                {
                    _eventAggregator.GetEvent<RawPacketReceivedEvent>().Publish(frame);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Serial] 接收异常: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Close();
            _parserResetTimer.Dispose();
            _writeLock.Dispose();
        }
    }
}
