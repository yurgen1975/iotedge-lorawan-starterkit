// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Docker.DotNet;
    using Docker.DotNet.Models;
    using StackExchange.Redis;
    using Xunit;

    /// <summary>
    /// Test fixture for tests using Redis
    /// </summary>
    public class RedisFixture : IAsyncLifetime
    {
        private const string ContainerName = "redis";
        private const string ImageName = "redis";
        private const string ImageTag = "5.0.4-alpine";

        private ConnectionMultiplexer redis;

        static string containerId;

        public IDatabase Database { get; set; }

        private async Task StartRedisContainer()
        {
            try
            {
                var dockerConnection = System.Environment.OSVersion.Platform.ToString().Contains("Win") ?
                    "npipe://./pipe/docker_engine" :
                    "unix:///var/run/docker.sock";
                System.Console.WriteLine("Starting container");
                using (var conf = new DockerClientConfiguration(new Uri(dockerConnection))) // localhost
                using (var client = conf.CreateClient())
                {
                    System.Console.WriteLine("On Premise execution detected");
                    System.Console.WriteLine("Starting container...");
                    var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                    System.Console.WriteLine("listing container...");
                    var container = containers.FirstOrDefault(c => c.Names.Contains("/" + ContainerName));
                    System.Console.WriteLine("Getting first container...");
                    if (container != null)
                    {
                        System.Console.WriteLine("Removing current container...");

                        // remove current container running
                        await client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters()
                        {
                            Force = true
                        });
                    }

                    System.Console.WriteLine("No Container detected");
                    // Download image
                    await client.Images.CreateImageAsync(new ImagesCreateParameters() { FromImage = ImageName, Tag = ImageTag }, new AuthConfig(), new Progress<JSONMessage>());

                    // Create the container
                    var config = new Config()
                    {
                        Hostname = "localhost"
                    };

                    // Configure the ports to expose
                    var hostConfig = new HostConfig()
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "6379/tcp", new List<PortBinding> { new PortBinding { HostIP = "127.0.0.1", HostPort = "6379" } }
                            }
                        }
                    };

                    System.Console.WriteLine("Creating container...");
                    // Create the container
                    var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
                    {
                        Image = ImageName + ":" + ImageTag,
                        Name = ContainerName,
                        Tty = false,
                        HostConfig = hostConfig
                    });
                    containerId = response.ID;

                    System.Console.WriteLine("Starting container...");

                    var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                    if (!started)
                    {
                        Assert.False(true, "Cannot start the docker container");
                    }

                    System.Console.WriteLine("Finish booting sequence container...");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        public async Task InitializeAsync()
        {
            // In this case we run locally
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID")))
            {
                await this.StartRedisContainer();
            }

            var redisConnectionString = "localhost:6379";
            try
            {
                this.redis = ConnectionMultiplexer.Connect(redisConnectionString);
                this.Database = this.redis.GetDatabase();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to connect to redis at '{redisConnectionString}'. If running locally with docker: run 'docker run -d -p 6379:6379 redis'. If running in Azure DevOps: run redis in docker.", ex);
            }
        }

        public async Task DisposeAsync()
        {
            // this.redis?.Dispose();
            // this.redis = null;
            await Task.FromResult(0);

            /*
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID")))
            {
                // we are running locally
                var dockerConnection = System.Environment.OSVersion.Platform.ToString().Contains("Win") ?
                "npipe://./pipe/docker_engine" :
                "unix:///var/run/docker.sock";
                using (var conf = new DockerClientConfiguration(new Uri(dockerConnection))) // localhost
                using (var client = conf.CreateClient())
                {
                    await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters()
                    {
                        Force = true
                    });
                }
            } */
        }
    }
}
