using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GeoCoordinatePortable;
using System.Linq;
using restAPI.DataContext.Models;
using restAPI.DataContracts;
using Microsoft.Extensions.Caching.Memory;

namespace restAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private static readonly object lockObject;
        static DeviceController()
        {
            lockObject = new object();
        }

        private DevicePositionContext _dataContext;
        private IMemoryCache _cache;

        public DeviceController(DevicePositionContext dataContext, IMemoryCache memoryCache)
        {
            _dataContext = dataContext;
            _cache = memoryCache;
        }

        [HttpGet]
        public ActionResult<Tuple<ulong, uint>[]> Get()
        {
            string cacheKey = CacheKeys.DeviceList;
            if (!_cache.TryGetValue(cacheKey, out Tuple<ulong, uint>[] deviceList))
            {
                lock (lockObject)
                {
                    if (!_cache.TryGetValue(cacheKey, out deviceList))
                    {
                        deviceList = _dataContext.DevicePositions.Select(dmp=>new Tuple<ulong, uint>(dmp.EUI, dmp.ID)).Distinct().ToArray();
                        _cache.Set(cacheKey, deviceList, DateTimeOffset.Now.AddSeconds(Config.CacheDurationSeconds));
                    }
                }
            }
            return deviceList;
        }

        [HttpGet("{id}")]
        public ActionResult<DeviceInfo> Get(ulong deviceEUI)
        {
            string cacheKey = CacheKeys.GetKeyForDeviceDetails(deviceEUI);
            if (!_cache.TryGetValue(cacheKey, out DeviceInfo deviceInfo))
            {
                lock (lockObject)
                {
                    if (!_cache.TryGetValue(cacheKey, out deviceInfo))
                    {
                        deviceInfo = new DeviceInfo() { EUI = deviceEUI, ID = 0, Name = "N/A", Description = "N/A" };
                    }
                }
            }
            return deviceInfo;
        }
    }
}
