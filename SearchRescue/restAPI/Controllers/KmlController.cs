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
    public class KmlController : ControllerBase
    {
        private static readonly object lockObject;
        static KmlController()
        {
            lockObject = new object();
        }

        private DevicePositionContext _dataContext;
        private IMemoryCache _cache;

        public KmlController(DevicePositionContext dataContext, IMemoryCache memoryCache)
        {
            _dataContext = dataContext;
            _cache = memoryCache;
        }

        // GET api/klm
        [HttpGet]
        public ActionResult<string> Get()
        {
            string cacheKey = CacheKeys.FullKml;
            if (!_cache.TryGetValue(cacheKey, out string kmlFile))
            {
                lock (lockObject)
                {
                    if (!_cache.TryGetValue(cacheKey, out kmlFile))
                    {
                        var deviceMapPoints = _dataContext.DevicePositions.ToArray();
                        IEnumerable<IGrouping<ulong, DeviceMapPoint>> goupedDeviceMapPoints = deviceMapPoints.GroupBy(dmp => dmp.EUI);
                        DeviceCoordinates[] deviceCoordinatesArray = goupedDeviceMapPoints.Select(item => new DeviceCoordinates(item.Key, 0, item.Select(gps => new Tuple<double, double>(gps.Latitude, gps.Longitude)))).ToArray();

                        Kml klm = new Kml();
                        foreach (DeviceCoordinates deviceCoordinates in deviceCoordinatesArray)
                        {
                            KmlDocumentPlacemark placemark = new KmlDocumentPlacemark();
                            placemark.Name = deviceCoordinates.EUI.ToString();
                            placemark.Description = deviceCoordinates.ID.ToString();
                            klm.Document.AddPlacemark(placemark);
                            IEnumerable<GeoCoordinate> coordinates = deviceCoordinates.GeoCoordinates.Select(gps => new GeoCoordinate(gps.Latitude, gps.Longitude));
                            placemark.AddPoins(coordinates);
                        }
                        kmlFile = klm.ToXml();
                        _cache.Set(cacheKey, kmlFile, DateTimeOffset.Now.AddSeconds(Config.CacheDurationSeconds));
                    }
                }
            }
            return kmlFile;
        }

        // GET api/klm/52549 - specific device ID
        [HttpGet("{id}")]
        public ActionResult<string> Get(ulong deviceEUI)
        {
            string cacheKey = CacheKeys.GetKeyForDeviceKml(deviceEUI);
            if (!_cache.TryGetValue(cacheKey, out string kmlFile))
            {
                lock (lockObject)
                {
                    if (!_cache.TryGetValue(cacheKey, out kmlFile))
                    {
                        var deviceMapPoints = _dataContext.DevicePositions.Where(dmp => dmp.EUI == deviceEUI).ToArray();
                        DeviceCoordinates deviceCoordinates = new DeviceCoordinates(deviceEUI, 0, deviceMapPoints.Select(gps => new Tuple<double, double>(gps.Latitude, gps.Longitude)));

                        Kml klm = new Kml();

                        KmlDocumentPlacemark placemark = new KmlDocumentPlacemark();
                        placemark.Name = deviceEUI.ToString();
                        placemark.Description = deviceCoordinates.ID.ToString();
                        klm.Document.AddPlacemark(placemark);
                        IEnumerable<GeoCoordinate> coordinates = deviceCoordinates.GeoCoordinates.Select(gps => new GeoCoordinate(gps.Latitude, gps.Longitude));
                        placemark.AddPoins(coordinates);

                        kmlFile = klm.ToXml();
                        _cache.Set(cacheKey, kmlFile, DateTimeOffset.Now.AddSeconds(Config.CacheDurationSeconds));
                    }
                }
            }
            return kmlFile;
        }
    }
}
