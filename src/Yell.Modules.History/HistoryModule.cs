using Prism.Ioc;
using Prism.Modularity;
using Yell.Core.Constants;
using Yell.Modules.History.ViewModels;
using Yell.Modules.History.Views;

namespace Yell.Modules.History
{
    /// <summary>
    /// 历史模块：SQLite 数据查询、历史波形与 CSV 导出。
    /// </summary>
    public sealed class HistoryModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<HistoryViewModel>();
            containerRegistry.RegisterForNavigation<HistoryView>(ViewNames.History);
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}
