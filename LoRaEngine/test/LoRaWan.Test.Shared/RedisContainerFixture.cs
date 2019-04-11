// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Docker.DotNet;
    using Docker.DotNet.Models;
    using Xunit;

    public class RedisContainerFixture : IDisposable
    {
        private const string ContainerName = "redis";
        private const string ImageName = "redis";
        private const string ImageTag = "5.0.4-alpine";
        static string containerId;

        public RedisContainerFixture()
        {
            this.StartRedisContainer().Wait();
        }

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
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Agent.Id")))
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
                    }

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

                    var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                    if (!started)
                    {
                            Assert.False(true, "Cannot start the docker container");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        public void Dispose()
        {
            var dockerConnection = System.Environment.OSVersion.Platform.ToString().Contains("Win") ?
                "npipe://./pipe/docker_engine" :
                "unix:///var/run/docker.sock";
            using (var conf = new DockerClientConfiguration(new Uri(dockerConnection))) // localhost
            using (var client = conf.CreateClient())
            {
                client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters()
                {
                    Force = true
                });
            }
        }
    }
}
