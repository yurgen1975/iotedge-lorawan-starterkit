// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Threading.Tasks;

    class LockDevAddrHelper : IDisposable
    {
        private ILoRaDeviceCacheStore loRaDeviceCache;
        private string[] lockNames;

        public async Task TakeLocksAsync(ILoRaDeviceCacheStore loRaDeviceCache, string[] lockNames)
        {
            this.loRaDeviceCache = loRaDeviceCache;
            this.lockNames = lockNames;
            foreach (var locks in lockNames)
            {
               await this.loRaDeviceCache.LockTakeAsync(locks, locks, TimeSpan.FromMinutes(3));
            }
        }

        public void ReleaseLocks(ILoRaDeviceCacheStore loRaDeviceCache, string[] lockNames)
        {
            foreach (var locks in lockNames)
            {
                loRaDeviceCache.LockRelease(locks, locks);
            }
        }

        public void Dispose()
        {
            if (this.lockNames != null)
            {
                foreach (var locks in this.lockNames)
                {
                    this.loRaDeviceCache.LockRelease(locks, locks);
                }
            }
        }
    }
}
