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
    using Xunit;

    public class DevAddrCacheTest : FunctionTestBase, IClassFixture<RedisContainerFixture>, IClassFixture<RedisFixture>
    {
        private const string PrimaryKey = "ABCDEFGH1234567890";
        private readonly RedisContainerFixture redisContainer;
        private readonly ILoRaDeviceCacheStore cache;

        public DevAddrCacheTest(RedisContainerFixture redisContainer, RedisFixture redis)
        {
            this.redisContainer = redisContainer;
            this.cache = new LoRaDeviceCacheRedisStore(redis.Database);
        }

        private RegistryManager InitRegistryManager(string[] deviceIds)
        {
            var mockRegistryManager = new Mock<RegistryManager>(MockBehavior.Strict);
            var primaryKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(PrimaryKey));
            mockRegistryManager
                .Setup(x => x.GetDeviceAsync(It.IsAny<string>()))
                .ReturnsAsync((string deviceId) => new Device(deviceId) { Authentication = new AuthenticationMechanism() { SymmetricKey = new SymmetricKey() { PrimaryKey = primaryKey } } });

            mockRegistryManager
                .Setup(x => x.GetTwinAsync(It.IsNotNull<string>()))
                .ReturnsAsync((string deviceId) => new Twin(deviceId));

            int numberOfDevices = deviceIds.Length;
            int deviceCount = 0;

            var queryMock = new Mock<IQuery>(MockBehavior.Loose);
            queryMock
                .Setup(x => x.HasMoreResults)
                .Returns(() => (deviceCount < numberOfDevices));

            IEnumerable<Twin> Twins()
            {
                while (deviceCount < numberOfDevices)
                {
                    yield return new Twin(deviceIds[deviceCount++]);
                }
            }

            queryMock
                .Setup(x => x.GetNextAsTwinAsync())
                .ReturnsAsync(Twins());

            mockRegistryManager
                .Setup(x => x.CreateQuery(It.IsAny<string>(), 100))
                .Returns(queryMock.Object);

            return mockRegistryManager.Object;
        }

        [Fact]
        public async void DeviceGetter_OTAA_Join()
        {
            string gatewayId = NewUniqueEUI64();

            string[] managerInput = new string[5]
            {
                NewUniqueEUI64(),
                NewUniqueEUI64(),
                NewUniqueEUI64(),
                NewUniqueEUI64(),
                NewUniqueEUI64()
            };

            var deviceGetter = new DeviceGetter(this.InitRegistryManager(managerInput), this.cache);
            var items = await deviceGetter.GetDeviceList(null, gatewayId, "ABCD", null);
            Assert.Single(items);
        }
    }
}
