using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using Yell.Core.Constants;
using Yell.Core.Contracts;
using Yell.Modules.Acquisition.Services;
using Yell.Modules.Acquisition.ViewModels;
using Yell.Modules.Acquisition.Views;

namespace Yell.Modules.Acquisition
{
    /// <summary>
    /// 采集模块只负责硬件相关能力：
    /// 串口发现、串口服务、硬件参数面板。
    /// </summary>
    public sealed class AcquisitionModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<PortDiscoveryService>();
            containerRegistry.RegisterSingleton<ISerialPortService, SerialPortService>();
            containerRegistry.RegisterSingleton<AcquisitionViewModel>();
            containerRegistry.Register<AcquisitionView>();
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            IRegionManager regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion(RegionNames.HardwareRegion, typeof(AcquisitionView));
        }
    }
}
