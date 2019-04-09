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
        private const string ImageTag = "5.0.4";
        static string containerId;

        public RedisContainerFixture()
        {
            this.StartRedisContainer().Wait();
        }

        private async Task StartRedisContainer()
        {
            System.Console.WriteLine("Starting container");
            using (var conf = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))) // localhost
            using (var client = conf.CreateClient())
            {
                System.Console.WriteLine("Starting container...");

                var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                var container = containers.FirstOrDefault(c => c.Names.Contains("/" + ContainerName));
                if (container == null)
                {
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

                    // Create the container
                    var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
                    {
                        Image = ImageName + ":" + ImageTag,
                        Name = ContainerName,
                        Tty = false,
                        HostConfig = hostConfig,
                    });
                    containerId = response.ID;

                    // Get the container object
                    containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                    container = containers.First(c => c.ID == response.ID);
                }

                // Start the container is needed
                if (container.State != "running")
                {
                    var started = await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                    if (!started)
                    {
                        Assert.False(true, "Cannot start the docker container");
                    }
                }
            }
        }

        [Fact]
        public async Task EnsureRedisIsRunning()
        {
            using (var httpClient = new HttpClient())
            {
                while (true)
                {
                    try
                    {
                        using (var response = await httpClient.GetAsync("http://localhost:8080"))
                        {
                            if (response.IsSuccessStatusCode)
                                break;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void Dispose()
        {
            using (var conf = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"))) // localhost
            using (var client = conf.CreateClient())
            {
                client.Containers.RemoveContainerAsync(containerId, null);
            }
        }
    }
}
