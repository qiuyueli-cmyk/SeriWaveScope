using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Yell.Core.Contracts;
using Yell.Core.Events;
using Yell.Core.Models;
using Yell.Modules.History.Models;

namespace Yell.Modules.History.ViewModels
{
    /// <summary>
    /// 历史查询页面 ViewModel：
    /// 时间范围校验 → SQLite 查询 → 生成波形点 → 可导出 CSV。
    /// </summary>
    public sealed class HistoryViewModel : BindableBase
    {
        private const int QueryLimit = 5000;

        private readonly IHistoryRepository _historyRepository;
        private readonly ICsvExportService _csvExportService;
        private readonly IUserInteractionService _userInteractionService;

        private DateTime? _startDate = DateTime.Today;
        private DateTime? _endDate = DateTime.Today;
        private string _startTimeText = "00:00:00";
        private string _endTimeText = "23:59:59";
        private string _statusText = "选择时间范围后查询历史数据。";
        private bool _isBusy;
        private AppTheme _currentTheme;

        public HistoryViewModel(
            IHistoryRepository historyRepository,
            ICsvExportService csvExportService,
            IUserInteractionService userInteractionService,
            IEventAggregator eventAggregator,
            IThemeService themeService)
        {
            _historyRepository = historyRepository;
            _csvExportService = csvExportService;
            _userInteractionService = userInteractionService;
            _currentTheme = themeService.CurrentTheme;

            QueryCommand = new AsyncDelegateCommand(QueryAsync, () => !IsBusy);
            ExportCommand = new AsyncDelegateCommand(ExportAsync, () => !IsBusy);

            eventAggregator
                .GetEvent<ThemeChangedEvent>()
                .Subscribe(OnThemeChanged, ThreadOption.UIThread);
        }

        public ObservableCollection<HistoryRecord> Records { get; } = new();

        public AsyncDelegateCommand QueryCommand { get; }

        public AsyncDelegateCommand ExportCommand { get; }

        public string DatabasePath => _historyRepository.DatabasePath;

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public string StartTimeText
        {
            get => _startTimeText;
            set => SetProperty(ref _startTimeText, value);
        }

        public string EndTimeText
        {
            get => _endTimeText;
            set => SetProperty(ref _endTimeText, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaisePropertyChanged(nameof(IsIdle));
                    QueryCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsIdle => !IsBusy;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set => SetProperty(ref _currentTheme, value);
        }

        public event Action<IReadOnlyList<HistoryPoint>>? QueryCompleted;

        public event Action<AppTheme>? ThemeChanged;

        private async Task QueryAsync()
        {
            if (!TryBuildTimeRange(out DateTime from, out DateTime to))
            {
                return;
            }

            IsBusy = true;
            StatusText = "正在查询 SQLite 历史数据...";

            try
            {
                List<HistoryRecord> records = await _historyRepository.QueryAsync(from, to, QueryLimit);

                List<HistoryPoint> points = records
                    .Where(r => r.Direction == "RX" && r.NumericValue.HasValue)
                    .OrderBy(r => r.Timestamp)
                    .Select(r => new HistoryPoint(r.Timestamp, r.NumericValue!.Value))
                    .ToList();

                var result = new HistoryQueryResult
                {
                    Records = records,
                    Points = points,
                    WasLimited = records.Count >= QueryLimit
                };

                Records.Clear();
                foreach (HistoryRecord record in result.Records)
                {
                    Records.Add(record);
                }

                QueryCompleted?.Invoke(result.Points);

                if (result.Records.Count == 0)
                {
                    StatusText = "当前时间段没有历史数据。";
                }
                else if (result.WasLimited)
                {
                    StatusText = $"已显示最近 {result.Records.Count:N0} 条（达到上限），请缩小时间范围。波形 {result.Points.Count:N0} 个点。";
                }
                else
                {
                    StatusText = $"查询到 {result.Records.Count:N0} 条记录，波形 {result.Points.Count:N0} 个点。";
                }
            }
            catch (Exception ex)
            {
                StatusText = "查询失败。";
                _userInteractionService.ShowError($"历史数据查询失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportAsync()
        {
            if (Records.Count == 0)
            {
                _userInteractionService.ShowWarning("当前没有可导出的历史数据，请先查询。");
                return;
            }

            string? filePath = _userInteractionService.PickSaveFile(
                "CSV 文件 (*.csv)|*.csv",
                $"历史数据_{DateTime.Now:yyyyMMdd_HHmmss}",
                ".csv");

            if (filePath is null)
            {
                return;
            }

            try
            {
                await _csvExportService.ExportHistoryAsync(filePath, Records);
                _userInteractionService.ShowInfo($"已导出 {Records.Count:N0} 条历史记录。");
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"导出失败：{ex.Message}");
            }
        }

        private bool TryBuildTimeRange(out DateTime from, out DateTime to)
        {
            from = default;
            to = default;

            if (StartDate is null || EndDate is null)
            {
                _userInteractionService.ShowWarning("请先选择开始日期和结束日期。");
                return false;
            }

            if (!TimeSpan.TryParse(StartTimeText, out TimeSpan startTime) ||
                !TimeSpan.TryParse(EndTimeText, out TimeSpan endTime))
            {
                _userInteractionService.ShowWarning("时间格式不正确，请使用 HH:mm:ss，例如 08:30:00。");
                return false;
            }

            if (startTime < TimeSpan.Zero || startTime >= TimeSpan.FromDays(1) ||
                endTime < TimeSpan.Zero || endTime >= TimeSpan.FromDays(1))
            {
                _userInteractionService.ShowWarning("时间必须在 00:00:00 到 23:59:59 之间。");
                return false;
            }

            from = StartDate.Value.Date + startTime;
            to = EndDate.Value.Date + endTime;

            if (from > to)
            {
                _userInteractionService.ShowWarning("开始时间不能晚于结束时间。");
                return false;
            }

            return true;
        }

        private void OnThemeChanged(AppTheme theme)
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
    }
}
