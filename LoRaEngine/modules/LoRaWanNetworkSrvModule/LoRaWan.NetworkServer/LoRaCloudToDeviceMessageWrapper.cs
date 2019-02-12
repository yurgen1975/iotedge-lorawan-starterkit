// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Threading.Tasks;
    using LoRaTools;
    using LoRaTools.Utils;
    using Microsoft.Azure.Devices.Client;

    class LoRaCloudToDeviceMessageWrapper : ILoRaCloudToDeviceMessage
    {
        private readonly LoRaDevice loRaDevice;
        private readonly Message message;

        public LoRaCloudToDeviceMessageWrapper(LoRaDevice loRaDevice, Message message)
        {
            this.loRaDevice = loRaDevice;
            this.message = message;
        }

        public byte Fport
        {
            get
            {
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
                return this.message.Properties.TryGetValueCaseInsensitive("confirmed", out var confirmedValue) && confirmedValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string MessageId => this.message.MessageId;

        public string DevEUI => this.loRaDevice.DevEUI;

        public byte[] GetPayload() => this.message.GetBytes();

        public Task<bool> CompleteAsync() => this.loRaDevice.CompleteCloudToDeviceMessageAsync(this.message);

        public Task<bool> AbandonAsync() => this.loRaDevice.AbandonCloudToDeviceMessageAsync(this.message);

        public MacCommandHolder GetMacCommands()
        {
            if (this.message.Properties.TryGetValueCaseInsensitive("cidtype", out var cidTypeValue))
            {
                return new MacCommandHolder(Convert.ToByte(cidTypeValue));
            }

            return null;
        }
    }
}
