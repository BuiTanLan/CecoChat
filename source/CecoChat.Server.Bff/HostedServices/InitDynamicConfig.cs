﻿using CecoChat.Data.Config.Partitioning;

namespace CecoChat.Server.Bff.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly IPartitioningConfig _partitioningConfig;

    public InitDynamicConfig(IPartitioningConfig partitioningConfig)
    {
        _partitioningConfig = partitioningConfig;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _partitioningConfig.Initialize(new PartitioningConfigUsage
        {
            UseServerAddresses = true
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}