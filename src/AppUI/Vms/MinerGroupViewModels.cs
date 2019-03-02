﻿using NTMiner.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NTMiner.Vms {
    public class MinerGroupViewModels : ViewModelBase, IEnumerable<MinerGroupViewModel> {
        public static readonly MinerGroupViewModels Current = new MinerGroupViewModels();
        private readonly Dictionary<Guid, MinerGroupViewModel> _dicById = new Dictionary<Guid, MinerGroupViewModel>();

        public ICommand Add { get; private set; }

        private MinerGroupViewModels() {
            if (Design.IsInDesignMode) {
                return;
            }
            foreach (var item in NTMinerRoot.Current.MinerGroupSet) {
                _dicById.Add(item.GetId(), new MinerGroupViewModel(item));
            }
            this.Add = new DelegateCommand(() => {
                new MinerGroupViewModel(Guid.NewGuid()).Edit.Execute(FormType.Add);
            });
            VirtualRoot.On<MinerGroupAddedEvent>(
                "添加矿工分组后刷新VM内存",
                LogEnum.Console,
                action: message => {
                    if (!_dicById.ContainsKey(message.Source.GetId())) {
                        _dicById.Add(message.Source.GetId(), new MinerGroupViewModel(message.Source));
                        OnPropertyChanged(nameof(List));
                        MinerClientsWindowViewModel.Current.OnPropertyChanged(nameof(MinerClientsWindowViewModel.MinerGroupVmItems));
                        MinerClientsWindowViewModel.Current.OnPropertyChanged(nameof(MinerClientsWindowViewModel.SelectedMinerGroup));
                    }
                });
            VirtualRoot.On<MinerGroupUpdatedEvent>(
                "更新矿工分组后刷新VM内存",
                LogEnum.Console,
                action: message => {
                    _dicById[message.Source.GetId()].Update(message.Source);
                });
            VirtualRoot.On<MinerGroupRemovedEvent>(
                "删除矿工分组后刷新VM内存",
                LogEnum.Console,
                action: message => {
                    _dicById.Remove(message.Source.GetId());
                    OnPropertyChanged(nameof(List));
                    MinerClientsWindowViewModel.Current.OnPropertyChanged(nameof(MinerClientsWindowViewModel.MinerGroupVmItems));
                    MinerClientsWindowViewModel.Current.OnPropertyChanged(nameof(MinerClientsWindowViewModel.SelectedMinerGroup));
                });
        }

        public List<MinerGroupViewModel> List {
            get {
                return _dicById.Values.ToList();
            }
        }

        public bool TryGetMineWorkVm(Guid id, out MinerGroupViewModel minerGroupVm) {
            return _dicById.TryGetValue(id, out minerGroupVm);
        }

        public IEnumerator<MinerGroupViewModel> GetEnumerator() {
            return _dicById.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _dicById.Values.GetEnumerator();
        }
    }
}
