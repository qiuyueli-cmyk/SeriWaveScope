using Prism.Ioc;
using Prism.Modularity;
using Yell.Core.Constants;
using Yell.Modules.Monitor.ViewModels;
using Yell.Modules.Monitor.Views;

namespace Yell.Modules.Monitor
{
    /// <summary>
    /// 监控模块：实时统计、波形和报文监控。
    /// </summary>
    public sealed class MonitorModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<MonitorViewModel>();
            containerRegistry.RegisterForNavigation<MonitorView>(ViewNames.Monitor);
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}
