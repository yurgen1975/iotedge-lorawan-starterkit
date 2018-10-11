namespace restAPI
{
    public static class CacheKeys
    {
        public static string FullKml { get { return "CacheEntry_FullKml"; } }
        public static string DeviceKmlPrefix { get { return "CacheEntry_DeviceKml_id_"; } }
        public static string GetKeyForDeviceKml(ulong id)
        {
            return string.Concat(DeviceKmlPrefix, id);
        }
    }
}