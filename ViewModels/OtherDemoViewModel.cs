﻿using Khsw.Instrument.Demo.Infrastructures;
using Khsw.Instrument.Demo.Views.Base;

namespace Khsw.Instrument.Demo.ViewModels
{
    public class OtherDemoViewModel : BindableBase
    {
        #region Fields
        private readonly IModuleManager _moduleManager;
        private readonly IRegionManager _regionManager;
        private readonly IContainerExtension _container;

        private string _ipAddress = "192.168.1.2";
        #endregion

        #region Properties

        public string IpAddress
        {
            get { return _ipAddress; }
            set { SetProperty(ref _ipAddress, value); }
        }

        #endregion

        #region Commands

        private DelegateCommand _loginLoadingCommand;
        public DelegateCommand LoginLoadingCommand =>
            _loginLoadingCommand ?? (_loginLoadingCommand = new DelegateCommand(ExecuteLoginLoadingCommand));


        #endregion

        public OtherDemoViewModel(IContainerExtension container, IModuleManager moduleManager, IRegionManager regionManager)
        {
            _moduleManager = moduleManager;
            _regionManager = regionManager;
            _container = container;
        }

        private void ExecuteLoginLoadingCommand()
        {
            var parameters = new NavigationParameters();
            parameters.Add("ipAddress", _ipAddress);

            var _region = _container.Resolve<IRegionManager>();
            var views = _region.Regions.Select(x => x.Name);
            _regionManager.RequestNavigate(RegionNames.InstrumentManangeRegion, nameof(InstrumentManangeView), result =>
            {
                if (result.Success == false)
                {
                    // 输出导航错误的原因
                    var error = result.Exception;
                    Console.WriteLine(error?.Message);
                }
            }, parameters);
        }
    }
}
