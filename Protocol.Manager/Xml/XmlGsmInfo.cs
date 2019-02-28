using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Protocol.Manager.Xml
{
    [XmlRoot(ElementName = "gsm")]
    [Serializable]
    public class XmlGsmInfo
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// 程序集对应的协议类型
        /// </summary>
        [XmlAttribute(AttributeName = "protocltype")]
        public EDllType4Xml DllType { get; set; }

        [XmlElement(ElementName = "basedir")]
        public string BaseDir { get; set; }

        [XmlAttribute(AttributeName = "enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [XmlElement(ElementName = "filename")]
        public string FileName { get; set; }

        /// <summary>
        /// 名字，显示给用户看的dll描述
        /// </summary>
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// DLL中接口信息
        /// </summary>
        [XmlArray(ElementName = "classes")]
        [XmlArrayItem(ElementName = "class")]
        public XmlMemberInfos Members { get; set; }

        /// <summary>
        /// 协议监听串口信息
        /// </summary>
        [XmlArray(ElementName = "coms")]
        [XmlArrayItem(ElementName = "com")]
        public List<int> Coms { get; set; }

        /// <summary>
        /// 协议监听端口信息
        /// </summary>
        [XmlArray(ElementName = "ports")]
        [XmlArrayItem(ElementName = "port")]
        public List<CXMLPort> Ports { get; set; }

        public override bool Equals(object obj)
        {
            var temp = obj as XmlDllInfo;
            if (temp != null)
            {
                if (this.Type == temp.Type &&
                    this.DllType == temp.DllType &&
                    this.BaseDir == temp.BaseDir &&
                    this.Enabled == temp.Enabled &&
                    this.FileName == temp.FileName &&
                    this.Name == temp.Name &&
                    this.Members.Equals(temp.Members) &&
                    IsComsEquals(this.Coms, temp.Coms) &&
                    IsPortEquals(this.Ports, temp.Ports)
                    )
                    return true;
            }
            return false;
        }

        private bool IsComsEquals(List<int> coms1, List<int> coms2)
        {
            if (coms1.Count != coms2.Count)
                return false;
            int count = 0;
            foreach (var com1 in coms1)
            {
                foreach (var com2 in coms2)
                {
                    if (com1.Equals(com2))
                        count += 1;
                }
            }
            return (count == coms1.Count);
        }
        private bool IsPortEquals(List<CXMLPort> ports1, List<CXMLPort> ports2)
        {
            if (ports1.Count != ports2.Count)
                return false;
            int count = 0;
            foreach (var port1 in ports1)
            {
                foreach (var port2 in ports2)
                {
                    if (port1.Equals(port2))
                        count += 1;
                }
            }
            return (count == ports1.Count);
        }
    }

    public class CXMLGSM
    {
        public CXMLGSM()
        {
            IP = "";
            PortNumber = 0;
            BAutoStart = false;
            BStartOrNot = false;
        }

        /// <summary>
        /// IP地址
        /// </summary>
        /// <param name=""></param>
        [XmlAttribute(AttributeName = "IP")]
        public string IP { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        [XmlAttribute(AttributeName = "PortNumber")]
        public int PortNumber { get; set; }

        /// <summary>
        /// 自动启动
        /// </summary>
        [XmlAttribute(AttributeName = "AutoStart")]
        public bool BAutoStart { get; set; }

        /// <summary>
        /// 对于手动启动的，是否启动
        /// </summary>
        [XmlAttribute(AttributeName = "StartOrNot")]
        public bool BStartOrNot { get; set; }

        public override bool Equals(object obj)
        {
            var temp = obj as CXMLPort;
            if (temp != null)
            {
                if (this.PortNumber == temp.PortNumber && this.BAutoStart == temp.BAutoStart && this.BStartOrNot == temp.BStartOrNot)
                    return true;
            }
            return false;
        }
    }
}
