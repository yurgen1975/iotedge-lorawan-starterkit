using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using GeoCoordinatePortable;

namespace restAPI.DataContracts
{
    [System.Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName ="kml", IsNullable = false)]
    public class Kml
    {
        public Kml()
        {
            Document = new KmlDocument();
        }

        public void AddPlacemark(DeviceCoordinates deviceCoordinates)
        {
            KmlDocumentPlacemark placemark = new KmlDocumentPlacemark();
            placemark.Name = deviceCoordinates.EUI.ToString();
            placemark.Description = deviceCoordinates.ID.ToString();
            placemark.AddPoins(deviceCoordinates.GeoCoordinates);
            Document.AddPlacemark(placemark);

        }
        [XmlElement(ElementName = "Document")]
        public KmlDocument Document { get; }
    }

    [System.Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class KmlDocument
    {
        public KmlDocument()
        {
            Placemark = new KmlDocumentPlacemark[0];
            Name = Description = "Device position list";
        }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement("Placemark")]
        [XmlElement(ElementName = "Placemark")]
        public KmlDocumentPlacemark[] Placemark { get; private set; }

        internal void AddPlacemark(KmlDocumentPlacemark placemark)
        {
            Placemark = Placemark.Concat(new[] { placemark }).ToArray();
        }
    }

    /// <remarks/>
    [System.Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class KmlDocumentPlacemark
    {
        public KmlDocumentPlacemark()
        {
            LineString = new KmlDocumentPlacemarkLineString();
        }
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "LineString")]
        public KmlDocumentPlacemarkLineString LineString { get; internal set; }

        internal void AddPoins(IEnumerable<GeoCoordinate> geoCoordinates)
        {
            LineString.Coordinates = string.Concat(LineString.Coordinates, Environment.NewLine,
                geoCoordinates.Select(c => string.Concat(c.Latitude, ",", c.Longitude, ",0")).Aggregate((current, next) => string.Concat(current, Environment.NewLine, next)));
        }
    }

    /// <remarks/>
    [System.Serializable()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class KmlDocumentPlacemarkLineString
    {
        [XmlElement(ElementName = "coordinates")]
        public string Coordinates { get; set; }
    }
}