using System;
using GeoCoordinatePortable;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace restAPI.Controllers
{
    public class DeviceCoordinates
    {
        public ulong EUI{get;set;}
        public uint ID {get;set;}
        public IEnumerable<GeoCoordinate> GeoCoordinates {get;set;}
    }
}