using System.Xml.Serialization;
using System;

namespace Protocol.Manager
{
    [XmlRoot(ElementName = "class")]
    [Serializable]
    public class XmlMember
    {
        public XmlMember() 
        {
            ClassName = string.Empty;
            Tag = string.Empty;
            InterfaceName = string.Empty;
        }
        [XmlAttribute(AttributeName = "name")]
        public string ClassName { get; set; }

        [XmlAttribute(AttributeName = "tag")]
        public string Tag { get; set; }

        [XmlAttribute(AttributeName = "iname")]
        public string InterfaceName { get; set; }

        public override bool Equals(object obj)
        {
            var temp = obj as XmlMember;
            if (temp != null)
            {
                if (this.ClassName == temp.ClassName && this.Tag == temp.Tag && this.InterfaceName == temp.InterfaceName)
                    return true;
            }
            return false;
        }
    }
}
