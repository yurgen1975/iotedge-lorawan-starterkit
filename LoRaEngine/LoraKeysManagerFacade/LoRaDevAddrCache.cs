// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.WebJobs;
    using Newtonsoft.Json;

    public sealed class LoRaDevAddrCache : IDisposable
    {
        public const int CacheUpdateAfterMinutes = 5;
        public const int CacheFullReloadAfterHours = 24;

        private const string CacheKeyLockSuffix = "devAddrlock";
        private const string CacheKeyPrefix = "DevAddrTable:";
        private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);

        public static DateTime LastDeltaReload { get; set; }

        public static DateTime LastFullReload { get; set; }

        private readonly ILoRaDeviceCacheStore cacheStore;
        private readonly string gatewayId;
        private readonly string cacheKey;
        private readonly string devAddr;
        private string devEUI;

        public bool IsLockOwner { get; private set; }

        private string lockKey;

        private static string GenerateKey(string devAddr) => CacheKeyPrefix + devAddr;

        // How do I detect for first boot??? Need to set LastFullReload
        public LoRaDevAddrCache(ILoRaDeviceCacheStore cacheStore, string devAddr, string gatewayId)
        {
            if (string.IsNullOrEmpty(devAddr))
            {
                throw new ArgumentNullException("devAddr");
            }

            if (string.IsNullOrEmpty(gatewayId))
            {
                throw new ArgumentNullException("gatewayId");
            }

            this.cacheStore = cacheStore;
            this.gatewayId = gatewayId;
            this.cacheKey = GenerateKey(devAddr);
            this.devAddr = devAddr;

            // remove gatewayid
            DateTime.TryParse(cacheStore.StringGet(string.Concat("lastfullreload:", gatewayId)), out var lastFullReload);
            LastFullReload = lastFullReload;

            DateTime.TryParse(cacheStore.StringGet(string.Concat("lastdeltareload:", gatewayId)), out var lastDeltaReload);
            LastDeltaReload = lastDeltaReload;
        }

        public async Task<bool> TryToLockAsync(string lockKey = null, bool block = true)
        {
            if (this.IsLockOwner)
            {
                return true;
            }

            var lk = lockKey ?? this.devAddr + CacheKeyLockSuffix;

            if (this.IsLockOwner = await this.cacheStore.LockTakeAsync(lk, this.gatewayId, LockExpiry, block))
            {
                // store the used key
                this.lockKey = lk;
            }

            return this.IsLockOwner;
        }

        public bool HasValue()
        {
            return this.cacheStore.StringGet(this.cacheKey) != null;
        }

        public bool TryGetInfo(out List<DevAddrCacheInfo> info)
        {
            info = null;
            this.EnsureLockOwner();

            info = this.cacheStore.GetObject<List<DevAddrCacheInfo>>(this.cacheKey);
            return info != null;
        }

        // change
        public bool StoreInfo(DevAddrCacheInfo info, bool initialize = false)
        {
            this.EnsureLockOwner();
            this.devEUI = info.DevEUI;

            // move above link
            if (this.TryGetInfo(out var devAddrCacheInfos))
            {
                for (int i = 0; i < devAddrCacheInfos.Count; i++)
                {
                    if (devAddrCacheInfos[i].DevEUI == info.DevEUI)
                    {
                        // in this case this is the same object, we override the value
                        // Maybe I could compare to avoid saving if unnecessary.
                        devAddrCacheInfos[i] = info;
                        return this.cacheStore.StringSet(this.cacheKey, JsonConvert.SerializeObject(devAddrCacheInfos), new TimeSpan(1, 0, 0, 0), initialize);
                    }
                }

                // this mean this devaddr was not yet saved here.
                devAddrCacheInfos.Add(info);
                return this.cacheStore.StringSet(this.cacheKey, JsonConvert.SerializeObject(devAddrCacheInfos), new TimeSpan(1, 0, 0, 0), initialize);
            }
            else
            {
                // nothing is saved on this devAddr entry
                List<DevAddrCacheInfo> newDevAddrCacheInfos = new List<DevAddrCacheInfo>(1)
                {
                    info
                };

                return this.cacheStore.StringSet(this.cacheKey, JsonConvert.SerializeObject(newDevAddrCacheInfos), new TimeSpan(1, 0, 0, 0), initialize);
            }
        }

        public static void RebuildCache(List<DevAddrCacheInfo> devAddrCacheInfos, ILoRaDeviceCacheStore cacheStore, string gatewayId)
        {
            var elementToClear = cacheStore.Scan(CacheKeyPrefix, string.Empty, 10);

            var hasht = new HashSet<string>();
            foreach (var element in elementToClear)
            {
                hasht.Add(element.Name);
            }

            // initiate connection
            // Do we need to lock?
            for (int i = 0; i < devAddrCacheInfos.Count; i++)
            {
                var cacheKey = GenerateKey(devAddrCacheInfos[i].DevAddr);
                cacheStore.ObjectSet(cacheKey, devAddrCacheInfos[i], new TimeSpan(24, 0, 0));
                hasht.Remove(cacheKey);
            }

            foreach (var elementToRemove in hasht)
            {
                cacheStore.KeyDelete(elementToRemove);
            }

            // todo exception?
            cacheStore.StringSet(string.Concat("lastfullreload:", gatewayId), DateTime.UtcNow.ToString(), null);
        }

        private void EnsureLockOwner()
        {
            if (!this.IsLockOwner)
            {
                throw new InvalidOperationException($"Trying to access cache without owning the lock. Device: {this.devEUI} Gateway: {this.gatewayId}");
            }
        }

        private void ReleaseLock()
        {
            if (!this.IsLockOwner)
            {
                return;
            }

            var released = this.cacheStore.LockRelease(this.lockKey, this.gatewayId);
            if (!released)
            {
                throw new InvalidOperationException("failed to release lock");
            }

            this.IsLockOwner = false;
        }

        public void Dispose()
        {
            this.ReleaseLock();
        }
    }
}
