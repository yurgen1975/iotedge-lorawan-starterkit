using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SearchRescue
{
    [DataContract]
    public class GpsMessage
    {
        [DataMember(Name = "time")]
        public object Time { get; set; }
        [DataMember(Name = "tmms")]
        public int Tmms { get; set; }
        [DataMember(Name = "tmst")]
        public int Tmst { get; set; }
        [DataMember(Name = "freq")]
        public float Freq { get; set; }
        [DataMember(Name = "chan")]
        public int Chan { get; set; }
        [DataMember(Name = "rfch")]
        public int Rfch { get; set; }
        [DataMember(Name = "stat")]
        public int Stat { get; set; }
        [DataMember(Name = "modu")]
        public string Modu { get; set; }
        [DataMember(Name = "datr")]
        public string Datr { get; set; }
        [DataMember(Name = "codr")]
        public string Codr { get; set; }
        [DataMember(Name = "rssi")]
        public int Rssi { get; set; }
        [DataMember(Name = "lsnr")]
        public float Lsnr { get; set; }
        [DataMember(Name = "size")]
        public int Size { get; set; }
        [DataMember(Name = "data")]
        public Data Data { get; set; }
        [DataMember(Name = "port")]
        public int Port { get; set; }
        [DataMember(Name = "fcnt")]
        public int Fcnt { get; set; }
        [DataMember(Name = "eui")]
        public string Eui { get; set; }
        [DataMember(Name = "gatewayid")]
        public string GatewayUd { get; set; }
        [DataMember(Name = "edgets")]
        public long Edgets { get; set; }
    }

    [DataContract]
    public class Data
    {
        [DataMember(Name = "coordinateType")]
        public string CoordinateType { get; set; }
        [DataMember(Name = "latitude")]
        public float Latitude { get; set; }
        [DataMember(Name = "longitude")]
        public float Longitude { get; set; }
    }

}
