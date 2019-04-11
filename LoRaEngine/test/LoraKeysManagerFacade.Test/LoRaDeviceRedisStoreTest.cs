// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LoRaWan.Test.Shared;
    using Xunit;

    /// <summary>
    /// Class to test interaction with RedisCache
    /// </summary>
    public class LoRaDeviceRedisStoreTest : IClassFixture<RedisContainerFixture>, IClassFixture<RedisFixture>
    {
        private readonly RedisContainerFixture redisContainer;
        private readonly ILoRaDeviceCacheStore cache;

        public LoRaDeviceRedisStoreTest(RedisContainerFixture redisContainer, RedisFixture redis)
        {
            this.redisContainer = redisContainer;
            this.cache = new LoRaDeviceCacheRedisStore(redis.Database);
        }

        [Theory]
        [InlineData("TestHashAreSavedCorrectly", "1", "2")]
        [InlineData("TestHashAreSavedCorrectly2", "1", "2asdad")]
        public void TestHashAreSavedCorrectly(string key, string subkey, string value)
        {
            this.cache.TrySetHashObject(key, subkey, value);
            var keys = this.cache.GetHashObject(key);
            Assert.Single(keys);
            Assert.Equal(subkey, keys[0].Name);
            Assert.Equal(value, keys[0].Value);
        }
    }
}
