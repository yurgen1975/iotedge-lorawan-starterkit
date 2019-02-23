// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using LoRaWan.Shared;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using StackExchange.Redis;

    public class DeviceGetter
    {
        private readonly RegistryManager registryManager;

        private readonly ILoRaDeviceCacheStore deviceCacheStore;

        public DeviceGetter(RegistryManager registryManager, ILoRaDeviceCacheStore deviceCacheStore)
        {
            this.deviceCacheStore = deviceCacheStore;
            this.registryManager = registryManager;
        }

        /// <summary>
        /// Entry point function for getting devices
        /// </summary>
        [FunctionName(nameof(GetDevice))]
        public async Task<IActionResult> GetDevice([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger logger)
        {
            try
            {
                VersionValidator.Validate(req);
            }
            catch (IncompatibleVersionException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            // ABP parameters
            string devAddr = req.Query["DevAddr"];

            // OTAA parameters
            string devEUI = req.Query["DevEUI"];
            string devNonce = req.Query["DevNonce"];
            string gatewayId = req.Query["GatewayId"];

            try
            {
                List<IoTHubDeviceInfo> results = await this.GetDeviceList(devEUI, gatewayId, devNonce, devAddr);
                string json = JsonConvert.SerializeObject(results);
                return new OkObjectResult(json);
            }
            catch (DeviceNonceUsedException)
            {
                return new BadRequestObjectResult("UsedDevNonce");
            }
        }

        public async Task<List<IoTHubDeviceInfo>> GetDeviceList(string devEUI, string gatewayId, string devNonce, string devAddr)
        {
            List<IoTHubDeviceInfo> results = new List<IoTHubDeviceInfo>();

            if (devEUI != null)
            {
                // OTAA join
                string cacheKey = devEUI + devNonce;
                using (LoRaDeviceCache deviceCache = new LoRaDeviceCache(this.deviceCacheStore, devEUI, gatewayId, cacheKey))
                {
                    if (deviceCache.TryToLock(cacheKey + "joinlock"))
                    {
                        if (deviceCache.TryGetValue(out _))
                        {
                            throw new DeviceNonceUsedException();
                        }

                        deviceCache.SetValue(devNonce, TimeSpan.FromMinutes(1));

                        Device device = await this.registryManager.GetDeviceAsync(devEUI);

                        if (device != null)
                        {
                            IoTHubDeviceInfo iotHubDeviceInfo = new IoTHubDeviceInfo
                            {
                            DevEUI = devEUI,
                            PrimaryKey = device.Authentication.SymmetricKey.PrimaryKey
                            };
                            results.Add(iotHubDeviceInfo);

                            // clear device FCnt cache after join
                            deviceCache.Delete(devEUI);
                        }
                    }
                }
            }
            else if (devAddr != null)
            {
                // ABP or normal message

                // TODO check for sql injection
                devAddr = devAddr.Replace('\'', ' ');

                IQuery query = this.registryManager.CreateQuery($"SELECT * FROM devices WHERE properties.desired.DevAddr = '{devAddr}' OR properties.reported.DevAddr ='{devAddr}'", 100);
                while (query.HasMoreResults)
                {
                    IEnumerable<Microsoft.Azure.Devices.Shared.Twin> page = await query.GetNextAsTwinAsync();

                    foreach (Microsoft.Azure.Devices.Shared.Twin twin in page)
                    {
                        if (twin.DeviceId != null)
                        {
                            Device device = await this.registryManager.GetDeviceAsync(twin.DeviceId);
                            IoTHubDeviceInfo iotHubDeviceInfo = new IoTHubDeviceInfo
                            {
                                DevAddr = devAddr,
                                DevEUI = twin.DeviceId,
                                PrimaryKey = device.Authentication.SymmetricKey.PrimaryKey
                            };
                            results.Add(iotHubDeviceInfo);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Missing devEUI or devAddr");
            }

            return results;
        }
    }
}