﻿using RabbitMQ.Client;

namespace NTMiner.Core.Mq.Senders.Impl {
    public class WsServerNodeMqSender : IWsServerNodeMqSender {
        private readonly IMq _mq;
        public WsServerNodeMqSender(IMq mq) {
            _mq = mq;
        }

        public void SendWsServerNodeAdded(string wsServerNodeAddress) {
            if (string.IsNullOrEmpty(wsServerNodeAddress)) {
                return;
            }
            _mq.MqChannel.BasicPublish(
                exchange: MqKeyword.NTMinerExchange, 
                routingKey: MqKeyword.WsServerNodeAddedRoutingKey, 
                basicProperties: CreateBasicProperties(), 
                body: WsServerNodeMqBodyUtil.GetWsServerNodeAddressMqSendBody(wsServerNodeAddress));
        }

        public void SendWsServerNodeRemoved(string wsServerNodeAddress) {
            if (string.IsNullOrEmpty(wsServerNodeAddress)) {
                return;
            }
            _mq.MqChannel.BasicPublish(
                exchange: MqKeyword.NTMinerExchange, 
                routingKey: MqKeyword.WsServerNodeRemovedRoutingKey,
                basicProperties: CreateBasicProperties(), 
                body: WsServerNodeMqBodyUtil.GetWsServerNodeAddressMqSendBody(wsServerNodeAddress));
        }

        private IBasicProperties CreateBasicProperties() {
            var basicProperties = _mq.MqChannel.CreateBasicProperties();
            basicProperties.Persistent = false;
            basicProperties.Expiration = MqKeyword.Expiration36sec;
            basicProperties.AppId = ServerRoot.HostConfig.ThisServerAddress;

            return basicProperties;
        }
    }
}
