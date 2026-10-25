using System.Windows;
using System.Windows.Controls;
using ScottPlot.Plottable;
using Yell.Core.Models;
using Yell.Modules.History.ViewModels;

namespace Yell.Modules.History.Views
{
    /// <summary>
    /// 历史页面。查询结果由 ViewModel 通过事件交给 View 绘制，
    /// View 不访问数据库，ViewModel 不接触 ScottPlot。
    /// </summary>
    public partial class HistoryView : UserControl
    {
        private readonly HistoryViewModel _viewModel;
        private ScatterPlot? _scatterPlot;
        private bool _isHooked;
        private bool _hasLoadedOnce;

        public HistoryView(HistoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            HistoryPlot.Plot.XAxis.DateTimeFormat(true);
            ApplyPlotTheme(_viewModel.CurrentTheme);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isHooked)
            {
                _viewModel.QueryCompleted += OnQueryCompleted;
                _viewModel.ThemeChanged += ApplyPlotTheme;
                _isHooked = true;
            }

            if (!_hasLoadedOnce)
            {
                _hasLoadedOnce = true;
                _viewModel.QueryCommand.Execute(null);
            }
            else
            {
                // 页面不可见期间可能已切换主题，重新加载时补一次上色
                ApplyPlotTheme(_viewModel.CurrentTheme);
                HistoryPlot.Refresh();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isHooked)
            {
                _viewModel.QueryCompleted -= OnQueryCompleted;
                _viewModel.ThemeChanged -= ApplyPlotTheme;
                _isHooked = false;
            }
        }

        private void OnQueryCompleted(IReadOnlyList<HistoryPoint> points)
        {
            var plot = HistoryPlot.Plot;
            plot.Clear();
            _scatterPlot = null;

            ApplyPlotTheme(_viewModel.CurrentTheme);

            if (points.Count == 0)
            {
                plot.Title("当前时间段没有可绘制的数据");
                HistoryPlot.Refresh();
                return;
            }

            double[] xs = points.Select(p => p.Timestamp.ToOADate()).ToArray();
            double[] ys = points.Select(p => p.Value).ToArray();

            _scatterPlot = plot.AddScatter(xs, ys);
            _scatterPlot.LineWidth = 1.6f;
            _scatterPlot.MarkerSize = 4;

            plot.Title($"历史波形 · {points.Count:N0} 个点");
            plot.SetAxisLimitsX(xs.Min(), xs.Max());
            plot.AxisAutoY();

            ApplyScatterColor(_viewModel.CurrentTheme);
            HistoryPlot.Refresh();
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

            var plot = HistoryPlot.Plot;
            plot.XAxis.Label("时间");
            plot.YAxis.Label("测量值");
            plot.Style(figure, figure, grid, tick, tick, title);

            ApplyScatterColor(theme);
            HistoryPlot.Refresh();
        }

        private void ApplyScatterColor(AppTheme theme)
        {
            if (_scatterPlot is null)
            {
                return;
            }

            _scatterPlot.Color = theme == AppTheme.Dark
                ? System.Drawing.Color.FromArgb(56, 189, 248)
                : System.Drawing.Color.FromArgb(2, 132, 199);
        }
    }
}
