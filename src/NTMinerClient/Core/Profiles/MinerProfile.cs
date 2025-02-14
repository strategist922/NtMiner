﻿using Microsoft.Win32;
using NTMiner.Core.Profile;
using NTMiner.Core.Profiles.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NTMiner.Core.Profiles {
    internal partial class MinerProfile : IWorkProfile {
        private readonly INTMinerContext _root;

        private MinerProfileData _data = null;
        private CoinKernelProfileSet _coinKernelProfileSet;
        private CoinProfileSet _coinProfileSet;
        private PoolProfileSet _poolProfileSet;
        private WalletSet _walletSet;

        public MinerProfile(INTMinerContext root) {
            _root = root;
            VirtualRoot.BuildCmdPath<RefreshAutoBootStartCommand>(
                path: message => {
                    var minerProfileRepository = root.ServerContext.CreateLocalRepository<MinerProfileData>();
                    var data = minerProfileRepository.GetAll().FirstOrDefault();
                    if (data != null && _data != null) {
                        _data.IsAutoBoot = data.IsAutoBoot;
                        _data.IsAutoStart = data.IsAutoStart;
                        VirtualRoot.RaiseEvent(new AutoBootStartRefreshedEvent());
                    }
                }, location: this.GetType());
            Init(root);
        }

        public void ReInit(INTMinerContext root) {
            Init(root);
            // 本地数据集已刷新，此时刷新本地数据集的视图模型集
            VirtualRoot.RaiseEvent(new LocalContextReInitedEvent());
            // 本地数据集的视图模型已刷新，此时刷新本地数据集的视图界面
            VirtualRoot.RaiseEvent(new LocalContextReInitedEventHandledEvent());
        }

        #region Init
        private void Init(INTMinerContext root) {
            var minerProfileRepository = root.ServerContext.CreateLocalRepository<MinerProfileData>();
            _data = minerProfileRepository.GetAll().FirstOrDefault();
            var mineWorkRepository = root.ServerContext.CreateLocalRepository<MineWorkData>();
            MineWork = mineWorkRepository.GetAll().FirstOrDefault();
            if (_data == null) {
                Guid coinId = Guid.Empty;
                ICoin coin = root.ServerContext.CoinSet.AsEnumerable().OrderBy(a => a.Code).FirstOrDefault();
                if (coin != null) {
                    coinId = coin.GetId();
                }
                _data = MinerProfileData.CreateDefaultData(coinId);
            }
            else {
                // 交换到注册表以供守护进程访问
                if (ClientAppType.IsMinerClient && !NTMinerContext.IsJsonLocal) {
                    SetIsOuterUserEnabled(_data.IsOuterUserEnabled);
                    SetOuterUserId(_data.OuterUserId);
                }
            }
            if (!_data.IsAutoStart && NTMinerRegistry.GetIsAutoStart()) {
                _data.IsAutoStart = true;
                minerProfileRepository.Update(_data);
            }
            if (_coinProfileSet == null) {
                _coinProfileSet = new CoinProfileSet(root);
            }
            else {
                _coinProfileSet.Refresh();
            }
            if (_coinKernelProfileSet == null) {
                _coinKernelProfileSet = new CoinKernelProfileSet(root);
            }
            else {
                _coinKernelProfileSet.Refresh();
            }
            if (_poolProfileSet == null) {
                _poolProfileSet = new PoolProfileSet(root);
            }
            else {
                _poolProfileSet.Refresh();
            }
            if (_walletSet == null) {
                _walletSet = new WalletSet(root);
            }
            else {
                _walletSet.Refresh();
            }
        }
        #endregion

        #region methods
        public ICoinKernelProfile GetCoinKernelProfile(Guid coinKernelId) {
            return _coinKernelProfileSet.GetCoinKernelProfile(coinKernelId);
        }

        public void SetCoinKernelProfileProperty(Guid coinKernelId, string propertyName, object value) {
            if (_root.ServerContext.CoinKernelSet.TryGetCoinKernel(coinKernelId, out _)) {
                _coinKernelProfileSet.SetCoinKernelProfileProperty(coinKernelId, propertyName, value);
            }
        }

        public ICoinProfile GetCoinProfile(Guid coinId) {
            return _coinProfileSet.GetCoinProfile(coinId);
        }

        public void SetCoinProfileProperty(Guid coinId, string propertyName, object value) {
            if (_root.ServerContext.CoinSet.TryGetCoin(coinId, out ICoin _)) {
                _coinProfileSet.SetCoinProfileProperty(coinId, propertyName, value);
            }
        }

        public IPoolProfile GetPoolProfile(Guid poolId) {
            return _poolProfileSet.GetPoolProfile(poolId);
        }

        public void SetPoolProfileProperty(Guid poolId, string propertyName, object value) {
            if (_root.ServerContext.PoolSet.TryGetPool(poolId, out IPool pool)) {
                if (!pool.IsUserMode) {
                    NTMinerConsole.UserError($"{pool.Name}不是用户名密码模式的矿池");
                    return;
                }
                _poolProfileSet.SetPoolProfileProperty(poolId, propertyName, value);
            }
        }

        public bool TryGetWallet(Guid walletId, out IWallet wallet) {
            return _walletSet.TryGetWallet(walletId, out wallet);
        }

        public List<IWallet> GetWallets() {
            return _walletSet.GetWallets().ToList();
        }

        public List<ICoinKernelProfile> GetCoinKernelProfiles() {
            return _coinKernelProfileSet.GetCoinKernelProfiles().ToList();
        }

        public List<ICoinProfile> GetCoinProfiles() {
            return _coinProfileSet.GetCoinProfiles().ToList();
        }

        public List<IPool> GetPools() {
            return _root.ServerContext.PoolSet.AsEnumerable().ToList();
        }

        public List<IPoolProfile> GetPoolProfiles() {
            return _poolProfileSet.GetPoolProfiles().ToList();
        }
        #endregion

        #region properties
        [IgnoreReflectionSet]
        public IMineWork MineWork { get; private set; }

        public Guid GetId() {
            return this.Id;
        }

        [IgnoreReflectionSet]
        public Guid Id {
            get { return _data.Id; }
            private set {
                _data.Id = value;
            }
        }

        public string MinerName {
            get {
                if (string.IsNullOrEmpty(_data.MinerName)) {
                    _data.MinerName = NTKeyword.GetSafeMinerName(NTMinerContext.ThisPcName);
                }
                return _data.MinerName;
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    value = NTMinerContext.ThisPcName;
                }
                value = NTKeyword.GetSafeMinerName(value);
                _data.MinerName = value;
            }
        }

        public bool IsSystemName {
            get { return _data.IsSystemName; }
            private set {
                _data.IsSystemName = value;
            }
        }

        public bool IsNoShareRestartKernel {
            get => _data.IsNoShareRestartKernel;
            private set {
                _data.IsNoShareRestartKernel = value;
            }
        }

        public int AutoRestartKernelTimes {
            get {
                if (_data.AutoRestartKernelTimes < 3) {
                    return 3;
                }
                return _data.AutoRestartKernelTimes;
            }
            private set {
                _data.AutoRestartKernelTimes = value;
            }
        }

        public int NoShareRestartKernelMinutes {
            get => _data.NoShareRestartKernelMinutes;
            private set {
                _data.NoShareRestartKernelMinutes = value;
            }
        }

        public bool IsNoShareRestartComputer {
            get => _data.IsNoShareRestartComputer;
            private set {
                _data.IsNoShareRestartComputer = value;
            }
        }

        public int NoShareRestartComputerMinutes {
            get => _data.NoShareRestartComputerMinutes;
            private set {
                _data.NoShareRestartComputerMinutes = value;
            }
        }

        public bool IsPeriodicRestartKernel {
            get => _data.IsPeriodicRestartKernel;
            private set {
                _data.IsPeriodicRestartKernel = value;
            }
        }

        public int PeriodicRestartKernelHours {
            get => _data.PeriodicRestartKernelHours;
            private set {
                _data.PeriodicRestartKernelHours = value;
            }
        }

        public bool IsPeriodicRestartComputer {
            get => _data.IsPeriodicRestartComputer;
            private set {
                _data.IsPeriodicRestartComputer = value;
            }
        }

        public int PeriodicRestartComputerHours {
            get => _data.PeriodicRestartComputerHours;
            private set {
                _data.PeriodicRestartComputerHours = value;
            }
        }

        public int PeriodicRestartKernelMinutes {
            get => _data.PeriodicRestartKernelMinutes;
            private set {
                _data.PeriodicRestartKernelMinutes = value;
            }
        }

        public int PeriodicRestartComputerMinutes {
            get => _data.PeriodicRestartComputerMinutes;
            private set {
                _data.PeriodicRestartComputerMinutes = value;
            }
        }

        public bool IsAutoRestartKernel {
            get {
                return _data.IsAutoRestartKernel;
            }
            private set {
                _data.IsAutoRestartKernel = value;
            }
        }

        public bool IsSpeedDownRestartComputer {
            get {
                return _data.IsSpeedDownRestartComputer;
            }
            private set {
                _data.IsSpeedDownRestartComputer = value;
            }
        }

        public int RestartComputerSpeedDownPercent {
            get {
                return _data.RestartComputerSpeedDownPercent;
            }
            private set {
                _data.RestartComputerSpeedDownPercent = value;
            }
        }

        public bool IsNetUnavailableStopMine {
            get {
                return _data.IsNetUnavailableStopMine;
            }
            private set {
                _data.IsNetUnavailableStopMine = value;
            }
        }

        public int NetUnavailableStopMineMinutes {
            get {
                return _data.NetUnavailableStopMineMinutes;
            }
            private set {
                _data.NetUnavailableStopMineMinutes = value;
            }
        }

        public bool IsNetAvailableStartMine {
            get {
                return _data.IsNetAvailableStartMine;
            }
            private set {
                _data.IsNetAvailableStartMine = value;
            }
        }

        public int NetAvailableStartMineSeconds {
            get {
                return _data.NetAvailableStartMineSeconds;
            }
            private set {
                _data.NetAvailableStartMineSeconds = value;
            }
        }

        public bool IsEChargeEnabled {
            get {
                return _data.IsEChargeEnabled;
            }
            private set {
                _data.IsEChargeEnabled = value;
            }
        }

        public double EPrice {
            get {
                return _data.EPrice;
            }
            private set {
                if (_data.EPrice != value) {
                    _data.EPrice = value;
                    VirtualRoot.RaiseEvent(new EPriceChangedEvent());
                }
            }
        }

        public bool IsPowerAppend {
            get {
                return _data.IsPowerAppend;
            }
            private set {
                if (_data.IsPowerAppend != value) {
                    _data.IsPowerAppend = value;
                    VirtualRoot.RaiseEvent(new PowerAppendChangedEvent());
                }
            }
        }

        public int PowerAppend {
            get {
                return _data.PowerAppend;
            }
            private set {
                if (_data.PowerAppend != value) {
                    _data.PowerAppend = value;
                    VirtualRoot.RaiseEvent(new PowerAppendChangedEvent());
                }
            }
        }

        public Guid CoinId {
            get => _data.CoinId;
            private set {
                _data.CoinId = value;
            }
        }

        public int MaxTemp {
            get => _data.MaxTemp;
            private set {
                if (_data.MaxTemp != value) {
                    _data.MaxTemp = value;
                    VirtualRoot.RaiseEvent(new MaxTempChangedEvent());
                }
            }
        }

        public int AutoStartDelaySeconds {
            get => _data.AutoStartDelaySeconds;
            private set {
                _data.AutoStartDelaySeconds = value;
            }
        }

        public bool IsAutoDisableWindowsFirewall {
            get => _data.IsAutoDisableWindowsFirewall;
            private set {
                _data.IsAutoDisableWindowsFirewall = value;
            }
        }

        public bool IsDisableUAC {
            get => _data.IsDisableUAC;
            private set {
                _data.IsDisableUAC = value;
            }
        }

        public bool IsDisableWAU {
            get => _data.IsDisableWAU;
            private set {
                _data.IsDisableWAU = value;
            }
        }

        public bool IsDisableAntiSpyware {
            get => _data.IsDisableAntiSpyware;
            private set {
                _data.IsDisableAntiSpyware = value;
            }
        }

        public bool IsAutoReboot {
            get => _data.IsAutoReboot;
            private set {
                _data.IsAutoReboot = value;
            }
        }

        public bool IsShowInTaskbar {
            get => _data.IsShowInTaskbar;
            private set {
                _data.IsShowInTaskbar = value;
            }
        }

        public bool IsPreventDisplaySleep {
            get { return _data.IsPreventDisplaySleep; }
            set {
                _data.IsPreventDisplaySleep = value;
            }
        }

        public bool IsAutoNoUi {
            get => _data.IsAutoNoUi;
            private set {
                _data.IsAutoNoUi = value;
            }
        }

        public int AutoNoUiMinutes {
            get => _data.AutoNoUiMinutes;
            private set {
                _data.AutoNoUiMinutes = value;
            }
        }

        public bool IsShowNotifyIcon {
            get => _data.IsShowNotifyIcon;
            private set {
                _data.IsShowNotifyIcon = value;
            }
        }

        public bool IsCloseMeanExit {
            get => _data.IsCloseMeanExit;
            private set {
                _data.IsCloseMeanExit = value;
            }
        }

        public bool Is1080PillEnabled {
            get { return _data.Is1080PillEnabled; }
            private set {
                _data.Is1080PillEnabled = value;
            }
        }

        public bool IsShowCommandLine {
            get => _data.IsShowCommandLine;
            private set {
                _data.IsShowCommandLine = value;
            }
        }

        public bool IsAutoBoot {
            get => _data.IsAutoBoot;
            private set {
                _data.IsAutoBoot = value;
            }
        }

        public bool IsAutoStart {
            get { 
                return _data.IsAutoStart; 
            }
            private set {
                _data.IsAutoStart = value;
                // 兼容旧版本
                NTMinerRegistry.SetIsAutoStart(value);
            }
        }

        public bool IsCreateShortcut {
            get => _data.IsCreateShortcut;
            private set {
                _data.IsCreateShortcut = value;
            }
        }

        public bool IsAutoStopByCpu {
            get => _data.IsAutoStopByCpu;
            private set {
                _data.IsAutoStopByCpu = value;
            }
        }

        public int CpuStopTemperature {
            get => _data.CpuStopTemperature;
            private set {
                _data.CpuStopTemperature = value;
            }
        }

        public int CpuGETemperatureSeconds {
            get => _data.CpuGETemperatureSeconds;
            private set {
                _data.CpuGETemperatureSeconds = value;
            }
        }

        public bool IsAutoStartByCpu {
            get => _data.IsAutoStartByCpu;
            private set {
                _data.IsAutoStartByCpu = value;
            }
        }

        public int CpuStartTemperature {
            get => _data.CpuStartTemperature;
            private set {
                _data.CpuStartTemperature = value;
            }
        }

        public int CpuLETemperatureSeconds {
            get => _data.CpuLETemperatureSeconds;
            private set {
                _data.CpuLETemperatureSeconds = value;
            }
        }

        public bool IsRaiseHighCpuEvent {
            get => _data.IsRaiseHighCpuEvent;
            private set {
                _data.IsRaiseHighCpuEvent = value;
            }
        }

        public int HighCpuBaseline {
            get => _data.HighCpuBaseline;
            private set {
                _data.HighCpuBaseline = value;
            }
        }

        public int HighCpuSeconds {
            get => _data.HighCpuSeconds;
            private set {
                _data.HighCpuSeconds = value;
            }
        }

        public bool IsOuterUserEnabled {
            get => _data.IsOuterUserEnabled;
            private set {
                _data.IsOuterUserEnabled = value;
                SetIsOuterUserEnabled(value);
            }
        }

        public string OuterUserId {
            get => _data.OuterUserId;
            private set {
                _data.OuterUserId = value;
                SetOuterUserId(value);
            }
        }

        #endregion

        private static void SetIsOuterUserEnabled(bool isOuterUserEnabled) {
            if (!ClientAppType.IsMinerClient) {
                return;
            }
            Windows.WinRegistry.SetValue(Registry.Users, NTMinerRegistry.NTMinerRegistrySubKey, NTKeyword.IsOuterUserEnabledRegistryKey, isOuterUserEnabled.ToString());
        }

        private static void SetOuterUserId(string outerUserId) {
            if (!ClientAppType.IsMinerClient) {
                return;
            }
            Windows.WinRegistry.SetValue(Registry.Users, NTMinerRegistry.NTMinerRegistrySubKey, NTKeyword.OuterUserIdRegistryKey, outerUserId);
        }

        private static Dictionary<string, PropertyInfo> s_properties;
        [IgnoreReflectionSet]
        private static Dictionary<string, PropertyInfo> Properties {
            get {
                if (s_properties == null) {
                    s_properties = GetPropertiesCanSet<MinerProfile>();
                }
                return s_properties;
            }
        }

        private static Dictionary<string, PropertyInfo> GetPropertiesCanSet<T>() {
            var properties = new Dictionary<string, PropertyInfo>();
            Type attrubuteType = typeof(IgnoreReflectionSetAttribute);
            foreach (var propertyInfo in typeof(T).GetProperties().Where(a => a.CanWrite)) {
                if (propertyInfo.GetCustomAttributes(attrubuteType, inherit: false).Length > 0) {
                    continue;
                }
                properties.Add(propertyInfo.Name, propertyInfo);
            }
            return properties;
        }

        public void SetMinerProfileProperty(string propertyName, object value) {
            if (Properties.TryGetValue(propertyName, out PropertyInfo propertyInfo)) {
                if (propertyInfo.CanWrite) {
                    // 这里的反射赋值没有经过序列化和反序列化且由接口约束类型一定相同所以可以直接赋值和比较
                    object oldValue = propertyInfo.GetValue(this, null);
                    if (oldValue != value) {
                        propertyInfo.SetValue(this, value, null);
                        var repository = _root.ServerContext.CreateLocalRepository<MinerProfileData>();
                        repository.Update(_data);
                        VirtualRoot.RaiseEvent(new MinerProfilePropertyChangedEvent(propertyName));
                    }
                }
            }
        }

        public string GetSha1() {
            StringBuilder sb = new StringBuilder();
            if (_data != null) {
                sb.Append(_data.ToString());
            }

            if (this.CoinId != Guid.Empty) {
                ICoinProfile coinProfile = GetCoinProfile(this.CoinId);
                if (coinProfile != null) {
                    sb.Append(coinProfile.ToString());
                    ICoinKernelProfile coinKernelProfile = GetCoinKernelProfile(coinProfile.CoinKernelId);
                    if (coinKernelProfile != null) {
                        sb.Append(coinKernelProfile.ToString());
                        if (coinKernelProfile.IsDualCoinEnabled) {
                            ICoinProfile dualCoinProfile = GetCoinProfile(coinKernelProfile.DualCoinId);
                            if (dualCoinProfile != null) {
                                sb.Append(dualCoinProfile.ToString());
                                IPoolProfile dualCoinPoolProfile = GetPoolProfile(dualCoinProfile.PoolId);
                                if (dualCoinPoolProfile != null) {
                                    sb.Append(dualCoinPoolProfile.ToString());
                                }
                            }
                            ICoinKernelProfile dualCoinKernelProfile = GetCoinKernelProfile(coinKernelProfile.DualCoinId);
                            if (dualCoinKernelProfile != null) {
                                sb.Append(dualCoinKernelProfile.ToString());
                            }
                        }
                    }
                    IPoolProfile poolProfile = GetPoolProfile(coinProfile.PoolId);
                    if (poolProfile != null) {
                        sb.Append(poolProfile.ToString());
                    }
                }
            }

            return HashUtil.Sha1(sb.ToString());
        }
    }
}
