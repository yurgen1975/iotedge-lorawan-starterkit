using System;
using GeoCoordinatePortable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace restAPI.DataContracts
{
    [Table("DeviceMapPoints")]
    public class DeviceMapPoint
    {
        [Key]
        [Column("eui")]
        public ulong EUI { get; set; }
        [Column("id")]
        public uint ID { get; set; }
        [Column("Longitude")]
        public double Longitute { get; set; }
        [Column("Latitude")]
        public double Latitude { get; set; }
        [Column("TimeStamp")]
        public DateTime TimeStamp { get; set; }
    }

    public class DeviceCoordinates
    {
        public DeviceCoordinates(ulong eui, uint id, IEnumerable<Tuple<double,double>> gpsTuples)
        {
            GeoCoordinates = new List<GeoCoordinate>();
            EUI = eui;
            ID = id;
            GeoCoordinates = gpsTuples.Select(gps => new GeoCoordinate(gps.Item1, gps.Item2)).ToArray();
        }
        public ulong EUI { get; set; }
        public uint ID { get; set; }
        public IEnumerable<GeoCoordinate> GeoCoordinates { get; set; }
    }
}