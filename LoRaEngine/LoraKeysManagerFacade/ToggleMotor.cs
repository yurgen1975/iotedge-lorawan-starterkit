// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public static class ToggleMotor
    {
        static ServiceClient serviceClient;
        static object toggleMotorInitLock = new object();

        static void EnsureInitialized(ExecutionContext context)
        {
            if (serviceClient == null)
            {
                lock (toggleMotorInitLock)
                {
                    if (serviceClient == null)
                    {
                        var config = new ConfigurationBuilder()
                          .SetBasePath(context.FunctionAppDirectory)
                          .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .Build();
                        string connectionString = config.GetConnectionString("IoTHubConnectionString");

                        if (connectionString == null)
                        {
                            string errorMsg = "Missing IoTHubConnectionString in settings";
                            throw new Exception(errorMsg);
                        }

                        serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
                    }
                }
            }
        }

        [FunctionName("ToggleMotor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            EnsureInitialized(context);

            // Get state from request body or query string
            string state = req.Query["state"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            state = state ?? data?.name;

            string deviceId = "aaeon-AIOT-IP6801";
            string moduleId = "LoRaWanNetworkSrvModule";
            string payload;

            if (string.Equals(state, "on", StringComparison.CurrentCultureIgnoreCase))
            {
                // Turn motor on
                payload = JsonConvert.SerializeObject(new
                {
                    devEUI = "70B3D5E75E004908",
                    rawPayload = "EQUAEwBVIAE=",
                    fport = 125
                });
            }
            else if (string.Equals(state, "off", StringComparison.CurrentCultureIgnoreCase))
            {
                // Turn motor off
                payload = JsonConvert.SerializeObject(new
                {
                    devEUI = "70B3D5E75E004908",
                    rawPayload = "EQUAEwBVIAA=",
                    fport = 125
                });
            }
            else
            {
                return new BadRequestObjectResult("Please pass a \"state\" on the query string or in the request body with \"on\" or \"off\" to turn the motor on or off.");
            }

            var methodInvocation = new CloudToDeviceMethod("cloudtodevicemessage", TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

            methodInvocation.SetPayloadJson(payload);

            CloudToDeviceMethodResult response = null;

            try
            {
                response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation);
            }
            catch (IotHubException exception)
            {
                log.LogInformation("Invocation failed: " + exception.Message);
                return new BadRequestObjectResult("Invocation failed. Exception: " + exception.Message);
            }

            if (response.Status == 200)
            {
                return (ActionResult)new OkObjectResult($"Turning the motor: {state}. Response status: " + response.Status);
            }
            else
            {
                return new BadRequestObjectResult("Invocation failed: Response status: " + response.Status);
            }
        }
    }
}
