// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using LoRaTools;
    using Newtonsoft.Json;

    public class LoRaCloudToDeviceMessage : ILoRaCloudToDeviceMessage
    {
        [JsonProperty("devEUI", NullValueHandling = NullValueHandling.Ignore)]
        public string DevEUI { get; set; }

        [JsonProperty("fport", NullValueHandling = NullValueHandling.Ignore)]
        public byte Fport { get; set; }

        /// <summary>
        /// Payload as base64 string
        /// Use this to send bytes
        /// </summary>
        [JsonProperty("rawPayload", NullValueHandling = NullValueHandling.Ignore)]
        public string RawPayload { get; set; }

        /// <summary>
        /// Payload as string
        /// Use this to send text
        /// </summary>
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public string Payload { get; set; }

        [JsonProperty("confirmed", NullValueHandling = NullValueHandling.Ignore)]
        public bool Confirmed { get; set; }

        [JsonProperty("messageId", NullValueHandling = NullValueHandling.Ignore)]
        public string MessageId { get; set; }

        public Task<bool> AbandonAsync() => Task.FromResult(true);

        public Task<bool> CompleteAsync() => Task.FromResult(true);

        public MacCommandHolder GetMacCommands() => null;

        public byte[] GetPayload()
        {
            if (!string.IsNullOrEmpty(this.Payload))
            {
                return Encoding.UTF8.GetBytes(this.Payload);
            }

            if (!string.IsNullOrEmpty(this.RawPayload))
            {
                try
                {
                    return Convert.FromBase64String(this.RawPayload);
                }
                catch (FormatException)
                {
                    // Invalid base64 string, return empty payload
                }
            }

            return new byte[0];
        }
    }
}
