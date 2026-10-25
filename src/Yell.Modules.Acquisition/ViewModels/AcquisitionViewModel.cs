using System.Collections.ObjectModel;
using System.IO.Ports;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Yell.Core.Contracts;
using Yell.Core.Events;
using Yell.Core.Models;
using Yell.Core.Protocol;
using Yell.Modules.Acquisition.Models;
using Yell.Modules.Acquisition.Services;

namespace Yell.Modules.Acquisition.ViewModels
{
    /// <summary>
    /// 硬件面板的 ViewModel：
    /// 只操作 ISerialPortService 接口，不直接 new SerialPort。
    /// </summary>
    public sealed class AcquisitionViewModel : BindableBase
    {
        private readonly ISerialPortService _serialPortService;
        private readonly PortDiscoveryService _portDiscoveryService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IUserInteractionService _userInteractionService;

        private List<PortInfo> _allPorts = new();
        private SerialSettings? _lastSettings;
        private bool _isLoadingPorts;
        private bool _isConnected;
        private bool _isSimulating;
        private PortInfo? _selectedPort;
        private bool _showBluetoothPorts;
        private string _portHint = "正在扫描串口...";
        private string _connectionDetail = "选择串口后建立连接";
        private string _inputHexText = string.Empty;
        private int _selectedBaudRate = 115200;
        private int _selectedDataBits = 8;
        private string _selectedParityKey = "无校验";
        private string _selectedStopBitsKey = "1 位";

        public AcquisitionViewModel(
            ISerialPortService serialPortService,
            PortDiscoveryService portDiscoveryService,
            IEventAggregator eventAggregator,
            IUserInteractionService userInteractionService)
        {
            _serialPortService = serialPortService;
            _portDiscoveryService = portDiscoveryService;
            _eventAggregator = eventAggregator;
            _userInteractionService = userInteractionService;

            RefreshPortsCommand = new AsyncDelegateCommand(RefreshPortsAsync);
            ToggleConnectionCommand = new DelegateCommand(ToggleConnection);
            SendCommand = new AsyncDelegateCommand(SendAsync, () => IsConnected);
            SimulateCommand = new AsyncDelegateCommand(SimulateAsync, () => !IsSimulating);

            _eventAggregator
                .GetEvent<ConnectionStateChangedEvent>()
                .Subscribe(OnConnectionChanged, ThreadOption.UIThread);

            _ = RefreshPortsAsync();
        }

        public ObservableCollection<PortInfo> Ports { get; } = new();

        public List<int> BaudRateList { get; } = new()
        {
            4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600
        };

        public List<int> DataBitsList { get; } = new() { 5, 6, 7, 8 };

        public Dictionary<string, Parity> ParityOptions { get; } = new()
        {
            { "无校验", Parity.None },
            { "奇校验", Parity.Odd },
            { "偶校验", Parity.Even },
            { "标志位", Parity.Mark },
            { "空格位", Parity.Space }
        };

        public Dictionary<string, StopBits> StopBitsOptions { get; } = new()
        {
            { "1 位", StopBits.One },
            { "1.5 位", StopBits.OnePointFive },
            { "2 位", StopBits.Two }
        };

        public AsyncDelegateCommand RefreshPortsCommand { get; }

        public DelegateCommand ToggleConnectionCommand { get; }

        public AsyncDelegateCommand SendCommand { get; }

        public AsyncDelegateCommand SimulateCommand { get; }

        public PortInfo? SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public bool ShowBluetoothPorts
        {
            get => _showBluetoothPorts;
            set
            {
                if (SetProperty(ref _showBluetoothPorts, value))
                {
                    RebuildPortList();
                }
            }
        }

        public string PortHint
        {
            get => _portHint;
            private set => SetProperty(ref _portHint, value);
        }

        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        public int SelectedDataBits
        {
            get => _selectedDataBits;
            set => SetProperty(ref _selectedDataBits, value);
        }

        public string SelectedParityKey
        {
            get => _selectedParityKey;
            set => SetProperty(ref _selectedParityKey, value);
        }

        public string SelectedStopBitsKey
        {
            get => _selectedStopBitsKey;
            set => SetProperty(ref _selectedStopBitsKey, value);
        }

