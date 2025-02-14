﻿using NTMiner.Core.MinerServer;
using NTMiner.Gpus;
using NTMiner.User;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NTMiner.Core.Impl {
    /// <summary>
    /// 该基类有两个子类，两个子类的不同主要是数据源的不同，外网群控子类的数据源是redis，
    /// 外网群控运行在服务端，内网群控子类的数据源是litedb，内网群控运行在用户的电脑上。
    /// </summary>
    public abstract class ClientDataSetBase {
        protected readonly Dictionary<string, ClientData> _dicByObjectId = new Dictionary<string, ClientData>();
        protected readonly Dictionary<Guid, ClientData> _dicByClientId = new Dictionary<Guid, ClientData>();

        protected DateTime InitedOn = DateTime.MinValue;
        public bool IsReadied {
            get; private set;
        }

        // 因为两个子类的持久层一个是redis一个是litedb所以需要不同的持久化逻辑
        protected void DoUpdateSave(MinerData minerData) {
            DoUpdateSave(new MinerData[] { minerData });
        }
        protected abstract void DoUpdateSave(IEnumerable<MinerData> minerDatas);
        protected abstract void DoRemoveSave(MinerData minerData);

        private readonly bool _isPull;
        /// <summary>
        /// 这里挺绕，逻辑是父类通过回调函数的参数声明一个接收矿工列表的方法，然后由
        /// 子类调用该基类方法时传入矿工列表，从而父类收到了来自子类的矿工列表。因为
        /// 该基类有两个子类，一个子类的数据源是redis一个子类的数据源是litedb。
        /// </summary>
        /// <param name="isPull">内网群控传true，外网群控传false</param>
        /// <param name="getDatas"></param>
        public ClientDataSetBase(bool isPull, Action<Action<IEnumerable<ClientData>>> getDatas) {
            _isPull = isPull;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            getDatas(clientDatas => {
                InitedOn = DateTime.Now;
                IsReadied = true;
                foreach (var clientData in clientDatas) {
                    _dicByObjectId[clientData.Id] = clientData;
                    _dicByClientId[clientData.ClientId] = clientData;
                }
                stopwatch.Stop();
                NTMinerConsole.UserLine($"矿机集就绪，耗时 {stopwatch.GetElapsedSeconds().ToString("f2")} 秒", isPull ? MessageType.Debug : MessageType.Ok);
                VirtualRoot.RaiseEvent(new ClientSetInitedEvent());
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">传null表示是内网群控调用</param>
        /// <param name="query"></param>
        /// <param name="total"></param>
        /// <param name="coinSnapshots"></param>
        /// <param name="onlineCount"></param>
        /// <param name="miningCount"></param>
        /// <returns></returns>
        public List<ClientData> QueryClients(
            IUser user,
            QueryClientsRequest query,
            out int total,
            out CoinSnapshotData[] coinSnapshots,
            out int onlineCount,
            out int miningCount) {

            coinSnapshots = new CoinSnapshotData[0];
            onlineCount = 0;
            miningCount = 0;
            if (!IsReadied || query == null) {
                total = 0;
                return new List<ClientData>();
            }
            List<ClientData> list = new List<ClientData>();
            var data = _dicByObjectId.Values.ToArray();
            bool isIntranet = user == null;
            bool isFilterByUser = !isIntranet && !string.IsNullOrEmpty(user.LoginName) && !user.IsAdmin();
            bool isFilterByGroup = query.GroupId.HasValue && query.GroupId.Value != Guid.Empty;
            bool isFilterByMinerIp = !string.IsNullOrEmpty(query.MinerIp);
            bool isFilterByMinerName = !string.IsNullOrEmpty(query.MinerName);
            bool isFilterByVersion = !string.IsNullOrEmpty(query.Version);
            bool isFilterByWork = query.WorkId.HasValue && query.WorkId.Value != Guid.Empty;
            bool isFilterByCoin = !isFilterByWork && !string.IsNullOrEmpty(query.Coin);
            bool isFilterByPool = !isFilterByWork && !string.IsNullOrEmpty(query.Pool);
            bool isFilterByWallet = !isFilterByWork && !string.IsNullOrEmpty(query.Wallet);
            bool isFilterByKernel = !isFilterByWork && !string.IsNullOrEmpty(query.Kernel);
            bool isFilterByGpuType = query.GpuType != GpuType.Empty;
            bool isFilterByGpuName = !string.IsNullOrEmpty(query.GpuName);
            bool isFilterByGpuDriver = !string.IsNullOrEmpty(query.GpuDriver);
            for (int i = 0; i < data.Length; i++) {
                var item = data[i];
                bool isInclude = true;
                if (isInclude && isFilterByUser) {
                    isInclude = user.LoginName.Equals(item.LoginName);
                }
                if (isInclude && isFilterByGroup) {
                    isInclude = item.GroupId == query.GroupId.Value;
                }
                if (isInclude) {
                    switch (query.MineState) {
                        case MineStatus.All:
                            break;
                        case MineStatus.Mining:
                            isInclude = item.IsMining == true;
                            break;
                        case MineStatus.UnMining:
                            isInclude = item.IsMining == false;
                            break;
                        default:
                            break;
                    }
                }
                if (isInclude && isFilterByMinerIp) {
                    isInclude = !string.IsNullOrEmpty(item.LocalIp) && item.LocalIp.Contains(query.MinerIp);
                    if (!isInclude) {
                        if (query.MinerIp.IndexOf(':') != -1) {
                            isInclude = query.MinerIp.Equals(item.MinerIp);
                        }
                        else if (!string.IsNullOrEmpty(item.MinerIp)) {
                            // MinerIp可能带有端口号
                            int index = item.MinerIp.IndexOf(':');
                            string minerIp = item.MinerIp;
                            if (index != -1) {
                                minerIp = minerIp.Substring(0, index);
                            }
                            isInclude = query.MinerIp.Equals(minerIp);
                        }
                    }
                }
                if (isInclude && isFilterByMinerName) {
                    isInclude = (!string.IsNullOrEmpty(item.MinerName) && item.MinerName.IndexOf(query.MinerName, StringComparison.OrdinalIgnoreCase) != -1)
                        || (!string.IsNullOrEmpty(item.WorkerName) && item.WorkerName.IndexOf(query.MinerName, StringComparison.OrdinalIgnoreCase) != -1);
                }
                if (isInclude && isFilterByVersion) {
                    isInclude = !string.IsNullOrEmpty(item.Version) && item.Version.Contains(query.Version);
                }
                if (isInclude) {
                    if (isFilterByWork) {
                        isInclude = item.WorkId == query.WorkId.Value;
                    }
                    else {
                        if (isInclude && isFilterByCoin) {
                            isInclude = query.Coin.Equals(item.MainCoinCode) || query.Coin.Equals(item.DualCoinCode);
                        }
                        if (isInclude && isFilterByPool) {
                            isInclude = query.Pool.Equals(item.MainCoinPool) || query.Pool.Equals(item.DualCoinPool);
                        }
                        if (isInclude && isFilterByWallet) {
                            isInclude = query.Wallet.Equals(item.MainCoinWallet) || query.Wallet.Equals(item.DualCoinWallet);
                        }
                        if (isInclude && isFilterByKernel) {
                            isInclude = !string.IsNullOrEmpty(item.Kernel) && item.Kernel.IgnoreCaseContains(query.Kernel);
                        }
                    }
                }
                if (isInclude && isFilterByGpuType) {
                    isInclude = item.GpuType == query.GpuType;
                }
                if (isInclude && isFilterByGpuName) {
                    isInclude = !string.IsNullOrEmpty(item.GpuInfo) && item.GpuInfo.IgnoreCaseContains(query.GpuName);
                }
                if (isInclude && isFilterByGpuDriver) {
                    isInclude = !string.IsNullOrEmpty(item.GpuDriver) && item.GpuDriver.IgnoreCaseContains(query.GpuDriver);
                }
                if (isInclude) {
                    list.Add(item);
                }
            }
            total = list.Count();
            list.Sort(new ClientDataComparer(query.SortDirection, query.SortField));
            ClientData[] items = (isIntranet || user.IsAdmin()) ? data : data.Where(a => a.LoginName == user.LoginName).ToArray();
            coinSnapshots = VirtualRoot.CreateCoinSnapshots(_isPull, DateTime.Now, items, out onlineCount, out miningCount).ToArray();
            var results = list.Take(query).ToList();
            DateTime time = DateTime.Now.AddSeconds(_isPull ? -20 : -180);
            // 一定时间未上报算力视为0算力
            foreach (var clientData in results) {
                if (clientData.MinerActiveOn < time) {
                    clientData.DualCoinSpeed = 0;
                    clientData.MainCoinSpeed = 0;
                }
            }
            return results;
        }

        public ClientData GetByClientId(Guid clientId) {
            if (!IsReadied) {
                return null;
            }
            _dicByClientId.TryGetValue(clientId, out ClientData clientData);
            return clientData;
        }

        public ClientData GetByObjectId(string objectId) {
            if (!IsReadied) {
                return null;
            }
            if (objectId == null) {
                return null;
            }
            _dicByObjectId.TryGetValue(objectId, out ClientData clientData);
            return clientData;
        }

        public void UpdateClient(string objectId, string propertyName, object value) {
            if (!IsReadied) {
                return;
            }
            if (objectId == null) {
                return;
            }
            if (_dicByObjectId.TryGetValue(objectId, out ClientData clientData)
                && ClientData.TryGetReflectionUpdateProperty(propertyName, out PropertyInfo propertyInfo)) {
                value = VirtualRoot.ConvertValue(propertyInfo.PropertyType, value);
                var oldValue = propertyInfo.GetValue(clientData, null);
                if (oldValue != value) {
                    propertyInfo.SetValue(clientData, value, null);
                    DoUpdateSave(MinerData.Create(clientData));
                }
            }
        }

        public void UpdateClients(string propertyName, Dictionary<string, object> values) {
            if (!IsReadied) {
                return;
            }
            if (values.Count == 0) {
                return;
            }
            if (ClientData.TryGetReflectionUpdateProperty(propertyName, out PropertyInfo propertyInfo)) {
                values.ChangeValueType(propertyInfo.PropertyType);
                List<MinerData> minerDatas = new List<MinerData>();
                foreach (var kv in values) {
                    string objectId = kv.Key;
                    object value = kv.Value;
                    if (_dicByObjectId.TryGetValue(objectId, out ClientData clientData)) {
                        var oldValue = propertyInfo.GetValue(clientData, null);
                        if (oldValue != value) {
                            propertyInfo.SetValue(clientData, value, null);
                            minerDatas.Add(MinerData.Create(clientData));
                        }
                    }
                }
                DoUpdateSave(minerDatas);
            }
        }

        public void RemoveByObjectId(string objectId) {
            if (!IsReadied) {
                return;
            }
            if (objectId == null) {
                return;
            }
            if (_dicByObjectId.TryGetValue(objectId, out ClientData clientData)) {
                _dicByObjectId.Remove(objectId);
                _dicByClientId.Remove(clientData.ClientId);
                DoRemoveSave(MinerData.Create(clientData));
            }
        }

        public bool IsAnyClientInGroup(Guid groupId) {
            if (!IsReadied) {
                return false;
            }
            return _dicByObjectId.Values.Any(a => a.GroupId == groupId);
        }

        public bool IsAnyClientInWork(Guid workId) {
            if (!IsReadied) {
                return false;
            }
            return _dicByObjectId.Values.Any(a => a.WorkId == workId);
        }

        public IEnumerable<ClientData> AsEnumerable() {
            if (!IsReadied) {
                return new List<ClientData>();
            }
            return _dicByObjectId.Values;
        }
    }
}
