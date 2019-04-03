// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Extensions.Hosting;

    class DevAddressCacheManager : IHostedService
    {
        private readonly RegistryManager registryManager;
        private readonly ILoRaDeviceCacheStore cacheStore;

        public DevAddressCacheManager(RegistryManager registryManager, ILoRaDeviceCacheStore cacheStore)
        {
            this.registryManager = registryManager;
            this.cacheStore = cacheStore;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            LoRaDevAddrCache loRaDevAddrCache = new LoRaDevAddrCache(this.cacheStore);
            // Tocheck should we wait?
            await loRaDevAddrCache.PerformNeededSyncs(this.registryManager);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
