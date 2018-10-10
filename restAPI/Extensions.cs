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

            using (StringWriter stringWriter = new StringWriter())
            {
                xmlSerialiser.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }
    }
}