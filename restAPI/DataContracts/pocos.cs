using System;
using GeoCoordinatePortable;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace restAPI.Controllers
{
    public class DeviceCoordinates
    {
        public Guid Id{get;set;}
        public string Name {get;set;}
        public IEnumerable<GeoCoordinate> GeoCoordinates {get;set;}
    }
}