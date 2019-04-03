// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DevicesByDevAddrCacheInfo
    {
        public const string KeyPrefix = "DevicesByDevAddr";

        public string DevEUI { get; set; }

        public string PrimaryKey { get; set; }

        public string GatewayId { get; set; }
    }
}
