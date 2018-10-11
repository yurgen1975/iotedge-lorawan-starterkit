
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace functions
{
    public static class retrieveandpush
    {
        /* Azure Function: retrieveandpush
         * 
         * Connection strings needed:
         *      IoTHubConnectionString
         * EnvironmentVariables needed:
         *      ModuleId
         *      
         */

        [FunctionName("retrieveandpush")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("RetrieveAndPush function started");

            var config = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();

            string IoTHubConnectionString = config.GetConnectionString("IoTHubConnectionString");
            string ModuleId = config.GetValue<string>("ModuleId");
            if (IoTHubConnectionString == null || ModuleId == null)
            {
                string errorMessage = "Missing environment variables or connection string";
                return (ActionResult)new ForbidResult(errorMessage);
            }

            string responseMessage = "";

            List<EdgeDevice> edgeDevices = new List<EdgeDevice>();
            Dictionary<string, string> leafDevices = new Dictionary<string, string>();

            //1) Instantiate registryManager for registry read/write ops
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(IoTHubConnectionString);

            //2) Retrieve all the IoT Edge Devices (capability iotedge set true)
            var queryEdgeDevices = registryManager.CreateQuery($"SELECT * FROM devices WHERE capabilities.iotEdge = true");
            while (queryEdgeDevices.HasMoreResults)
            {
                var page = await queryEdgeDevices.GetNextAsTwinAsync();
                foreach (var twin in page)
                {
                    var device = await registryManager.GetDeviceAsync(twin.DeviceId);
                    edgeDevices.Add(new EdgeDevice() { DeviceId = device.Id, GenerationId = device.GenerationId, ETag = twin.ETag });
                }
            }

            //3) Retrieve all leaf devices associated to each gateway
            foreach (var edgeDevice in edgeDevices)
            {
                leafDevices = new Dictionary<string, string>();
                var queryLeafDevices = registryManager.CreateQuery($"SELECT * FROM devices WHERE capabilities.iotEdge = false AND deviceScope = 'ms-azure-iot-edge://{edgeDevice.DeviceId}-{edgeDevice.GenerationId}'");
                while (queryLeafDevices.HasMoreResults)
                {
                    var page = await queryLeafDevices.GetNextAsTwinAsync();

                    try
                    {
                        foreach (var twin in page)
                        {
                            var device = await registryManager.GetDeviceAsync(twin.DeviceId);
                            leafDevices.Add(device.Id, device.Authentication.SymmetricKey.PrimaryKey);
                        }
                    }
                    catch (Exception)
                    {
                        //TODO Daniele: will fail if no leaf devices are associated to the found edge device
                    }
                }
                //4) Update the Edge Module Twins with the list of leaf devices
                try
                {
                    var moduleETag = registryManager.GetTwinAsync(edgeDevice.DeviceId, ModuleId).Result.ETag;
                    var desiredProperties = "{\"properties\": {\"desired\": { \"LeafDevices\": " + JsonConvert.SerializeObject(leafDevices) + " } } }";

                    await registryManager.UpdateTwinAsync(edgeDevice.DeviceId, ModuleId, desiredProperties, moduleETag);
                }
                catch (Exception)
                {
                    //TODO Daniele: will throw exception when there is not any ${ModuleId} on that specified ${DeviceId}
                }
                responseMessage += $"Module Twin for {edgeDevice.DeviceId} updated with {leafDevices.Count} leaf devices\n";
            }

            return (ActionResult)new OkObjectResult(responseMessage);
        }
    }
}
