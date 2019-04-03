// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DevicesByDevEUICacheInfo
    {
        public const string KeyPrefix = "DevicesByDevEUI";

        public string PrimaryKey { get; set; }

        public string GatewayId { get; set; }
    }
}
