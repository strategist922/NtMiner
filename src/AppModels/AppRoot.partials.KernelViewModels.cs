﻿using NTMiner.Core;
using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner {
    public static partial class AppRoot {
        public class KernelViewModels : ViewModelBase {
            public static KernelViewModels Instance { get; private set; } = new KernelViewModels();
            private readonly Dictionary<Guid, KernelViewModel> _dicById = new Dictionary<Guid, KernelViewModel>();
            public event Action<KernelViewModel> IsDownloadingChanged;

            public void OnIsDownloadingChanged(KernelViewModel kernelVm) {
                IsDownloadingChanged?.Invoke(kernelVm);
            }

            private KernelViewModels() {
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
                        OnPropertyChanged(nameof(AllKernels));
                    }, location: this.GetType());
                BuildEventPath<KernelAddedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        _dicById.Add(message.Source.GetId(), new KernelViewModel(message.Source));
                        OnPropertyChanged(nameof(AllKernels));
                        foreach (var coinKernelVm in CoinKernelVms.AllCoinKernels.Where(a => a.KernelId == message.Source.GetId())) {
                            coinKernelVm.OnPropertyChanged(nameof(coinKernelVm.IsSupportDualMine));
                        }
                    }, location: this.GetType());
                BuildEventPath<KernelRemovedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: message => {
                        _dicById.Remove(message.Source.GetId());
                        OnPropertyChanged(nameof(AllKernels));
                        foreach (var coinKernelVm in CoinKernelVms.AllCoinKernels.Where(a => a.KernelId == message.Source.GetId())) {
                            coinKernelVm.OnPropertyChanged(nameof(coinKernelVm.IsSupportDualMine));
                        }
                    }, location: this.GetType());
                BuildEventPath<KernelUpdatedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: message => {
                        if (_dicById.TryGetValue(message.Source.GetId(), out KernelViewModel vm)) {
                            PublishStatus publishStatus = vm.PublishState;
                            Guid kernelInputId = vm.KernelInputId;
                            vm.Update(message.Source);
                            if (publishStatus != vm.PublishState) {
                                foreach (var coinKernelVm in CoinKernelVms.AllCoinKernels.Where(a => a.KernelId == vm.Id)) {
                                    foreach (var coinVm in CoinVms.AllCoins.Where(a => a.Id == coinKernelVm.CoinId)) {
                                        coinVm.OnPropertyChanged(nameof(coinVm.CoinKernels));
                                    }
                                }
                            }
                            if (kernelInputId != vm.KernelInputId) {
                                CoinViewModel coinVm = MinerProfileVm.CoinVm;
                                if (coinVm != null && coinVm.CoinKernel != null && coinVm.CoinKernel.Kernel.Id == vm.Id) {
                                    NTMinerContext.RefreshArgsAssembly.Invoke("当前选用的内核切换了引用的内核输入");
                                }
                            }
                        }
                    }, location: this.GetType());
                Init();
            }

            private void Init() {
                foreach (var item in NTMinerContext.Instance.ServerContext.KernelSet.AsEnumerable()) {
                    _dicById.Add(item.GetId(), new KernelViewModel(item));
                }
            }

            public bool TryGetKernelVm(Guid kernelId, out KernelViewModel kernelVm) {
                return _dicById.TryGetValue(kernelId, out kernelVm);
            }

            public List<KernelViewModel> AllKernels {
                get {
                    return _dicById.Values.ToList();
                }
            }
        }
    }
}
