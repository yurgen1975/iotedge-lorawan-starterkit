// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Threading.Tasks;

    static class LockDevAddrHelper
    {
        public static async Task TakeLocksAsync(ILoRaDeviceCacheStore loRaDeviceCache, string[] lockNames)
        {
            foreach (var locks in lockNames)
            {
                await loRaDeviceCache.LockTakeAsync(locks, locks, TimeSpan.FromMinutes(3));
            }
        }

        public static void ReleaseLocks(ILoRaDeviceCacheStore loRaDeviceCache, string[] lockNames)
        {
            foreach (var locks in lockNames)
            {
                loRaDeviceCache.LockRelease(locks, locks);
            }
        }
    }
}
