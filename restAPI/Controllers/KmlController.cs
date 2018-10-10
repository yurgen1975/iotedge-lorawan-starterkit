using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GeoCoordinatePortable;
using System.Linq;
using restAPI.DataContext.Models;
using restAPI.DataContracts;

namespace restAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KmlController : ControllerBase
    {
        private DevicePositionContext _dataContext;

        public KmlController(DevicePositionContext dataContext)
        {
            _dataContext = dataContext;
        }
        
        // GET api/klm
        [HttpGet]
        public ActionResult<string> Get()
        {
            var deviceMapPoints = _dataContext.DevicePositions.Select(dp => dp).ToArray();
            IEnumerable<IGrouping<ulong, DeviceMapPoint>> goupedDeviceMapPoints = deviceMapPoints.GroupBy(dmp => dmp.EUI);
            DeviceCoordinates[] deviceCoordinatesArray = goupedDeviceMapPoints.Select(item => new DeviceCoordinates(item.Key, 0, item.Select(gps => new Tuple<double, double>(gps.Latitude, gps.Longitute)))).ToArray();

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

            return klm.ToXml();
        }

        // GET api/klm/52549 - specific device ID
        [HttpGet("{id}")]
        public ActionResult<IEnumerable<GeoCoordinate>> Get(ulong deviceEUI)
        {
            throw new NotImplementedException();
        }
    }
}
