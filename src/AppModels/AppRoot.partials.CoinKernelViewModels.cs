﻿using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner {
    public static partial class AppRoot {
        public class CoinKernelViewModels : ViewModelBase {
            public static CoinKernelViewModels Instance { get; private set; } = new CoinKernelViewModels();

            private readonly Dictionary<Guid, CoinKernelViewModel> _dicById = new Dictionary<Guid, CoinKernelViewModel>();
            private CoinKernelViewModels() {
                if (WpfUtil.IsInDesignMode) {
                    return;
                }
                VirtualRoot.BuildEventPath<ServerContextReInitedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: message => {
                        _dicById.Clear();
                        Init();
                    }, location: this.GetType());
                VirtualRoot.BuildEventPath<ServerContextReInitedEventHandledEvent>("刷新视图界面", LogEnum.DevConsole,
                    path: message => {
                        OnPropertyChanged(nameof(AllCoinKernels));
                    }, location: this.GetType());
                BuildEventPath<CoinKernelAddedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        var coinKernelVm = new CoinKernelViewModel(message.Source);
                        _dicById.Add(message.Source.GetId(), coinKernelVm);
                        OnPropertyChanged(nameof(AllCoinKernels));
                        var kernelVm = coinKernelVm.Kernel;
                        if (kernelVm != null) {
                            kernelVm.OnPropertyChanged(nameof(kernelVm.CoinKernels));
                            kernelVm.OnPropertyChanged(nameof(kernelVm.CoinVms));
                            kernelVm.OnPropertyChanged(nameof(kernelVm.SupportedCoinVms));
                            kernelVm.OnPropertyChanged(nameof(kernelVm.SupportedCoins));
                        }
                        VirtualRoot.RaiseEvent(new CoinKernelVmAddedEvent(message));
                    }, location: this.GetType());
                BuildEventPath<CoinKernelUpdatedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        if (_dicById.TryGetValue(message.Source.GetId(), out CoinKernelViewModel vm)) {
                            var supportedGpu = vm.SupportedGpu;
                            Guid dualCoinGroupId = vm.DualCoinGroupId;
                            vm.Update(message.Source);
                            if (supportedGpu != vm.SupportedGpu) {
                                var coinKernels = AllCoinKernels.Where(a => a.KernelId == vm.Id);
                                foreach (var coinKernel in coinKernels) {
                                    if (CoinVms.TryGetCoinVm(coinKernel.CoinId, out CoinViewModel coinVm)) {
                                        coinVm.OnPropertyChanged(nameof(coinVm.IsSupported));
                                        coinVm.OnPropertyChanged(nameof(coinVm.CoinKernels));
                                    }
                                }
                                var kernelVm = vm.Kernel;
                                kernelVm.OnPropertyChanged(nameof(kernelVm.CoinKernels));
                            }
                        }
                    }, location: this.GetType());
                BuildEventPath<CoinKernelRemovedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        if (_dicById.TryGetValue(message.Source.GetId(), out CoinKernelViewModel coinKernelVm)) {
                            _dicById.Remove(message.Source.GetId());
                            OnPropertyChanged(nameof(AllCoinKernels));
                            var kernelVm = coinKernelVm.Kernel;
                            kernelVm.OnPropertyChanged(nameof(kernelVm.CoinKernels));
                            kernelVm.OnPropertyChanged(nameof(kernelVm.CoinVms));
                            kernelVm.OnPropertyChanged(nameof(kernelVm.SupportedCoinVms));
                            kernelVm.OnPropertyChanged(nameof(kernelVm.SupportedCoins));
                            VirtualRoot.RaiseEvent(new CoinKernelVmRemovedEvent(message));
                        }
                    }, location: this.GetType());
                Init();
            }

            private void Init() {
                foreach (var item in NTMinerContext.Instance.ServerContext.CoinKernelSet.AsEnumerable()) {
                    _dicById.Add(item.GetId(), new CoinKernelViewModel(item));
                }
            }

            public bool TryGetCoinKernelVm(Guid id, out CoinKernelViewModel vm) {
                return _dicById.TryGetValue(id, out vm);
            }

            public List<CoinKernelViewModel> AllCoinKernels {
                get {
                    return _dicById.Values.ToList();
                }
            }
        }
    }
}
