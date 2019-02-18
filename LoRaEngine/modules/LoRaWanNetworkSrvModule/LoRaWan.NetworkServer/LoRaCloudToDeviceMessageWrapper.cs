// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using LoRaTools;
    using LoRaTools.Utils;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    class LoRaCloudToDeviceMessageWrapper : ILoRaCloudToDeviceMessage
    {
        private readonly LoRaDevice loRaDevice;
        private readonly Message message;
        private LoRaCloudToDeviceMessage parseCloudToDeviceMessage;

        public LoRaCloudToDeviceMessageWrapper(LoRaDevice loRaDevice, Message message)
        {
            this.loRaDevice = loRaDevice;
            this.message = message;

            this.ParseMessage();
        }

        /// <summary>
        /// Tries to parse the <see cref="Message.GetBytes"/> to a json representation of <see cref="LoRaCloudToDeviceMessage"/>
        /// </summary>
        private void ParseMessage()
        {
            string json = string.Empty;
            var bytes = this.message.GetBytes();
            if (bytes?.Length > 0)
            {
                json = Encoding.UTF8.GetString(bytes);
                try
                {
                    this.parseCloudToDeviceMessage = JsonConvert.DeserializeObject<LoRaCloudToDeviceMessage>(json);
                }
                catch (JsonReaderException)
                {
                    Logger.Log(this.loRaDevice.DevEUI, $"Could not parse cloud to device message: {json}", LogLevel.Error);
                }
            }
        }

        public byte Fport
        {
            get
            {
                if (this.parseCloudToDeviceMessage != null)
                    return this.parseCloudToDeviceMessage.Fport;

                if (this.message.Properties.TryGetValueCaseInsensitive("fport", out var fPortValue))
                {
                    return byte.Parse(fPortValue);
                }

                return 0;
            }
        }

        public bool Confirmed
        {
            get
            {
                if (this.parseCloudToDeviceMessage != null)
                    return this.parseCloudToDeviceMessage.Confirmed;

                return this.message.Properties.TryGetValueCaseInsensitive("confirmed", out var confirmedValue) && confirmedValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string MessageId => this.parseCloudToDeviceMessage?.MessageId ?? this.message.MessageId;

        public string DevEUI => this.loRaDevice.DevEUI;

        public byte[] GetPayload()
        {
            if (this.parseCloudToDeviceMessage != null)
                return this.parseCloudToDeviceMessage.GetPayload();

            return this.message.GetBytes();
        }

        public IList<GenericMACCommand> MACCommands
        {
            get
            {
                if (this.parseCloudToDeviceMessage != null)
                {
                    return this.parseCloudToDeviceMessage.MACCommands;
                }

                if (this.message.Properties.TryGetValueCaseInsensitive("cidtype", out var cidTypeValue))
                {
                    return new MacCommandHolder(Convert.ToByte(cidTypeValue)).MacCommand;
                }

                return null;
            }
        }

        public Task<bool> CompleteAsync() => this.loRaDevice.CompleteCloudToDeviceMessageAsync(this.message);

        public Task<bool> AbandonAsync() => this.loRaDevice.AbandonCloudToDeviceMessageAsync(this.message);
    }
}
