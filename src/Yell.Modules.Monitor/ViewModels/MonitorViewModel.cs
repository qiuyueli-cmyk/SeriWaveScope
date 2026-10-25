using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Yell.Core.Contracts;
using Yell.Core.Events;
using Yell.Core.Models;
using Yell.Core.Protocol;
using Yell.Modules.Monitor.Models;

namespace Yell.Modules.Monitor.ViewModels
{
    /// <summary>
    /// 监控模块 ViewModel。
    /// 订阅采集模块发布的事件，负责解析、统计、日志、落盘和导出；
    /// 波形绘制交给 View（ScottPlot 属于 UI 技术细节）。
    /// </summary>
    public sealed class MonitorViewModel : BindableBase
    {
        private const int MaxUiLogCount = 1000;

        private readonly IEventAggregator _eventAggregator;
        private readonly IHistoryRepository _historyRepository;
        private readonly ILogFileService _logFileService;
        private readonly ICsvExportService _csvExportService;
        private readonly IUserInteractionService _userInteractionService;
        private readonly MonitorStatistics _statistics = new();
        private readonly List<LogEntry> _allLogs = new();
        private readonly object _cacheLock = new();

        private AppTheme _currentTheme;
        private string _statusText = "等待数据...";

        public MonitorViewModel(
            IEventAggregator eventAggregator,
            IHistoryRepository historyRepository,
            ILogFileService logFileService,
            ICsvExportService csvExportService,
            IUserInteractionService userInteractionService,
            IThemeService themeService)
        {
            _eventAggregator = eventAggregator;
            _historyRepository = historyRepository;
            _logFileService = logFileService;
            _csvExportService = csvExportService;
            _userInteractionService = userInteractionService;
            _currentTheme = themeService.CurrentTheme;

            ClearCommand = new DelegateCommand(ClearAll);
            ExportCommand = new AsyncDelegateCommand(ExportAsync);

            _eventAggregator.GetEvent<RawPacketReceivedEvent>().Subscribe(OnPacketReceived);
            _eventAggregator.GetEvent<PacketSentEvent>().Subscribe(OnPacketSent);
            _eventAggregator.GetEvent<SystemMessageEvent>().Subscribe(OnSystemMessage);
            _eventAggregator
                .GetEvent<ThemeChangedEvent>()
                .Subscribe(OnThemeChanged, ThreadOption.UIThread);
        }

        public ObservableCollection<LogEntry> Logs { get; } = new();

        public DelegateCommand ClearCommand { get; }

        public AsyncDelegateCommand ExportCommand { get; }

        public string SampleCountText => _statistics.Count.ToString("N0");

        public string CurrentValueText =>
            _statistics.Count == 0 ? "--" : _statistics.Current.ToString("F1");

        public string MinValueText =>
            _statistics.Count == 0 ? "--" : _statistics.Min.ToString("F1");

        public string MaxValueText =>
            _statistics.Count == 0 ? "--" : _statistics.Max.ToString("F1");

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set => SetProperty(ref _currentTheme, value);
        }

        /// <summary>
        /// 通知 View：收到了一个新的有效物理量。
        /// </summary>
        public event Action<double>? SampleReceived;

        /// <summary>
        /// 通知 View：清空波形缓冲。
        /// </summary>
        public event Action? ClearRequested;

        /// <summary>
        /// 通知 View：主题变化，需要重新给 ScottPlot 上色。
        /// </summary>
        public event Action<AppTheme>? ThemeChanged;

        private void OnPacketReceived(byte[] frame)
        {
            string hex = HexConverter.BytesToHexString(frame);
            bool parsed = MeasurementParser.TryParse(frame, out double value);

            if (parsed)
            {
                UpdateStatistics(value);
            }

            AddLog("RX", hex, parsed ? value.ToString("F1") : "-", parsed ? value : null);
        }

        private void OnPacketSent(byte[] frame)
        {
            AddLog("TX", HexConverter.BytesToHexString(frame), "-", null);
        }

        private void OnSystemMessage(string message)
        {
            AddLog("系统", message, "-", null);
        }

        private void AddLog(string direction, string rawData, string value, double? numericValue)
        {
            DateTime now = DateTime.Now;
            var entry = new LogEntry(now.ToString("HH:mm:ss.fff"), direction, rawData, value);

            lock (_cacheLock)
            {
                _allLogs.Add(entry);
            }

            _ = _logFileService.WriteAsync(direction, rawData, value);

            if (numericValue.HasValue)
            {
                _ = _historyRepository.InsertAsync(new HistoryRecord
                {
                    Timestamp = now,
                    Direction = direction,
                    RawData = rawData,
                    Value = value,
                    NumericValue = numericValue
                });
            }

            RunOnUiThread(() =>
            {
                if (Logs.Count >= MaxUiLogCount)
                {
                    Logs.RemoveAt(0);
                }
                Logs.Add(entry);
            });
        }

        private void UpdateStatistics(double value)
        {
            _statistics.Add(value);
            string status = $"已接收 {_statistics.Count:N0} 个样本 · 最后更新 {DateTime.Now:HH:mm:ss}";

            RunOnUiThread(() =>
            {
                RaisePropertyChanged(nameof(SampleCountText));
                RaisePropertyChanged(nameof(CurrentValueText));
                RaisePropertyChanged(nameof(MinValueText));
                RaisePropertyChanged(nameof(MaxValueText));
                StatusText = status;
                SampleReceived?.Invoke(value);
            });
        }

        private void OnThemeChanged(AppTheme theme)
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }

        private void ClearAll()
        {
            lock (_cacheLock)
            {
                _allLogs.Clear();
            }

            _statistics.Reset();
            Logs.Clear();
            RaisePropertyChanged(nameof(SampleCountText));
            RaisePropertyChanged(nameof(CurrentValueText));
            RaisePropertyChanged(nameof(MinValueText));
            RaisePropertyChanged(nameof(MaxValueText));
            StatusText = "缓冲区已清空，等待新的数据...";

            ClearRequested?.Invoke();
        }

        private async Task ExportAsync()
        {
            List<LogEntry> snapshot;
            lock (_cacheLock)
            {
                snapshot = _allLogs.ToList();
            }

            if (snapshot.Count == 0)
            {
                _userInteractionService.ShowWarning("当前没有可导出的数据。");
                return;
            }

            string? filePath = _userInteractionService.PickSaveFile(
                "CSV 文件 (*.csv)|*.csv",
                $"测试报告_{DateTime.Now:yyyyMMdd_HHmmss}",
                ".csv");

            if (filePath is null)
            {
                return;
            }

            try
            {
                await _csvExportService.ExportLogsAsync(filePath, snapshot);
                _userInteractionService.ShowInfo($"已导出 {snapshot.Count:N0} 条记录。");
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"导出失败：{ex.Message}");
            }
        }

        private static void RunOnUiThread(Action action)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(DispatcherPriority.Background, action);
            }
        }
    }
}
