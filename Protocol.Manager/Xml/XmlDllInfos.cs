using System.Collections.Generic;
using System.Xml.Serialization;
using System;

namespace Protocol.Manager
{
    [XmlRoot(ElementName = "dlls")]
    [Serializable] 
    public class XmlDllInfos : List<XmlDllInfo>
    {
        public override bool Equals(object obj)
        {
            var temp = obj as XmlDllInfos;

            if (temp != null)
            {
                if (temp.Count != this.Count)
                    return false;
                int count = 0;
                foreach (var item in this)
                {
                    foreach (var item1 in temp)
                    {
                        if (item.Equals(item1))
                            count += 1;
                    }
                }

                if (count == this.Count)
                    return true;
            }
            return false;
        }
    }
}
