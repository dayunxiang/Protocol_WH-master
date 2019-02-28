using System.Xml.Serialization;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Protocol.Manager
{
    [XmlRoot(ElementName = "root")]
    [Serializable]
    public class XmlDllCollections : ICloneable
    {
        [XmlArray(ElementName = "dlls")]
        [XmlArrayItem(ElementName = "dll")]
        public XmlDllInfos Infos { get; set; }

        public XmlDllCollections()
        {
            this.Infos = new XmlDllInfos();
        }

        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            object obj;
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                ms.Seek(0, SeekOrigin.Begin);
                obj = bf.Deserialize(ms);
            }
            finally
            {
                ms.Close();
            }

            return obj;
        }

        public override bool Equals(object obj)
        {
            var temp = obj as XmlDllCollections;
            if (temp != null)
            {
                if (this.Infos.Equals(temp.Infos))
                    return true;
            }
            return false;
        }
    }
}
