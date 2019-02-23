// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(LoraKeysManagerFacade.LoraKeysManagerFacadeStartup))]

namespace LoraKeysManagerFacade
{
    using System;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class LoraKeysManagerFacadeStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var iotHubConnectionString = config.GetValue<string>("IoTHubConnectionString");
            if (iotHubConnectionString == null)
            {
                throw new Exception("Missing IoTHubConnectionString in settings");
            }

            builder.Services.AddSingleton<RegistryManager>(RegistryManager.CreateFromConnectionString(iotHubConnectionString));

            var redisConnectionString = config.GetValue<string>("RedisConnectionString");
            builder.Services.AddSingleton<ILoRaDeviceCacheStore>(new LoRaDeviceCacheRedisStore(redisConnectionString));

            builder.Services.AddTransient<CreateEdgeDevice>();
            builder.Services.AddTransient<DeviceGetter>();
            builder.Services.AddTransient<DuplicateMsgCacheCheck>();
        }
    }
}