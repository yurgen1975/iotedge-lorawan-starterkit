using System.Collections.Generic;

namespace restAPI
{
    internal static class CacheKeys
    {
        internal static string FullKml { get { return "CacheEntry_FullKml"; } }
        internal static string DeviceKmlPrefix { get { return "CacheEntry_DeviceKml_id_"; } }
        internal static string GetKeyForDeviceKml(ulong id)
        {
            return string.Concat(DeviceKmlPrefix, id);
        }
        internal static string DeviceList { get { return "CacheEntry_DeviceList"; } }
        public static string DeviceDetailsPrefix { get { return "CacheEntry_DeviceInfo"; } }
        internal static string GetKeyForDeviceDetails(ulong id)
        {
            return string.Concat(DeviceDetailsPrefix, id);
        }
    }
}