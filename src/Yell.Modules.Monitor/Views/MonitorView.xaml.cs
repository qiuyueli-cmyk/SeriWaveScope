using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ScottPlot.Plottable;
using Yell.Core.Models;
using Yell.Modules.Monitor.ViewModels;

namespace Yell.Modules.Monitor.Views
{
    /// <summary>
    /// 监控页面。ScottPlot 的缓冲、定时刷新和主题上色都属于 View 的职责。
    /// </summary>
    public partial class MonitorView : UserControl
    {
        private const int BufferSize = 1000;

        private readonly MonitorViewModel _viewModel;
        private readonly double[] _buffer = new double[BufferSize];
        private readonly DispatcherTimer _renderTimer;
        private SignalPlot? _signalPlot;
        private int _sampleCount;
        private bool _isHooked;

        public MonitorView(MonitorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            InitPlot();

            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _renderTimer.Tick += (_, _) =>
            {
                if (IsVisible)
                {
                    RealTimePlot.Refresh();
                }
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void InitPlot()
        {
            _signalPlot = RealTimePlot.Plot.AddSignal(_buffer);
            _signalPlot.IsVisible = false;
            RealTimePlot.Plot.SetAxisLimits(0, 99, 0, 50);
            ApplyPlotTheme(_viewModel.CurrentTheme);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isHooked)
            {
                _viewModel.SampleReceived += OnSampleReceived;
                _viewModel.ClearRequested += OnClearRequested;
                _viewModel.ThemeChanged += ApplyPlotTheme;
                _viewModel.Logs.CollectionChanged += OnLogsChanged;
                _isHooked = true;
            }

            _renderTimer.Start();

            // 页面不可见期间可能已切换主题，重新加载时补一次上色
            ApplyPlotTheme(_viewModel.CurrentTheme);
            RealTimePlot.Refresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _renderTimer.Stop();

            if (_isHooked)
            {
                _viewModel.SampleReceived -= OnSampleReceived;
                _viewModel.ClearRequested -= OnClearRequested;
                _viewModel.ThemeChanged -= ApplyPlotTheme;
                _viewModel.Logs.CollectionChanged -= OnLogsChanged;
                _isHooked = false;
            }
        }

        private void OnSampleReceived(double value)
        {
            if (_sampleCount < BufferSize)
            {
                // 未满时从左侧开始生长，避免开头出现一条长长的 0
                _buffer[_sampleCount] = value;
                _sampleCount++;
            }
            else
            {
                // 满窗口后：最旧的样本移出，新样本追加到末尾
                Array.Copy(_buffer, 1, _buffer, 0, BufferSize - 1);
                _buffer[BufferSize - 1] = value;
            }

            if (_signalPlot is null)
            {
                return;
            }

            _signalPlot.IsVisible = true;
            _signalPlot.MaxRenderIndex = _sampleCount - 1;

            double min = _buffer[0];
            double max = _buffer[0];
            for (int i = 1; i < _sampleCount; i++)
            {
                if (_buffer[i] < min) min = _buffer[i];
                if (_buffer[i] > max) max = _buffer[i];
            }

            double padding = Math.Max((max - min) * 0.15, 1.0);
            RealTimePlot.Plot.SetAxisLimits(
                0,
                Math.Max(99, _sampleCount - 1),
                min - padding,
                max + padding);
        }

        private void OnClearRequested()
        {
            _sampleCount = 0;
            Array.Clear(_buffer, 0, BufferSize);

            if (_signalPlot is not null)
            {
                _signalPlot.IsVisible = false;
                _signalPlot.MaxRenderIndex = 0;
            }

            RealTimePlot.Plot.SetAxisLimits(0, 99, 0, 50);
            RealTimePlot.Refresh();
        }

        private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add ||
                AutoScrollCheckBox.IsChecked != true)
            {
                return;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                if (LogListBox.Items.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[^1]);
                }
            }));
        }

        private void ApplyPlotTheme(AppTheme theme)
        {
            bool dark = theme == AppTheme.Dark;

            System.Drawing.Color figure = dark
                ? System.Drawing.Color.FromArgb(23, 32, 51)
                : System.Drawing.Color.White;

            System.Drawing.Color grid = dark
                ? System.Drawing.Color.FromArgb(34, 49, 74)
                : System.Drawing.Color.FromArgb(226, 232, 240);

            System.Drawing.Color tick = dark
                ? System.Drawing.Color.FromArgb(148, 163, 184)
                : System.Drawing.Color.FromArgb(71, 85, 105);

            System.Drawing.Color title = dark
                ? System.Drawing.Color.FromArgb(230, 237, 247)
                : System.Drawing.Color.FromArgb(15, 23, 42);

            var plot = RealTimePlot.Plot;
            plot.Title("实时采样波形");
            plot.XAxis.Label("采样点");
            plot.YAxis.Label("测量值");
            plot.Style(figure, figure, grid, tick, tick, title);

            if (_signalPlot is not null)
            {
                _signalPlot.Color = dark
                    ? System.Drawing.Color.FromArgb(56, 189, 248)
                    : System.Drawing.Color.FromArgb(2, 132, 199);
                _signalPlot.LineWidth = 1.8;
            }

            RealTimePlot.Refresh();
        }
    }
}
