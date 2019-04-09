// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using LoRaWan.Test.Shared;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Shared;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class DevAddrCacheTest : FunctionTestBase, IClassFixture<RedisContainerFixture>, IClassFixture<RedisFixture>
    {
        private const string FullUpdateKey = "fullUpdateKey";
        private const string GlobalDevAddrUpdateKey = "globalUpdateKey";
        private const string DeltaUpdateKey = "deltaUpdateKey";
        private const string CacheKeyPrefix = "devAddrTable:";

        private const string PrimaryKey = "ABCDEFGH1234567890";
        private readonly RedisContainerFixture redisContainer;
        private readonly ILoRaDeviceCacheStore cache;

        public DevAddrCacheTest(RedisContainerFixture redisContainer, RedisFixture redis)
        {
            this.redisContainer = redisContainer;
            this.cache = new LoRaDeviceCacheRedisStore(redis.Database);
        }

        private Mock<RegistryManager> InitRegistryManager(List<DevAddrCacheInfo> deviceIds)
        {
            string currentDevAddr = string.Empty;
            List<DevAddrCacheInfo> currentDevices = deviceIds;
            var mockRegistryManager = new Mock<RegistryManager>(MockBehavior.Strict);
            var primaryKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(PrimaryKey));
            mockRegistryManager
                .Setup(x => x.GetDeviceAsync(It.IsAny<string>()))
                .ReturnsAsync((string deviceId) => new Device(deviceId) { Authentication = new AuthenticationMechanism() { SymmetricKey = new SymmetricKey() { PrimaryKey = primaryKey } } });

            mockRegistryManager
                .Setup(x => x.GetTwinAsync(It.IsNotNull<string>()))
                .ReturnsAsync((string deviceId) => new Twin(deviceId));

            int numberOfDevices = deviceIds.Count;
            int deviceCount = 0;

            // CacheMiss query
            var cacheMissQueryMock = new Mock<IQuery>(MockBehavior.Strict);
            cacheMissQueryMock
                .Setup(x => x.HasMoreResults)
                .Returns(() =>
                {
                    return deviceCount++ < currentDevices.Where(z => z.DevAddr == currentDevAddr).Count();
                });

            cacheMissQueryMock
                .Setup(x => x.GetNextAsTwinAsync())
                .ReturnsAsync(() =>
                {
                    var devAddressesToConsider = currentDevices.Where(v => v.DevAddr == currentDevAddr);
                    List<Twin> twins = new List<Twin>();
                    foreach (var devaddrItem in devAddressesToConsider)
                    {
                        var deviceTwin = new Twin();
                        deviceTwin.DeviceId = devaddrItem.DevEUI;
                        deviceTwin.Properties = new TwinProperties()
                        {
                            Desired = new TwinCollection($"{{\"DevAddr\": \"{devaddrItem.DevAddr}\", \"GatewayId\": \"{devaddrItem.GatewayId}\"}}"),
                        };

                        twins.Add(deviceTwin);
                    }
                    return twins;
                });

            mockRegistryManager
                .Setup(x => x.CreateQuery(It.Is<string>(z => z.Contains("SELECT * FROM devices WHERE properties.desired.DevAddr =")), 100))
                .Returns((string query, int pageSize) =>
                {
                    currentDevAddr = query.Split('\'')[1];
                    return cacheMissQueryMock.Object;
                });

            return mockRegistryManager;
        }

        private void InitCache(ILoRaDeviceCacheStore cache, List<DevAddrCacheInfo> deviceIds)
        {
            var loradevaddrcache = new LoRaDevAddrCache(cache, null);
            foreach (var device in deviceIds)
            {
                loradevaddrcache.StoreInfo(device, cacheKey: device.DevAddr);
            }
        }

        [Fact]
        public async void When_DevAddr_Is_Not_In_Cache_Query_Iot_Hub_And_Save_In_Cache()
        {
            string gatewayId = NewUniqueEUI64();

            using (LockDevAddrHelper lockManager = new LockDevAddrHelper())
            {
                await lockManager.TakeLocksAsync(this.cache, new string[2] { FullUpdateKey, DeltaUpdateKey });
                List<DevAddrCacheInfo> managerInput = new List<DevAddrCacheInfo>();

                for (int i = 0; i < 2; i++)
                {
                    managerInput.Add(new DevAddrCacheInfo()
                    {
                        DevEUI = NewUniqueEUI64(),
                        DevAddr = NewUniqueEUI32()
                    });
                }

                var devAddrJoining = managerInput[0].DevAddr;
                var registryManagerMock = this.InitRegistryManager(managerInput);
                var deviceGetter = new DeviceGetter(registryManagerMock.Object, this.cache);
                var items = await deviceGetter.GetDeviceList(null, gatewayId, "ABCD", devAddrJoining);
                Assert.Single(items);
                // If a cache miss it should save it in the redisCache
                var devAddrcache = new LoRaDevAddrCache(this.cache, null);
                var queryResult = this.cache.TryGetHashObject(string.Concat(CacheKeyPrefix, devAddrJoining));
                Assert.Single(queryResult);
                var resultObject = JsonConvert.DeserializeObject<DevAddrCacheInfo>(queryResult[0].Value);
                Assert.Equal(managerInput[0].DevAddr, resultObject.DevAddr);
                Assert.Equal(managerInput[0].GatewayId, resultObject.GatewayId);
                Assert.Equal(managerInput[0].DevEUI, resultObject.DevEUI);
            }
        }

        [Fact]
        public async void When_DevAddr_Is_In_Cache_Should_Not_Query_Iot_Hub()
        {
            string gatewayId = NewUniqueEUI64();

            using (LockDevAddrHelper lockManager = new LockDevAddrHelper())
            {
                await lockManager.TakeLocksAsync(this.cache, new string[2] { FullUpdateKey, DeltaUpdateKey });
                List<DevAddrCacheInfo> managerInput = new List<DevAddrCacheInfo>();
                for (int i = 0; i < 2; i++)
                {
                    managerInput.Add(new DevAddrCacheInfo()
                    {
                        DevEUI = NewUniqueEUI64(),
                        DevAddr = NewUniqueEUI32(),
                        GatewayId = gatewayId
                    });
                }

                var devAddrJoining = managerInput[0].DevAddr;
                this.InitCache(this.cache, managerInput);
                var registryManagerMock = this.InitRegistryManager(managerInput);

                var deviceGetter = new DeviceGetter(registryManagerMock.Object, this.cache);
                var items = await deviceGetter.GetDeviceList(null, gatewayId, "ABCD", devAddrJoining);
                Assert.Single(items);
                // Iot hub should never have been called.
                registryManagerMock.Verify(x => x.CreateQuery(It.IsAny<string>()), Times.Never, "IoT Hub should not have been called, as the device was present in the cache.");
                registryManagerMock.Verify(x => x.GetTwinAsync(It.IsAny<string>()), Times.Never, "IoT Hub should not have been called, as the device was present in the cache.");
            }
        }
    }
}
