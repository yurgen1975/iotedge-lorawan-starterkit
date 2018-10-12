using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace SearchRescue
{
    class Program
    {
        private static string ApiUri = "";
        static Program()
        {
            _client = new HttpClient();
        }
        static int counter;

        static double LatFix, LonFix;
        static void Main(string[] args)
        {
            Init().Wait();

            LatFix = LonFix = 0;

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        private static readonly HttpClient _client;

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>


        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                GpsMessage gpsMessage;
                using (MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(messageString)))
                {
                    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(GpsMessage));
                    gpsMessage = (GpsMessage)deserializer.ReadObject(stream);
                }
                if(gpsMessage != null)
                {
                    double lat, lon;
                    if(gpsMessage.Data.CoordinateType.ToLower().Trim() == "full")
                    {
                        LatFix = lat = gpsMessage.Data.Latitude;
                        LonFix = lon = gpsMessage.Data.Latitude;
                    }
                    else
                    {
                        lat = LatFix + gpsMessage.Data.Latitude;
                        lon = LonFix + gpsMessage.Data.Latitude;
                    }
                    dynamic data = new
                    {
                        Latitude =lat,
                        Longitude = lon,
                        UtcDateTime = DateTime.UtcNow,
                        EUI = gpsMessage.Eui,
                        MessageTime = FromUnixTime(gpsMessage.Edgets),
                    };
                    //ToDo send data to REST API
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(data));
                    await _client.PostAsync(ApiUri, content);
                }
            }
            return MessageResponse.Completed;
        }
        public static DateTime FromUnixTime(long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
