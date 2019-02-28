using System.Collections.Generic;
using System.Xml.Serialization;
using System;

namespace Protocol.Manager
{
    [XmlRoot(ElementName = "classes")]
    [Serializable]
    public class XmlMemberInfos : List<XmlMember>
    {
        public override bool Equals(object obj)
        {
            var temp = obj as XmlMemberInfos;

            if (temp != null)
            {
                if (this.Count != temp.Count)
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
