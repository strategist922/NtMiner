﻿using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner {
    public static partial class AppRoot {
        public class PackageViewModels : ViewModelBase {
            public static PackageViewModels Instance { get; private set; } = new PackageViewModels();
            private readonly Dictionary<Guid, PackageViewModel> _dicById = new Dictionary<Guid, PackageViewModel>();

            private PackageViewModels() {
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
                        OnPropertyChanged(nameof(AllPackages));
                    }, location: this.GetType());
                BuildEventPath<PackageAddedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        _dicById.Add(message.Source.GetId(), new PackageViewModel(message.Source));
                        OnPropertyChanged(nameof(AllPackages));
                        foreach (var item in KernelVms.AllKernels) {
                            item.OnPropertyChanged(nameof(item.IsPackageValid));
                        }
                    }, location: this.GetType());
                BuildEventPath<PackageRemovedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: message => {
                        _dicById.Remove(message.Source.GetId());
                        OnPropertyChanged(nameof(AllPackages));
                        foreach (var item in KernelVms.AllKernels) {
                            item.OnPropertyChanged(nameof(item.IsPackageValid));
                        }
                    }, location: this.GetType());
                BuildEventPath<PackageUpdatedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: message => {
                        if (_dicById.TryGetValue(message.Source.GetId(), out PackageViewModel vm)) {
                            vm.Update(message.Source);
                            foreach (var item in KernelVms.AllKernels) {
                                item.OnPropertyChanged(nameof(item.IsPackageValid));
                            }
                        }
                    }, location: this.GetType());
                Init();
            }

            private void Init() {
                foreach (var item in NTMinerContext.Instance.ServerContext.PackageSet.AsEnumerable()) {
                    _dicById.Add(item.GetId(), new PackageViewModel(item));
                }
            }

            public bool TryGetPackageVm(Guid packageId, out PackageViewModel PackageVm) {
                return _dicById.TryGetValue(packageId, out PackageVm);
            }

            public List<PackageViewModel> AllPackages {
                get {
                    return _dicById.Values.OrderBy(a => a.Name).ToList();
                }
            }
        }
    }
}
