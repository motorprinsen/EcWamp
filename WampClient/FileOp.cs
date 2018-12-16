using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WampClient
{
  
    public static class Xml
    {
        public static T Deserialize<T>(string input) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader sr = new StringReader(input))
            {
                return (T)serializer.Deserialize(sr);
            }
        }

        public static string Serialize<T>(T ObjectToSerialize)
        {
            XmlSerializer serializer = new XmlSerializer(ObjectToSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                serializer.Serialize(textWriter, ObjectToSerialize);
                return textWriter.ToString();
            }
        }
    }
    [XmlRoot(ElementName = "UserArea")]
    public class ExoUserAreaImpl
    {
        [XmlElement(ElementName = "DefaultController")]
        public string DefaultController { get; set; }

        [XmlElement(ElementName = "Description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "Icon")]
        public string Icon { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "UseInFullTitle")]
        public string UseInFullTitle { get; set; }

        [XmlElement(ElementName = "UserArea")]
        public List<ExoUserAreaImpl> UserAreas { get; set; }

        [XmlElement(ElementName = "UserDescription")]
        public string UserDescription { get; set; }

        [XmlElement(ElementName = "Visible")]
        public string Visible { get; set; }

        [XmlIgnore]
        public bool IsVisible { get { return Visible.Equals("Yes"); } }
    }

    [XmlRoot(ElementName = "UserAreas")]
    public class ExoUserAreas
    {
        [XmlElement(ElementName = "UserArea")]
        public ExoUserAreaImpl RootArea { get; set; }

        public static ExoUserAreas DeserializeFrom(string xmlInputData)
        {
            return Xml.Deserialize<ExoUserAreas>(xmlInputData);
        }
    }


}
