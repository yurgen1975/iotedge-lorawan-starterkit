using System;
using GeoCoordinatePortable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace restAPI.DataContracts
{
    [Table("DeviceMapPoints")]
    public class DeviceMapPoint
    {
        [Key]
        [Column("RecordId")]
        public long RecordId { get; set; }
        [Column("eui")]
        public ulong EUI { get; set; }
        [Column("id")]
        public uint ID { get; set; }
        [Column("Longitude")]
        public double Longitude { get; set; }
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

    [DataContract]
    public class DeviceInfo
    {
        [DataMember]
        public ulong EUI {get;set;}
        [DataMember]
        public uint ID {get;set;}
        [DataMember]
        public string Name {get;set;}
        [DataMember]
        public string Description {get;set;}
    }

    [DataContract]
    public class GpsData
    {
        [DataMember(Name = "latitude")]
        public double Latitude {get;set;}
        [DataMember(Name = "longitude")]
        public double Longitude {get;set;}
        [DataMember(Name = "time")]
        public DateTime TimeStamp {get;set;}
        [DataMember(Name = "eui")]
        public ulong EUI { get; set; }
        [DataMember(Name = "id")]
        public uint ID { get; set; }
    }
}