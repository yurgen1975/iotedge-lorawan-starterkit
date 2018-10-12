using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GeoCoordinatePortable;
using System.Linq;
using restAPI.DataContext.Models;
using restAPI.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace restAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataHelperController : ControllerBase
    {
        private DevicePositionContext _dataContext;
        private IMemoryCache _cache;

        public DataHelperController(DevicePositionContext dataContext, IMemoryCache memoryCache)
        {
            _dataContext = dataContext;
            _cache = memoryCache;
        }

        [HttpPost("Create")]        
        public ActionResult<long> Create([FromForm] GpsData gpsData)
        {
            //ToDo Automapper
            DeviceMapPoint dmp = new DeviceMapPoint();
            dmp.EUI=gpsData.EUI;
            dmp.ID=gpsData.ID;
            dmp.TimeStamp = gpsData.TimeStamp;
            dmp.Latitude = gpsData.Latitude;
            dmp.Longitude = gpsData.Longitude;
            _dataContext.DevicePositions.Add(dmp);
            _dataContext.SaveChanges();
            return 0;
        }
    }
}
