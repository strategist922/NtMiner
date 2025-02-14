﻿using NTMiner.Core;
using NTMiner.Core.Daemon;
using NTMiner.Core.MinerClient;
using NTMiner.Core.MinerServer;
using NTMiner.Ws;
using System;
using System.Collections.Generic;

namespace NTMiner {
    public static class WsMessageFromMinerStudioHandler {
        private static readonly Dictionary<string, Action<IMinerStudioSession, WsMessage>>
            _handlers = new Dictionary<string, Action<IMinerStudioSession, WsMessage>>(StringComparer.OrdinalIgnoreCase) {
                [WsMessage.GetConsoleOutLines] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientIdData) && wrapperClientIdData.TryGetData(out long afterTime)) {
                        AppRoot.OperationMqSender.SendGetConsoleOutLines(session.LoginName, wrapperClientIdData.ClientId, afterTime);
                    }
                },
                [WsMessage.GetLocalMessages] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientIdData) && wrapperClientIdData.TryGetData(out long afterTime)) {
                        AppRoot.OperationMqSender.SendGetLocalMessages(session.LoginName, wrapperClientIdData.ClientId, afterTime);
                    }
                },
                [WsMessage.GetDrives] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendGetDrives(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.GetLocalIps] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendGetLocalIps(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.GetOperationResults] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientIdData) && wrapperClientIdData.TryGetData(out long afterTime)) {
                        AppRoot.OperationMqSender.SendGetOperationResults(session.LoginName, wrapperClientIdData.ClientId, afterTime);
                    }
                },
                [WsMessage.GetSpeed] = (session, message) => {
                    if (message.TryGetData(out List<Guid> minerIds)) {
                        AppRoot.OperationMqSender.SendGetSpeed(session.LoginName, minerIds);
                    }
                },
                [WsMessage.EnableRemoteDesktop] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendEnableRemoteDesktop(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.BlockWAU] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendBlockWAU(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.SetVirtualMemory] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientIdData) && wrapperClientIdData.TryGetData(out Dictionary<string, int> data)) {
                        AppRoot.OperationMqSender.SendSetVirtualMemory(session.LoginName, wrapperClientIdData.ClientId, data);
                    }
                },
                [WsMessage.SetLocalIps] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientIdData) && wrapperClientIdData.TryGetData(out List<LocalIpInput> data)) {
                        AppRoot.OperationMqSender.SendSetLocalIps(session.LoginName, wrapperClientIdData.ClientId, data);
                    }
                },
                [WsMessage.SwitchRadeonGpu] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientIdData) && wrapperClientIdData.TryGetData(out bool on)) {
                        AppRoot.OperationMqSender.SendSwitchRadeonGpu(session.LoginName, wrapperClientIdData.ClientId, on);
                    }
                },
                [WsMessage.GetSelfWorkLocalJson] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendGetSelfWorkLocalJson(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.SaveSelfWorkLocalJson] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientData) && wrapperClientData.TryGetData(out WorkRequest workRequest)) {
                        AppRoot.OperationMqSender.SendSaveSelfWorkLocalJson(session.LoginName, wrapperClientData.ClientId, workRequest);
                    }
                },
                [WsMessage.GetGpuProfilesJson] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendGetGpuProfilesJson(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.SaveGpuProfilesJson] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientData) && wrapperClientData.TryGetData(out string json)) {
                        AppRoot.OperationMqSender.SendSaveGpuProfilesJson(session.LoginName, wrapperClientData.ClientId, json);
                    }
                },
                [WsMessage.SetAutoBootStart] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientData) && wrapperClientData.TryGetData(out SetAutoBootStartRequest body)) {
                        AppRoot.OperationMqSender.SendSetAutoBootStart(session.LoginName, wrapperClientData.ClientId, body);
                    }
                },
                [WsMessage.RestartWindows] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendRestartWindows(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.ShutdownWindows] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendShutdownWindows(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.UpgradeNTMiner] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientData) && wrapperClientData.TryGetData(out string ntminerFileName)) {
                        AppRoot.OperationMqSender.SendUpgradeNTMiner(session.LoginName, wrapperClientData.ClientId, ntminerFileName);
                    }
                },
                [WsMessage.StartMine] = (session, message) => {
                    if (message.TryGetData(out WrapperClientIdData wrapperClientData) && wrapperClientData.TryGetData(out Guid workId)) {
                        AppRoot.OperationMqSender.SendStartMine(session.LoginName, wrapperClientData.ClientId, workId);
                    }
                },
                [WsMessage.StopMine] = (session, message) => {
                    if (message.TryGetData(out WrapperClientId wrapperClientId)) {
                        AppRoot.OperationMqSender.SendStopMine(session.LoginName, wrapperClientId.ClientId);
                    }
                },
                [WsMessage.QueryClientDatas] = (session, message) => {
                    if (message.TryGetData(out QueryClientsRequest query)) {
                        AppRoot.MinerClientMqSender.SendQueryClientsForWs(session.WsSessionId, QueryClientsForWsRequest.Create(query, session.LoginName));
                    }
                }
            };

        public static bool TryGetHandler(string wsMessageType, out Action<IMinerStudioSession, WsMessage> handler) {
            return _handlers.TryGetValue(wsMessageType, out handler);
        }
    }
}
