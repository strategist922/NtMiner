﻿using NTMiner.Core.Daemon;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace NTMiner.Core.Mq.Senders.Impl {
    public class OperationMqSender : IOperationMqSender {
        private readonly IMq _mq;
        public OperationMqSender(IMq mq) {
            _mq = mq;
        }

        public void SendStartWorkMine(string loginName, Guid clientId, WorkRequest request) {
            if (string.IsNullOrEmpty(loginName) || clientId == Guid.Empty || request == null) {
                return;
            }
            _mq.MqChannel.BasicPublish(
                exchange: MqKeyword.NTMinerExchange,
                routingKey: MqKeyword.StartWorkMineRoutingKey,
                basicProperties: CreateBasicProperties(loginName, clientId),
                body: OperationMqBodyUtil.GetWorkRequestMqSendBody(request));
        }

        private IBasicProperties CreateBasicProperties(string loginName, Guid clientId) {
            var basicProperties = CreateBasicProperties(loginName);
            basicProperties.Headers[MqKeyword.ClientIdHeaderName] = clientId.ToString();

            return basicProperties;
        }

        private IBasicProperties CreateBasicProperties(string loginName) {
            var basicProperties = _mq.MqChannel.CreateBasicProperties();
            basicProperties.Persistent = false;
            basicProperties.Expiration = MqKeyword.Expiration36sec;
            basicProperties.Timestamp = new AmqpTimestamp(Timestamp.GetTimestamp());
            basicProperties.AppId = ServerRoot.HostConfig.ThisServerAddress;
            basicProperties.Headers = new Dictionary<string, object> {
                [MqKeyword.LoginNameHeaderName] = loginName
            };

            return basicProperties;
        }
    }
}
