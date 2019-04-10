// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using StackExchange.Redis;

    public sealed class LoRaDevAddrCache
    {
        private const string DeltaUpdateKey = "deltaUpdateKey";
        private const string LastDeltaUpdateKeyValue = "lastDeltaUpdateKeyValue";
        private const string FullUpdateKey = "fullUpdateKey";
        private const string GlobalDevAddrUpdateKey = "globalUpdateKey";
        private const string CacheKeyPrefix = "devAddrTable:";
        private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan FullUpdateKeyTimeSpan = TimeSpan.FromHours(25);
        private static readonly TimeSpan DeltaUpdateKeyTimeSpan = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan GlobalDevAddrUpdateKeyTimeSpan = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan DevAddrObjectsTTL = TimeSpan.FromHours(24);

        private readonly ILoRaDeviceCacheStore cacheStore;
        private readonly ILogger logger;
        private readonly string gatewayId;
        private readonly string cacheKey;
        private readonly string devAddr;
        private string devEUI;

        private static string GenerateKey(string devAddr) => CacheKeyPrefix + devAddr;

        public LoRaDevAddrCache(ILoRaDeviceCacheStore cacheStore, string devAddr, string gatewayId, RegistryManager registryManager, ILogger logger)
        {
            if (string.IsNullOrEmpty(devAddr))
            {
                throw new ArgumentNullException("devAddr");
            }

            this.cacheStore = cacheStore;
            this.gatewayId = gatewayId;
            this.cacheKey = GenerateKey(devAddr);
            this.devAddr = devAddr;
            this.logger = logger;

            // perform the necessary syncs
            _ = this.PerformNeededSyncs(registryManager);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoRaDevAddrCache"/> class.
        /// This constructor is only used by the DI Manager. Use the other constructore for general usage.
        /// </summary>
        public LoRaDevAddrCache(ILoRaDeviceCacheStore cacheStore, ILogger logger)
        {
            this.cacheStore = cacheStore;
            this.logger = logger;
        }

        public bool HasValue()
        {
            return this.cacheStore.StringGet(this.cacheKey) != null;
        }

        public bool TryGetInfo(out List<DevAddrCacheInfo> info)
        {
            info = new List<DevAddrCacheInfo>();

            var tmp = this.cacheStore.TryGetHashObject(this.cacheKey);
            if (tmp?.Length > 0)
            {
                foreach (var tm in tmp)
                {
                    info.Add(JsonConvert.DeserializeObject<DevAddrCacheInfo>(tm.Value));
                }
            }

            return info.Count > 0;
        }

        // change
        public bool StoreInfo(DevAddrCacheInfo info, bool initialize = false, string cacheKey = "")
        {
            this.devEUI = info.DevEUI;
            if (string.IsNullOrEmpty(cacheKey))
            {
                return this.cacheStore.TrySetHashObject(this.cacheKey, info.DevEUI, JsonConvert.SerializeObject(info));
            }

            return this.cacheStore.TrySetHashObject(GenerateKey(cacheKey), info.DevEUI, JsonConvert.SerializeObject(info));
        }

        internal async Task PerformNeededSyncs(RegistryManager registryManager)
        {
            if (await this.cacheStore.LockTakeAsync(FullUpdateKey, FullUpdateKey, FullUpdateKeyTimeSpan))
            {
                var ownGlobalLock = false;
                try
                {
                    // if a full update is needed I take the global lock and perform a full reload
                    if (!await this.cacheStore.LockTakeAsync(GlobalDevAddrUpdateKey, GlobalDevAddrUpdateKey, GlobalDevAddrUpdateKeyTimeSpan, true))
                    {
                        // should that really be a exception, this is somehow expected?
                        throw new Exception("Failed to lock the global dev addr update key");
                    }

                    ownGlobalLock = true;
                    await this.PerformFullReload(registryManager);
                    // if successfull i set the delta lock to 5 minutes and release the global lock
                    this.cacheStore.StringSet(FullUpdateKey, DateTime.UtcNow.ToString(), FullUpdateKeyTimeSpan);
                    this.cacheStore.StringSet(DeltaUpdateKey, DeltaUpdateKey, DeltaUpdateKeyTimeSpan);
                }
                catch (Exception)
                {
                    // there was a problem, to deal with iot hub throttling we add some time.
                    this.cacheStore.ChangeLockTTL(FullUpdateKey, timeToExpire: TimeSpan.FromMinutes(1)); // on 24
                }
                finally
                {
                    if (ownGlobalLock)
                    {
                        this.cacheStore.LockRelease(GlobalDevAddrUpdateKey, GlobalDevAddrUpdateKey);
                    }
                }
            }
            else if (await this.cacheStore.LockTakeAsync(GlobalDevAddrUpdateKey, GlobalDevAddrUpdateKey, TimeSpan.FromMinutes(5)))
            {
                try
                {
                    if (await this.cacheStore.LockTakeAsync(DeltaUpdateKey, DeltaUpdateKey, TimeSpan.FromMinutes(5)))
                    {
                        await this.PerformDeltaReload(registryManager);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    this.cacheStore.LockRelease(GlobalDevAddrUpdateKey, GlobalDevAddrUpdateKey);
                }
            }
        }

        /// <summary>
        /// Perform a full relaoad on the dev address cache. This occur typically once every 24 h.
        /// </summary>
        private async Task PerformFullReload(RegistryManager registryManager)
        {
            var query = $"SELECT * FROM devices WHERE is_defined(properties.desired.AppKey) OR is_defined(properties.desired.AppSKey) OR is_defined(properties.desired.NwkSKey)";
            List<DevAddrCacheInfo> devAddrCacheInfos = await this.GetDeviceTwinsFromIotHub(registryManager, query);
            this.BulkSaveDevAddrCache(devAddrCacheInfos, true);
        }

        /// <summary>
        /// Method performing a deltaReload. Typically occur every 5 minutes.
        /// </summary>
        private async Task PerformDeltaReload(RegistryManager registryManager)
        {
            // if the value is null (first call), we take five minutes before this call
            var lastUpdate = this.cacheStore.StringGet(LastDeltaUpdateKeyValue) ?? DateTime.UtcNow.AddMinutes(-5).ToString();
            var query = $"SELECT * FROM c where properties.desired.$metadata.$lastUpdated >= '{lastUpdate}' OR properties.reported.$metadata.DevAddr.$lastUpdated >= '{lastUpdate}'";
            List<DevAddrCacheInfo> devAddrCacheInfos = await this.GetDeviceTwinsFromIotHub(registryManager, query);
            this.BulkSaveDevAddrCache(devAddrCacheInfos, false);
        }

        private async Task<List<DevAddrCacheInfo>> GetDeviceTwinsFromIotHub(RegistryManager registryManager, string inputQuery)
        {
            var query = registryManager.CreateQuery(inputQuery);
            this.cacheStore.StringSet(LastDeltaUpdateKeyValue, DateTime.UtcNow.ToString(), TimeSpan.FromDays(1));
            List<DevAddrCacheInfo> devAddrCacheInfos = new List<DevAddrCacheInfo>();
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();

                foreach (var twin in page)
                {
                    if (twin.DeviceId != null)
                    {
                        string currentDevAddr;
                        if (twin.Properties.Desired.Contains("DevAddr"))
                        {
                            currentDevAddr = twin.Properties.Desired["DevAddr"].Value as string;
                        }
                        else if (twin.Properties.Reported.Contains("DevAddr"))
                        {
                            currentDevAddr = twin.Properties.Reported["DevAddr"].Value as string;
                        }
                        else
                        {
                            continue;
                        }

                        devAddrCacheInfos.Add(new DevAddrCacheInfo()
                        {
                            DevAddr = currentDevAddr,
                            DevEUI = twin.DeviceId,
                            GatewayId = twin.Properties.Desired.Contains("GatewayId") ? twin.Properties.Desired["GatewayId"].Value as string : string.Empty
                        });
                    }
                }
            }

            return devAddrCacheInfos;
        }

        /// <summary>
        /// Method to bulk save a devAddrCacheInfo list in redis in a call per devAddr
        /// </summary>
        /// <param name="canDeleteDeviceWithDevAddr"> Should delete all other elements non present in this list?</param>
        private void BulkSaveDevAddrCache(List<DevAddrCacheInfo> devAddrCacheInfos, bool canDeleteDeviceWithDevAddr)
        {
            // elements will naturally expire we only need to add new ones
            var regrouping = devAddrCacheInfos.GroupBy(x => x.DevAddr);
            foreach (var elementPerDevAddr in regrouping)
            {
                var cacheKey = GenerateKey(elementPerDevAddr.Key);
                var currentDevAddrEntry = this.cacheStore.TryGetHashObject(cacheKey);
                var devicesByDevEui = this.KeepExistingCacheInformation(currentDevAddrEntry, elementPerDevAddr, canDeleteDeviceWithDevAddr);
                if (devicesByDevEui != null)
                {
                    this.cacheStore.ReplaceHashObjects(cacheKey, devicesByDevEui, DevAddrObjectsTTL, canDeleteDeviceWithDevAddr);
                }
            }
        }

        /// <summary>
        /// Method to make sure we keep information currently available in the cache and we don't perform unnessecary updates.
        /// </summary>
        private Dictionary<string, DevAddrCacheInfo> KeepExistingCacheInformation(HashEntry[] cacheDevEUIEntry, IGrouping<string, DevAddrCacheInfo> newDevEUIList, bool canDeleteExistingDevice)
        {
            // if the new value are not different we want to ensure we don't save, to not update the TTL of the item.
            bool areNewValuesDifferent = false;
            var toSyncValues = newDevEUIList.ToDictionary(x => x.DevEUI);
            var cacheValues = new Dictionary<string, DevAddrCacheInfo>();

            foreach (var devEUIEntry in cacheDevEUIEntry)
            {
                cacheValues.Add(devEUIEntry.Name, JsonConvert.DeserializeObject<DevAddrCacheInfo>(devEUIEntry.Value));
            }

            // If nothing is in the cache we want to return the new values.
            if (cacheValues.Count == 0)
            {
                return toSyncValues;
            }

            // if we can delete existing devices in the devadr cache, we take the new list as base, otherwise we take the old one.
            if (canDeleteExistingDevice)
            {
                return this.MergeOldAndNewChanges(ref areNewValuesDifferent, toSyncValues, cacheValues, canDeleteExistingDevice);
            }
            else
            {
                return this.MergeOldAndNewChanges(ref areNewValuesDifferent, cacheValues, toSyncValues, canDeleteExistingDevice);
            }
        }

        // In the end we simply need to update the gateway and the Primary key. The DEVEUI and DevAddr can't be updated.
        private Dictionary<string, DevAddrCacheInfo> MergeOldAndNewChanges(ref bool isSaveRequired, Dictionary<string, DevAddrCacheInfo> valueArrayBase, Dictionary<string, DevAddrCacheInfo> valueArrayimport, bool shouldImportFromNewValues)
        {
            foreach (var baseValue in valueArrayBase)
            {
                if (valueArrayimport.ContainsKey(baseValue.Key))
                {
                    if (!baseValue.Value.IsEqual(valueArrayimport[baseValue.Key]))
                    {
                        // the item is different we need to trigger a save of the object
                        isSaveRequired = true;
                    }

                    if (shouldImportFromNewValues)
                    {
                        // In this case (FullUpdate) we are taking new values as base, we only want to keep the primary key if it was not empty.
                        // In our current update strategy the device key will always be null at this point.
                        if (!string.IsNullOrEmpty(valueArrayimport[baseValue.Key].PrimaryKey))
                        {
                            baseValue.Value.PrimaryKey = valueArrayimport[baseValue.Key].PrimaryKey;
                        }
                    }
                    else
                    {
                        // In this case (delta update). We are taking old value as base. We want to make sure to update the gateway Id as this is the only parameter that could change.
                        baseValue.Value.GatewayId = valueArrayimport[baseValue.Key].GatewayId;
                    }

                    // I remove the key from the import
                    valueArrayimport.Remove(baseValue.Key);
                }
                else
                {
                    // there is an additional item, we need to save
                    isSaveRequired = true;
                }
            }

            if (!shouldImportFromNewValues)
            {
                // In this case we want to make sure we import any new value that were not contained in the old cache information
                foreach (var remainingElementToImport in valueArrayimport)
                {
                    valueArrayBase.Add(remainingElementToImport.Value.DevEUI, remainingElementToImport.Value);
                }
            }

            // If no changes are required we return null to avoid saving and updating the expiry on the cache.
            return isSaveRequired ? valueArrayBase : null;
        }
    }
    }