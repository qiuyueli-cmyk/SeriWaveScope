using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Yell.Core.Constants;
using Yell.Core.Contracts;
using Yell.Core.Events;
using Yell.Core.Models;

namespace Yell.ViewModels
{
    /// <summary>
    /// Shell 的 ViewModel：负责侧边栏导航、主题切换和全局连接状态。
    /// </summary>
    public sealed class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IThemeService _themeService;

        private string _currentView = ViewNames.Monitor;
        private string _currentTitle = "实时监控";
        private string _currentSubtitle = "串口采集 · 实时波形 · 报文监控";
        private bool _isConnected;
        private bool _isDarkTheme;
        private bool _initialized;

        public MainWindowViewModel(
            IRegionManager regionManager,
            IThemeService themeService,
            IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _themeService = themeService;
            _isDarkTheme = themeService.CurrentTheme == AppTheme.Dark;

            NavigateCommand = new DelegateCommand<string>(Navigate);
            ToggleThemeCommand = new DelegateCommand(() => _themeService.ToggleTheme());

            eventAggregator
                .GetEvent<ThemeChangedEvent>()
                .Subscribe(OnThemeChanged, ThreadOption.UIThread);

            eventAggregator
                .GetEvent<ConnectionStateChangedEvent>()
                .Subscribe(OnConnectionChanged, ThreadOption.UIThread);
        }

        public DelegateCommand<string> NavigateCommand { get; }

        public DelegateCommand ToggleThemeCommand { get; }

        public bool IsMonitorActive => _currentView == ViewNames.Monitor;

        public bool IsHistoryActive => _currentView == ViewNames.History;

        public string CurrentTitle
        {
            get => _currentTitle;
            private set => SetProperty(ref _currentTitle, value);
        }

        public string CurrentSubtitle
        {
            get => _currentSubtitle;
            private set => SetProperty(ref _currentSubtitle, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    RaisePropertyChanged(nameof(ConnectionText));
                }
            }
        }

        public string ConnectionText => IsConnected ? "串口已连接" : "串口未连接";

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            private set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    RaisePropertyChanged(nameof(ThemeButtonText));
                }
            }
        }

        public string ThemeButtonText => IsDarkTheme ? "切换到浅色" : "切换到深色";

        /// <summary>
        /// 窗口 Loaded 后调用：此时区域已经创建，可以安全导航。
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Navigate(ViewNames.Monitor);
        }

        private void Navigate(string? viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return;
            }

            _regionManager.RequestNavigate(RegionNames.MainRegion, viewName);

            _currentView = viewName;
            RaisePropertyChanged(nameof(IsMonitorActive));
            RaisePropertyChanged(nameof(IsHistoryActive));

            if (viewName == ViewNames.History)
            {
                CurrentTitle = "历史数据";
                CurrentSubtitle = "SQLite 历史记录 · 时间范围查询 · CSV 导出";
            }
            else
            {
                CurrentTitle = "实时监控";
                CurrentSubtitle = "串口采集 · 实时波形 · 报文监控";
            }
        }

        private void OnThemeChanged(AppTheme theme)
        {
            IsDarkTheme = theme == AppTheme.Dark;
        }

        private void OnConnectionChanged(bool connected)
        {
            IsConnected = connected;
        }
    }
}
