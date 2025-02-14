﻿using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner {
    public static partial class AppRoot {
        public class KernelOutputTranslaterViewModels : ViewModelBase {
            public static KernelOutputTranslaterViewModels Instance { get; private set; } = new KernelOutputTranslaterViewModels();
            private readonly Dictionary<Guid, List<KernelOutputTranslaterViewModel>> _dicByKernelOutputId = new Dictionary<Guid, List<KernelOutputTranslaterViewModel>>();
            private readonly Dictionary<Guid, KernelOutputTranslaterViewModel> _dicById = new Dictionary<Guid, KernelOutputTranslaterViewModel>();

            private KernelOutputTranslaterViewModels() {
                if (WpfUtil.IsInDesignMode) {
                    return;
                }
                VirtualRoot.BuildEventPath<ServerContextReInitedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: message => {
                        _dicById.Clear();
                        _dicByKernelOutputId.Clear();
                        Init();
                    }, location: this.GetType());
                VirtualRoot.BuildEventPath<ServerContextReInitedEventHandledEvent>("刷新视图界面", LogEnum.DevConsole,
                    path: message => {
                        OnPropertyChanged(nameof(AllKernelOutputTranslaterVms));
                    }, location: this.GetType());
                BuildEventPath<KernelOutputTranslaterAddedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: message => {
                        if (KernelOutputVms.TryGetKernelOutputVm(message.Source.KernelOutputId, out KernelOutputViewModel kernelOutputVm)) {
                            if (!_dicByKernelOutputId.ContainsKey(message.Source.KernelOutputId)) {
                                _dicByKernelOutputId.Add(message.Source.KernelOutputId, new List<KernelOutputTranslaterViewModel>());
                            }
                            var vm = new KernelOutputTranslaterViewModel(message.Source);
                            _dicByKernelOutputId[message.Source.KernelOutputId].Add(vm);
                            _dicById.Add(message.Source.GetId(), vm);
                            kernelOutputVm.OnPropertyChanged(nameof(kernelOutputVm.KernelOutputTranslaters));
                        }
                    }, location: this.GetType());
                BuildEventPath<KernelOutputTranslaterUpdatedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: message => {
                        if (_dicByKernelOutputId.TryGetValue(message.Source.KernelOutputId, out List<KernelOutputTranslaterViewModel> vms)) {
                            var vm = vms.FirstOrDefault(a => a.Id == message.Source.GetId());
                            if (vm != null) {
                                vm.Update(message.Source);
                            }
                        }
                    }, location: this.GetType());
                BuildEventPath<KernelOutputTranslaterRemovedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: message => {
                        if (_dicByKernelOutputId.ContainsKey(message.Source.KernelOutputId)) {
                            var item = _dicByKernelOutputId[message.Source.KernelOutputId].FirstOrDefault(a => a.Id == message.Source.GetId());
                            if (item != null) {
                                _dicByKernelOutputId[message.Source.KernelOutputId].Remove(item);
                            }
                        }
                        if (_dicById.ContainsKey(message.Source.GetId())) {
                            _dicById.Remove(message.Source.GetId());
                        }
                        if (KernelOutputVms.TryGetKernelOutputVm(message.Source.KernelOutputId, out KernelOutputViewModel kernelOutputVm)) {
                            kernelOutputVm.OnPropertyChanged(nameof(kernelOutputVm.KernelOutputTranslaters));
                        }
                    }, location: this.GetType());
                Init();
            }

            private void Init() {
                foreach (var item in NTMinerContext.Instance.ServerContext.KernelOutputTranslaterSet.AsEnumerable()) {
                    if (!_dicByKernelOutputId.ContainsKey(item.KernelOutputId)) {
                        _dicByKernelOutputId.Add(item.KernelOutputId, new List<KernelOutputTranslaterViewModel>());
                    }
                    var vm = new KernelOutputTranslaterViewModel(item);
                    _dicByKernelOutputId[item.KernelOutputId].Add(vm);
                    _dicById.Add(item.GetId(), vm);
                }
            }

            public IEnumerable<KernelOutputTranslaterViewModel> AllKernelOutputTranslaterVms {
                get {
                    return _dicById.Values;
                }
            }

            public IEnumerable<KernelOutputTranslaterViewModel> GetListByKernelId(Guid kernelId) {
                if (_dicByKernelOutputId.ContainsKey(kernelId)) {
                    return _dicByKernelOutputId[kernelId];
                }
                return new List<KernelOutputTranslaterViewModel>();
            }
        }
    }
}
