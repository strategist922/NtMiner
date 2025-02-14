﻿using NTMiner.Core.MinerServer;
using System;

namespace NTMiner.Core.Mq.Senders {
    public interface IMinerClientMqSender : IMqSender {
        void SendMinerDataAdded(string minerId, Guid clientId);
        void SendMinerDataRemoved(string minerId, Guid clientId);
        void SendMinerSignChanged(string minerId, Guid clientId);
        void SendResponseClientsForWs(string wsServerIp, string loginName, string sessionId, QueryClientsResponse response);
    }
}
