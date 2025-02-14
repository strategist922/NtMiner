﻿using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NTMiner {
    public static partial class AppRoot {
        public class FileWriterViewModels : ViewModelBase {
            public static FileWriterViewModels Instance { get; private set; } = new FileWriterViewModels();
            private readonly Dictionary<Guid, FileWriterViewModel> _dicById = new Dictionary<Guid, FileWriterViewModel>();
            public ICommand Add { get; private set; }
            private FileWriterViewModels() {
                if (WpfUtil.IsInDesignMode) {
                    return;
                }
                this.Add = new DelegateCommand(() => {
                    new FileWriterViewModel(Guid.NewGuid()).Edit.Execute(FormType.Add);
                });
                VirtualRoot.BuildEventPath<ServerContextReInitedEvent>("刷新VM内存", LogEnum.DevConsole,
                    path: message => {
                        _dicById.Clear();
                        Init();
                    }, location: this.GetType());
                VirtualRoot.BuildEventPath<ServerContextReInitedEventHandledEvent>("刷新视图界面", LogEnum.DevConsole,
                    path: message => {
                        OnPropertyChangeds();
                    }, location: this.GetType());
                BuildEventPath<FileWriterAddedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        if (!_dicById.ContainsKey(message.Source.GetId())) {
                            FileWriterViewModel groupVm = new FileWriterViewModel(message.Source);
                            _dicById.Add(message.Source.GetId(), groupVm);
                            OnPropertyChangeds();
                        }
                    }, location: this.GetType());
                BuildEventPath<FileWriterUpdatedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        if (_dicById.TryGetValue(message.Source.GetId(), out FileWriterViewModel vm)) {
                            vm.Update(message.Source);
                        }
                    }, location: this.GetType());
                BuildEventPath<FileWriterRemovedEvent>("调整VM内存", LogEnum.DevConsole,
                    path: (message) => {
                        _dicById.Remove(message.Source.GetId());
                        OnPropertyChangeds();
                    }, location: this.GetType());
                Init();
            }

            private void Init() {
                foreach (var item in NTMinerContext.Instance.ServerContext.FileWriterSet.AsEnumerable()) {
                    FileWriterViewModel groupVm = new FileWriterViewModel(item);
                    _dicById.Add(item.GetId(), groupVm);
                }
            }

            private void OnPropertyChangeds() {
                OnPropertyChanged(nameof(List));
            }

            public bool TryGetFileWriterVm(Guid groupId, out FileWriterViewModel groupVm) {
                return _dicById.TryGetValue(groupId, out groupVm);
            }

            public List<FileWriterViewModel> List {
                get {
                    return _dicById.Values.ToList();
                }
            }
        }
    }
}
