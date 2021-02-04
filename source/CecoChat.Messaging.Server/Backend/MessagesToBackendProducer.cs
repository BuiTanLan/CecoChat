﻿using System;
using CecoChat.Contracts.Backend;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.Kafka;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public sealed class MessagesToBackendProducer : IBackendProducer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IProducer<Null, BackendMessage> _producer;

        public MessagesToBackendProducer(
            ILogger<MessagesToBackendProducer> logger,
            IOptions<BackendOptions> backendOptions,
            IMessagingConfiguration messagingConfiguration,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _messagingConfiguration = messagingConfiguration;
            _partitionUtility = partitionUtility;
            _partitionFlyweight = partitionFlyweight;

            applicationLifetime.ApplicationStopping.Register(FlushPendingMessages);

            ProducerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', _backendOptions.Kafka.BootstrapServers),
                Acks = Acks.All,
                LingerMs = 1.0,
                MessageTimeoutMs = 300000,
                MessageSendMaxRetries = 8
            };

            _producer = new ProducerBuilder<Null, BackendMessage>(configuration)
                .SetValueSerializer(new BackendMessageSerializer())
                .Build();
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

        private void FlushPendingMessages()
        {
            if (_producer == null)
                return;

            try
            {
                _logger.LogInformation("Flushing pending backend messages...");
                _producer.Flush();
                _logger.LogInformation("Flushing pending backend messages succeeded.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Flushing pending backend messages failed.");
            }
        }

        public void ProduceMessage(BackendMessage message)
        {
            int partition = _partitionUtility.ChoosePartition(message.ReceiverId, _messagingConfiguration.PartitionCount);
            TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backendOptions.MessagesTopicName, partition);
            Message<Null, BackendMessage> kafkaMessage = new()
            {
                Value = message
            };

            _producer.Produce(topicPartition, kafkaMessage, DeliveryHandler);
            _logger.LogTrace("Produced backend message {0}.", message);
        }

        private void DeliveryHandler(DeliveryReport<Null, BackendMessage> report)
        {
            BackendMessage message = report.Message.Value;

            if (report.Status != PersistenceStatus.Persisted)
            {
                _logger.LogError("Backend message {0} persistence status {1}.",
                    message.MessageId, report.Status);
            }
            if (report.Error.IsError)
            {
                _logger.LogError("Backend message {0} error '{1}'.",
                    message.MessageId, report.Error.Reason);
            }
            if (report.TopicPartitionOffsetError.Error.IsError)
            {
                _logger.LogError("Backend message {0} topic partition {1} error '{2}'.",
                    message.MessageId, report.TopicPartitionOffsetError.Partition, report.TopicPartitionOffsetError.Error.Reason);
            }
        }
    }
}