        public string InputHexText
        {
            get => _inputHexText;
            set => SetProperty(ref _inputHexText, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    RaisePropertyChanged(nameof(ConnectionStateText));
                    RaisePropertyChanged(nameof(ConnectButtonText));
                    SendCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string ConnectionStateText => IsConnected ? "已连接" : "未连接";

        public string ConnectionDetail
        {
            get => _connectionDetail;
            private set => SetProperty(ref _connectionDetail, value);
        }

        public string ConnectButtonText => IsConnected ? "断开连接" : "建立连接";

        public bool IsSimulating
        {
            get => _isSimulating;
            private set
            {
                if (SetProperty(ref _isSimulating, value))
                {
                    RaisePropertyChanged(nameof(SimulateButtonText));
                    SimulateCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SimulateButtonText => IsSimulating ? "正在生成模拟数据..." : "生成模拟波形";

        private async Task RefreshPortsAsync()
        {
            if (_isLoadingPorts)
            {
                return;
            }

            _isLoadingPorts = true;
            try
            {
                List<PortInfo> ports = (await Task.Run(_portDiscoveryService.GetPorts)).ToList();
                _allPorts = ports;
                RebuildPortList();
            }
            catch (Exception ex)
            {
                PortHint = "串口扫描失败。";
                _userInteractionService.ShowError($"串口扫描失败：{ex.Message}");
            }
            finally
            {
                _isLoadingPorts = false;
            }
        }

        private void RebuildPortList()
        {
            string? previousPortName = SelectedPort?.PortName;

            Ports.Clear();
            foreach (PortInfo port in _allPorts.Where(p => ShowBluetoothPorts || !p.IsBluetooth))
            {
                Ports.Add(port);
            }

            SelectedPort =
                Ports.FirstOrDefault(p => p.PortName == previousPortName) ??
                Ports.FirstOrDefault();

            int bluetoothCount = _allPorts.Count(p => p.IsBluetooth);

            if (_allPorts.Count == 0)
            {
                PortHint = "没有检测到串口设备，请检查 USB 转串口是否插入。";
            }
            else if (bluetoothCount == 0)
            {
                PortHint = $"共检测到 {_allPorts.Count} 个串口。";
            }
            else
            {
                PortHint = ShowBluetoothPorts
                    ? $"共 {_allPorts.Count} 个串口，其中 {bluetoothCount} 个为蓝牙虚拟串口。"
                    : $"已隐藏 {bluetoothCount} 个蓝牙虚拟串口，勾选后可显示。";
            }
        }

        private void ToggleConnection()
        {
            if (IsConnected)
            {
                _serialPortService.Close();
                return;
            }

            if (SelectedPort is null)
            {
                _userInteractionService.ShowWarning("请先选择一个串口。");
                return;
            }

            if (!ParityOptions.TryGetValue(SelectedParityKey, out Parity parity) ||
                !StopBitsOptions.TryGetValue(SelectedStopBitsKey, out StopBits stopBits))
            {
                _userInteractionService.ShowWarning("串口参数不正确，请重新选择。");
                return;
            }

            var settings = new SerialSettings
            {
                PortName = SelectedPort.PortName,
                BaudRate = SelectedBaudRate,
                DataBits = SelectedDataBits,
                Parity = parity,
                StopBits = stopBits
            };

            try
            {
                _serialPortService.Open(settings);
                _lastSettings = settings;
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"无法打开串口：{ex.Message}");
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            IsConnected = connected;
            ConnectionDetail = connected && _lastSettings is not null
                ? _lastSettings.Description
                : "选择串口后建立连接";
        }

        private async Task SendAsync()
        {
            if (!IsConnected)
            {
                _userInteractionService.ShowWarning("串口未连接，无法发送。");
                return;
            }

            try
            {
                byte[] payload = HexConverter.HexStringToBytes(InputHexText);
                if (payload.Length == 0)
                {
                    _userInteractionService.ShowWarning("请输入要发送的 HEX 负载。");
                    return;
                }

                byte[] frame = FrameBuilder.Build(payload);
                await _serialPortService.SendAsync(frame);
                InputHexText = string.Empty;
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"发送失败：{ex.Message}");
            }
        }

        private async Task SimulateAsync()
        {
            if (IsSimulating)
            {
                return;
            }

            IsSimulating = true;
            _eventAggregator.GetEvent<SystemMessageEvent>()
                .Publish("开始生成模拟波形，共 300 帧。");

            try
            {
                await Task.Run(async () =>
                {
                    for (int i = 0; i < 300; i++)
                    {
                        // 一帧只有一个字节，value * 10 必须落在 0~255，否则会被截断成平顶
                        double value = 21 + 4 * Math.Sin(i * 0.06);
                        byte raw = (byte)Math.Clamp(value * 10, 0, 255);

                        _eventAggregator
                            .GetEvent<RawPacketReceivedEvent>()
                            .Publish(FrameBuilder.Build(raw));

                        await Task.Delay(10);
                    }
                });

                _eventAggregator.GetEvent<SystemMessageEvent>()
                    .Publish("模拟波形推送完成。");
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"模拟数据生成失败：{ex.Message}");
            }
            finally
            {
                IsSimulating = false;
            }
        }
    }
}
