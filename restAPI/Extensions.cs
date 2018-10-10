using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace restAPI
{
    public static class Extensions
    {
        public static string ToXml(this object obj)
        {
            XmlSerializer xmlSerialiser = new XmlSerializer(obj.GetType());
            string xml = string.Empty;

            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                {
                    xmlSerialiser.Serialize(xmlWriter, obj);
                    xml = stringWriter.ToString(); 
                }
            }
            return xml;
        }
    }
}