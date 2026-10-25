using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Yell.Core.Contracts;
using Yell.Infrastructure.Export;
using Yell.Infrastructure.Logging;
using Yell.Infrastructure.Persistence;
using Yell.Infrastructure.Services;
using Yell.Infrastructure.Settings;
using Yell.Modules.Acquisition;
using Yell.Modules.History;
using Yell.Modules.Monitor;
using Yell.Services;
using Yell.ViewModels;
using Yell.Views;

namespace Yell
{
    /// <summary>
    /// 组合根（Composition Root）：
    /// Shell 只注册基础设施和模块目录，业务服务由各模块自己注册。
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Infrastructure：接口的 WPF / SQLite 实现
            containerRegistry.RegisterSingleton<IHistoryRepository, SqliteHistoryRepository>();
            containerRegistry.RegisterSingleton<ILogFileService, FileLogService>();
            containerRegistry.RegisterSingleton<ICsvExportService, CsvExportService>();
            containerRegistry.RegisterSingleton<IAppSettingsService, JsonAppSettingsService>();
            containerRegistry.RegisterSingleton<IUserInteractionService, WpfUserInteractionService>();

            // Shell 自身
            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
            containerRegistry.RegisterSingleton<MainWindowViewModel>();
            containerRegistry.RegisterSingleton<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<AcquisitionModule>();
            moduleCatalog.AddModule<MonitorModule>();
            moduleCatalog.AddModule<HistoryModule>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // 应用上次保存的主题（默认深色）
            IThemeService themeService = Container.Resolve<IThemeService>();
            themeService.ApplyTheme(themeService.CurrentTheme);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Container.Resolve<ISerialPortService>().Dispose();
            base.OnExit(e);
        }
    }
}
