using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GeoCoordinatePortable;
using restAPI.DataContext.Models;
using Microsoft.Extensions.Caching.Memory;
using restAPI.DataContracts;

namespace restAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class DataprepController : ControllerBase
  {
    private DevicePositionContext _dataContext;
    private IMemoryCache _cache;

    public DataprepController(DevicePositionContext dataContext, IMemoryCache memoryCache)
    {
      _dataContext = dataContext;
      _cache = memoryCache;
    }



    // GET api/app
    [HttpGet]
    public ActionResult<string> Get()
    {
      List<DeviceMapPoint> points = new List<DeviceMapPoint> {
                new DeviceMapPoint { RecordId = 1, EUI = 5812678618081591328, ID = 1, Longitude = -122.33389115658, Latitude = 47.5950984468382, TimeStamp = DateTime.Now.AddMinutes(-20)},
                new DeviceMapPoint { RecordId = 2, EUI = 5812678618081591328, ID = 1, Longitude = -122.333408458928, Latitude = 47.5946602521766, TimeStamp = DateTime.Now.AddMinutes(-19)},
                new DeviceMapPoint { RecordId = 3, EUI = 5812678618081591328, ID = 1, Longitude = -122.333983983051, Latitude = 47.5943785536708, TimeStamp = DateTime.Now.AddMinutes(-18)},
                new DeviceMapPoint { RecordId = 4, EUI = 5812678618081591328, ID = 1, Longitude = -122.333519850694, Latitude = 47.5941093736816, TimeStamp = DateTime.Now.AddMinutes(-17)},
                new DeviceMapPoint { RecordId = 5, EUI = 6965600122688438304, ID = 2, Longitude = -122.333454872164, Latitude = 47.595768251722, TimeStamp = DateTime.Now.AddMinutes(-16)},
                new DeviceMapPoint { RecordId = 6, EUI = 6965600122688438304, ID = 2, Longitude = -122.333863308638, Latitude = 47.5966508785309, TimeStamp = DateTime.Now.AddMinutes(-15)},
                new DeviceMapPoint { RecordId = 7, EUI = 6965600122688438304, ID = 2, Longitude = -122.333928287168, Latitude = 47.5970139405561, TimeStamp = DateTime.Now.AddMinutes(-14)},
                new DeviceMapPoint { RecordId = 8, EUI = 6965600122688438304, ID = 2, Longitude = -122.333417741575, Latitude = 47.5979153250375, TimeStamp = DateTime.Now.AddMinutes(-13)},
                new DeviceMapPoint { RecordId = 9, EUI = 7037657716726366240, ID = 3, Longitude = -122.328850679181, Latitude = 47.5926132086414, TimeStamp = DateTime.Now.AddMinutes(-12)},
                new DeviceMapPoint { RecordId = 10, EUI = 7037657716726366240, ID = 3, Longitude = -122.32810806741, Latitude = 47.593026379869, TimeStamp = DateTime.Now.AddMinutes(-11)},
                new DeviceMapPoint { RecordId = 11, EUI = 7037657716726366240, ID = 3, Longitude = -122.327811022702, Latitude = 47.5937838519807, TimeStamp = DateTime.Now.AddMinutes(-10)},
                new DeviceMapPoint { RecordId = 12, EUI = 7037657716726366240, ID = 3, Longitude = -122.328646460944, Latitude = 47.5949356893849, TimeStamp = DateTime.Now.AddMinutes(-9)},
      };

      foreach (var point in points)
      {
        //DeviceMapPoint dmp = new DeviceMapPoint();
        //Fill properties
        _dataContext.DevicePositions.Add(point);
        _dataContext.SaveChanges();
      }
      return "OK";
    }
  }
}