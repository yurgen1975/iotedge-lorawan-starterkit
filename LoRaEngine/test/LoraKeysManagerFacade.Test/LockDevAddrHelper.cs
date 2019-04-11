// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Threading.Tasks;

    static class LockDevAddrHelper
    {
        private const string GlobalDevAddrUpdateKey = "globalUpdateKey";

        public static async Task TakeLocksAsync(ILoRaDeviceCacheStore loRaDeviceCache, string[] lockNames)
        {
            if (lockNames?.Length > 0)
            {
                foreach (var locks in lockNames)
                {
                    await loRaDeviceCache.LockTakeAsync(locks, locks, TimeSpan.FromMinutes(3));
                }
            }
        }

        public static void ReleaseLocks(ILoRaDeviceCacheStore loRaDeviceCache, string[] lockNames)
        {
            if (lockNames?.Length > 0)
            {
                foreach (var locks in lockNames)
                {
                    loRaDeviceCache.LockRelease(locks, locks);
                }
            }
        }

        public static async Task PrepareLocksForTests(ILoRaDeviceCacheStore loRaDeviceCache, string[] neededLocksForTestToRun, string[] locksGuideTest)
        {
            LockDevAddrHelper.ReleaseLocks(loRaDeviceCache, neededLocksForTestToRun);
            await LockDevAddrHelper.TakeLocksAsync(loRaDeviceCache, locksGuideTest);
        }
    }
}
