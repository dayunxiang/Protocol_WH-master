using Protocol.Manager.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Protocol.Manager
{
    /// <summary>
    /// 动态库配置的XML文件管理类
    /// </summary>
    public class XmlDocManager
    {
        private static string m_path = "Config/DllConf.xml";

        private static string gsm_path = "Config/WebGsmConf.xml";


        /// <summary>
        /// 读取XML文件，生成自定义的结构体
        /// </summary>
        /// <returns></returns>
        public static XmlDllCollections Deserialize()
        {
            XmlDllCollections result = null;
            using (Stream fileStream = new FileStream(m_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(XmlDllCollections));
                result = (XmlDllCollections)deserializer.Deserialize(fileStream);
            }
            return result;
        }

        /// <summary>
        /// 序列化配置好的实例,XmlDllCollections是配置好路径的实例
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool Serialize_GSM(CXMLGSM obj)
        {
            try
            {
                if (!Directory.Exists("Config"))
                {
                    // 创建文件夹
                    Directory.CreateDirectory("Config");
                }
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);
                settings.Indent = true;
                XmlWriter xw = XmlWriter.Create(gsm_path, settings);
                XmlSerializer serializer = new XmlSerializer(typeof(XmlDllCollections));
                serializer.Serialize(xw, obj);
                xw.Close();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("程序集 XML 配置写入错误" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 读取XML文件，生成自定义的结构体
        /// </summary>
        /// <returns></returns>
        public static XmlDllCollections Deserialize_GSM()
        {
            XmlDllCollections result = null;
            using (Stream fileStream = new FileStream(gsm_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(XmlDllCollections));
                result = (XmlDllCollections)deserializer.Deserialize(fileStream);
            }
            return result;
        }

        /// <summary>
        /// 序列化配置好的实例,XmlDllCollections是配置好路径的实例
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool Serialize(XmlDllCollections obj)
        {
            try
            {
                if (!Directory.Exists("Config"))
                {
                    // 创建文件夹
                    Directory.CreateDirectory("Config");
                }
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);
                settings.Indent = true;
                XmlWriter xw = XmlWriter.Create(m_path, settings);
                XmlSerializer serializer = new XmlSerializer(typeof(XmlDllCollections));
                serializer.Serialize(xw, obj);
                xw.Close();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("程序集 XML 配置写入错误" + ex.Message);
                return false;
            }
        }

        private static XmlDocManager instance;
        public static XmlDocManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new XmlDocManager();
                return instance;
            }
        }
        private XmlDocManager()
        {
            this.m_dllInfo = new XmlDllCollections();
            this.m_dllInfoCopy = new XmlDllCollections();
            ReadFromXml();
        }

        private XmlDllCollections m_dllInfo;
        public XmlDllCollections DllInfo
        {
            get { return this.m_dllInfo; }
            set { this.m_dllInfo = value; }
        }

        private XmlDllCollections m_dllInfoCopy;
        public XmlDllCollections DllInfoCopy
        {
            get { return this.m_dllInfoCopy; }
        }

        /// <summary>
        /// 读取Xml配置文件
        /// </summary>
        public void ReadFromXml()
        {
            //  序列化配置文件
            XmlDllCollections result = null;

            //  如果配置文件目录中不包含DllConf.xml，创建一个空的配置文件
            if (!File.Exists(m_path))
            {
                var emptyNode = new XmlDllCollections();
                Serialize(emptyNode);
            }
            //  读取配置文件
            using (Stream fileStream = new FileStream(m_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(XmlDllCollections));
                result = (XmlDllCollections)deserializer.Deserialize(fileStream);
            }

            if (result == null)
                return;
            //  清空配置文件中信息
            this.m_dllInfo.Infos.Clear();
            var temp = new XmlDllCollections();
            foreach (var item in result.Infos)
            {
                //  仅加载系统启用的dll
                if (item.Enabled)
                    temp.Infos.Add(item);
            }
            this.m_dllInfo = temp;
            this.m_dllInfoCopy = temp;
        }
        /// <summary>
        /// 写入Xml配置文件
        /// </summary>
        public void WriteToXml()
        {
            try
            {
                if (this.m_dllInfo == null)
                    return;

                var temp = new XmlDllCollections();
                foreach (var item in this.m_dllInfo.Infos)
                {
                    if (item.Enabled)
                        temp.Infos.Add(item);
                }

                if (!Directory.Exists("Config"))
                {
                    // 创建文件夹
                    Directory.CreateDirectory("Config");
                }
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);
                settings.Indent = true;
                XmlWriter xw = XmlWriter.Create(m_path, settings);
                XmlSerializer serializer = new XmlSerializer(typeof(XmlDllCollections));
                serializer.Serialize(xw, temp);
                xw.Close();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("程序集 XML 配置写入错误" + ex.Message);
            }
        }

        /// <summary>
        /// 读取Xml配置文件
        /// </summary>
        public void ReadFromGsmXml()
        {
            //  序列化配置文件
            XmlDllCollections result = null;

            //  如果配置文件目录中不包含DllConf.xml，创建一个空的配置文件
            if (!File.Exists(m_path))
            {
                var emptyNode = new XmlDllCollections();
                Serialize(emptyNode);
            }
            //  读取配置文件
            using (Stream fileStream = new FileStream(m_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(XmlDllCollections));
                result = (XmlDllCollections)deserializer.Deserialize(fileStream);
            }

            if (result == null)
                return;
            //  清空配置文件中信息
            this.m_dllInfo.Infos.Clear();
            var temp = new XmlDllCollections();
            foreach (var item in result.Infos)
            {
                //  仅加载系统启用的dll
                if (item.Enabled)
                    temp.Infos.Add(item);
            }
            this.m_dllInfo = temp;
            this.m_dllInfoCopy = temp;
        }
        /// <summary>
        /// 写入Xml配置文件
        /// </summary>
        public void WriteToGsmXml()
        {
            try
            {
                if (this.m_dllInfo == null)
                    return;

                var temp = new XmlDllCollections();
                foreach (var item in this.m_dllInfo.Infos)
                {
                    if (item.Enabled)
                        temp.Infos.Add(item);
                }

                if (!Directory.Exists("Config"))
                {
                    // 创建文件夹
                    Directory.CreateDirectory("Config");
                }
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);
                settings.Indent = true;
                XmlWriter xw = XmlWriter.Create(m_path, settings);
                XmlSerializer serializer = new XmlSerializer(typeof(XmlDllCollections));
                serializer.Serialize(xw, temp);
                xw.Close();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("程序集 XML 配置写入错误" + ex.Message);
            }
        }

        /// <summary>
        /// 获取所有可用的配置程序集的串口信息
        /// </summary>
        public List<int> GetAllComPorts()
        {
            var coms = new List<int>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                if (item.Coms == null)
                    continue;
                foreach (var com in item.Coms)
                {
                    if (!coms.Contains(com))
                        coms.Add(com);
                }
            }
            return coms;
        }
        /// <summary>
        /// 获取所有可用的配置程序集的端口信息
        /// </summary>
        public List<int> GetAllPorts()
        {
            var ports = new List<int>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                if (item.Ports == null)
                    continue;
                foreach (var port in item.Ports)
                {
                    if (!ports.Contains(port.PortNumber))
                    {
                        // 如果结果集中不包括当前串口，添加，去重
                        ports.Add(port.PortNumber);
                    }
                }
            }
            return ports;
        }

        /// <summary>
        /// 根据串口ID，获取程序集DLL信息
        /// </summary>
        public List<XmlDllInfo> GetComDlls(int comValue)
        {
            var infos = new List<XmlDllInfo>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                foreach (var com in item.Coms)
                {
                    if (comValue == com)
                    {
                        infos.Add(item);
                        continue;
                    }
                }
            }
            return infos;
        }
        /// <summary>
        /// 根据端口ID，获取程序集DLL信息
        /// </summary>
        public List<XmlDllInfo> GetPortDlls(int portValue)
        {
            var infos = new List<XmlDllInfo>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                foreach (var port in item.Ports)
                {
                    if (portValue == port.PortNumber)
                    {
                        infos.Add(item);
                        continue;
                    }
                }
            }
            return infos;
        }

        /// <summary>
        /// 获取GSM所有配置信息
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, List<XmlDllInfo>> GetGSMs()
        {
            var dics = new Dictionary<int, List<XmlDllInfo>>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                //  使用此协议
                if (!item.Enabled)
                    continue;
                //  协议类型为GSM类型
                if (item.DllType != EDllType4Xml.gsm)
                    continue;
            }
            return dics;
        }

        #region 属性

        // 获取当前配置文件中所有可用信道协议的名称
        public List<string> ChannelProtocolNames
        {
            get
            {
                return GetProtocolNamesByType("channel");
            }
        }

        // 获取当前配置文件中所有可用数据协议的名称
        public List<string> DataProtocolNames
        {
            get
            {
                return GetProtocolNamesByType("data");
            }
        }

        /// <summary>
        /// 获取所有北斗卫星普通终端的串口号，如果没有，返回COUNT=0的非空List
        /// </summary>
        public List<int> BeidouNormalComPorts
        {
            get
            {
                var coms = new List<int>();
                foreach (var item in this.m_dllInfo.Infos)
                {
                    if (!item.Enabled)
                        continue;
                    if (item.DllType != EDllType4Xml.beidou_normal)
                        continue;
                    foreach (var com in item.Coms)
                    {
                        if (coms.Contains(com))
                        {
                            continue;
                        }
                        coms.Add(com);
                    }
                }
                return coms;
            }
        }
        public List<int> Beidou500ComPorts
        {
            get
            {
                var coms = new List<int>();
                foreach (var item in this.m_dllInfo.Infos)
                {
                    if (!item.Enabled)
                        continue;
                    if (item.DllType != EDllType4Xml.beidou_500)
                        continue;
                    foreach (var com in item.Coms)
                    {
                        if (coms.Contains(com))
                        {
                            continue;
                        }
                        coms.Add(com);
                    }
                }
                return coms;
            }
        }

        public List<string> BeidouNormalComPortsName()
        {
            var result = new List<string>();
            var coms = BeidouNormalComPorts;
            foreach (var com in coms)
            {
                result.Add(string.Format("COM{0}", com));
            }
            return result;
        }

        public List<string> BeidouNormalProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.beidou_normal);
            }
        }

        public List<string> Beidou500ComPortsName()
        {
            var result = new List<string>();
            var coms = Beidou500ComPorts;
            foreach (var com in coms)
            {
                result.Add(string.Format("COM{0}", com));
            }
            return result;
        }

        public List<string> Beidou500ProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.beidou_500);
            }
        }

        public List<string> GSMProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.gsm);
            }
        }

        public List<string> WebGSMProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.webgsm);
            }
        }

        public List<string> GPRSProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.gprs);
            }
        }

        public List<string> CableProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.cable);
            }
        }

        public List<string> NoneProtocolNames
        {
            get
            {
                return GetProtocolNamesByType(EDllType4Xml.none);
            }
        }

        /// <summary>
        /// 获取当前所有配置串口的信道协议，如果没有任何配置，返回COUINT=0的集合
        /// </summary>
        public List<string> ComProtocolChannelNames
        {
            get
            {
                List<string> result = new List<string>();
                result.AddRange(GSMProtocolNames);
                result.AddRange(BeidouNormalProtocolNames);
                result.AddRange(Beidou500ProtocolNames);
                result.AddRange(CableProtocolNames);
                return result;
            }
        }
        #endregion 属性

        private List<string> GetProtocolNamesByType(string type)
        {
            var result = new List<string>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                if (item.Type != type)
                    continue;
                result.Add(item.Name);
            }
            return result;
        }


        private List<string> GetProtocolNamesByType(EDllType4Xml dtype, string type = "channel")
        {
            var result = new List<string>();
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                if (item.Type != type)
                    continue;
                if (item.DllType != dtype)
                    continue;
                result.Add(item.Name);
            }
            return result;
        }


        public List<XmlDllInfo> GPRSLists()
        {
            var gprsNameLists = GPRSProtocolNames;
            var result = new List<XmlDllInfo>();
            foreach (var name in gprsNameLists)
            {
                var info = GetInfoByName(name);
                if (info != null)
                    result.Add(info);
            }
            return result;
        }

        public XmlDllInfo GetInfoByName(string name)
        {
            XmlDllInfo info = null;
            foreach (var item in this.m_dllInfo.Infos)
            {
                if (!item.Enabled)
                    continue;
                if (item.Name != name)
                    continue;
                info = item;
                break;
            }
            return info;
        }

        public XmlDllInfo GetGPRSChannelInfoByPort(int comValue)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                if (!dll.Enabled)
                    continue;
                if (dll.DllType != EDllType4Xml.gprs)
                    continue;
                if (dll.Type != "channel")
                    continue;
                foreach (var com in dll.Ports)
                {
                    if (com.PortNumber == comValue)
                    {
                        return dll;
                    }
                }
            }
            return null;
        }

        public XmlDllInfo GetGPRSDataInfoByPort(int comValue)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                if (!dll.Enabled)
                    continue;
                if (dll.DllType != EDllType4Xml.gprs)
                    continue;
                if (dll.Type != "data")
                    continue;
                foreach (var com in dll.Ports)
                {
                    if (com.PortNumber == comValue)
                    {
                        return dll;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取北斗卫星信道协议,如果没有找到，返回null
        /// </summary>
        /// <param name="comValue"></param>
        /// <returns></returns>
        public XmlDllInfo GetBeidouChannelInfoByCom(int comValue)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                if (!dll.Enabled)
                    continue;
                if (dll.DllType != EDllType4Xml.beidou_normal)
                    continue;
                if (dll.Type != "channel")
                    continue;
                foreach (var com in dll.Coms)
                {
                    if (com == comValue)
                    {
                        return dll;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取北斗卫星的数据数据协议，传入端口号
        /// </summary>
        /// <param name="comValue"></param>
        /// <returns>返回NULL</returns>
        public XmlDllInfo GetBeidouDataInfoByCom(int comValue)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                if (!dll.Enabled)
                    continue;
                if (dll.DllType != EDllType4Xml.beidou_normal)
                    continue;
                if (dll.Type != "data")
                    continue;
                foreach (var com in dll.Coms)
                {
                    if (com == comValue)
                    {
                        return dll;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 根据端口或者串口号得到数据协议的Dll信息，如果没有返回NULL，默认是获取串口的dll
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bIsCom"></param>
        /// <returns></returns>
        public XmlDllInfo GetDataDllByComOrPort(int value, bool bIsCom = true)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                // 如果没有启动dll, 保留字段，意义不大
                if (!dll.Enabled)
                    continue;
                if (dll.Type == "data")
                {
                    // 如果是串口
                    if (bIsCom)
                    {
                        if (dll.Coms == null)
                            continue;
                        // 并且查询的是串口号
                        foreach (var com in dll.Coms)
                        {
                            if (com == value)
                            {
                                return dll;
                            }

                        }
                    }
                    else
                    {
                        if (dll.Ports == null)
                            continue;
                        // 查询的是端口号
                        for (int i = 0; i < dll.Ports.Count; ++i)
                        {
                            if (value == dll.Ports[i].PortNumber)
                            {
                                return dll;
                            }
                        }
                    }
                }// end of if dll type = data
            } // end of for
            return null;
        }

        public XmlDllInfo GetDataDllByWeb(string name, bool bIsWeb)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                // 如果没有启动dll, 保留字段，意义不大
                if (!dll.Enabled)
                    continue;
                if (dll.Type == "data")
                {
                    // 如果是webgsm服务
                    if (bIsWeb)
                    {
                            if (name == dll.Name)
                            {
                                return dll;
                            }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// 获取端口或者串口的信道协议DLL,如果没有查到，返回NULL
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bIsCom"></param>
        /// <returns></returns>
        public XmlDllInfo GetChannelDllByComOrPort(int value, bool bIsCom = true)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                // 如果没有启动dll, 保留字段，意义不大
                if (!dll.Enabled)
                    continue;
                if (dll.Type == "channel")
                {
                    // 如果是数据协议
                    if (bIsCom)
                    {
                        if (dll.Coms == null)
                            continue;
                        // 并且查询的是串口号
                        foreach (var com in dll.Coms)
                        {
                            if (com == value)
                            {
                                return dll;
                            }

                        }
                    }
                    else
                    {
                        if (dll.Ports == null)
                            continue;
                        // 查询的是端口号
                        for (int i = 0; i < dll.Ports.Count; ++i)
                        {
                            if (value == dll.Ports[i].PortNumber)
                            {
                                return dll;
                            }
                        }
                    }
                }// end of if dll type = data
            } // end of for
            return null;
        }

        public XmlDllInfo GetGSMDataInfoByCom(int comValue)
        {
            foreach (var dll in this.m_dllInfo.Infos)
            {
                if (!dll.Enabled)
                    continue;
                if (dll.DllType != EDllType4Xml.gsm)
                    continue;
                if (dll.Type != "data")
                    continue;
                foreach (var com in dll.Coms)
                {
                    if (com == comValue)
                    {
                        return dll;
                    }
                }
            }
            return null;
        }

        public Dictionary<int, List<XmlDllInfo>> GetGPRSs()
        {
            var result = new Dictionary<int, List<XmlDllInfo>>();
            var ports = GetAllPorts();
            foreach (var port in ports)
            {
                var lists = GetPortDlls(port);
                result.Add(port, lists);
            }
            return result;
        }

        #region 配置页面相关

        /// <summary>
        /// 获取所有的串口或者端口配置，如果没有配置信道协议或者数据协议，返回空的信道协议和数据协议名
        /// </summary>
        /// <returns></returns>
        public List<CPortProtocolConfig> GetComOrPortConfig(bool bIsCom = true)
        {
            // 先遍历所有串口号
            List<CPortProtocolConfig> listResult = new List<CPortProtocolConfig>();
            List<int> listPorts = null;
            if (bIsCom)
            {
                // 获取串口号
                listPorts = GetAllComPorts();
            }
            else
            {
                // 获取端口号
                listPorts = GetAllPorts();
            }
            for (int i = 0; i < listPorts.Count; ++i)
            {
                // 获取信道协议
                XmlDllInfo dllChannel = GetChannelDllByComOrPort(listPorts[i], bIsCom);
                XmlDllInfo dllData = GetDataDllByComOrPort(listPorts[i], bIsCom);
                // 即使没有配任何数据协议和信道协议，返回空的信道协议和数据协议名
                CPortProtocolConfig config = new CPortProtocolConfig();
                config.PortNumber = listPorts[i];
                if (dllChannel != null)
                {
                    config.ProtocolChannelName = dllChannel.Name;
                    // 判断端口的启动模式
                    for (int j = 0; j < dllChannel.Ports.Count; ++j)
                    {
                        if (dllChannel.Ports[j].PortNumber == listPorts[i])
                        {
                            //启动模式写入，是否启动，并不在配置文件中存储
                            config.BAutoStart = dllChannel.Ports[j].BAutoStart;
                            break;
                        }
                    }
                }
                if (dllData != null)
                {
                    config.ProtocolDataName = dllData.Name;
                    // 判断端口的启动模式, 数据协议应该和信道协议一致的
                    for (int j = 0; j < dllData.Ports.Count; ++j)
                    {
                        if (dllData.Ports[j].PortNumber == listPorts[i])
                        {
                            //启动模式写入，是否启动，并不在配置文件中存储
                            config.BAutoStart = dllData.Ports[j].BAutoStart;
                            break;
                        }
                    }
                }
                listResult.Add(config);
            }
            return listResult;
        }


        /// <summary>
        /// 重置串口或者端口配置，所有的串口或者端口配置，如果以前的不存在了，则认为已经删除.默认是更新串口的
        /// </summary>
        /// <param name="listComs"></param>
        /// <returns></returns>
        public bool ResetComOrPortConfig(List<CPortProtocolConfig> listComs, bool bIsCom = true)
        {
            // 先清空所有协议的所有端口或者串口配置
            RemoveAllComOrPortConfig(bIsCom);
            // 协议名字，然后是对应得到串口号的集合，便于更新操作
            Dictionary<string, CProtocolPortConfig> mapProtocolName = new Dictionary<string, CProtocolPortConfig>();
            foreach (CPortProtocolConfig config in listComs)
            {
                if (mapProtocolName.ContainsKey(config.ProtocolDataName))
                {
                    // 数据协议, 如果map中包含了当前的协议，添加到当前的串口
                    if (bIsCom)
                    {
                        mapProtocolName[config.ProtocolDataName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber });
                    }
                    else
                    {
                        mapProtocolName[config.ProtocolDataName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber, BAutoStart = config.BAutoStart.Value });
                    }

                }
                else
                {
                    // 如果存在的Map中不包含当前的协议，新建一个
                    mapProtocolName.Add(config.ProtocolDataName, new CProtocolPortConfig());
                    mapProtocolName[config.ProtocolDataName].BIsDataProtocol = true; // 数据协议，不是信道协议
                    if (bIsCom)
                    {
                        mapProtocolName[config.ProtocolDataName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber });
                    }
                    else
                    {
                        mapProtocolName[config.ProtocolDataName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber, BAutoStart = config.BAutoStart.Value, BStartOrNot = config.BStartOrNot.Value });
                    }
                }

                if (mapProtocolName.ContainsKey(config.ProtocolChannelName))
                {
                    // 信道协议
                    if (bIsCom)
                    {
                        mapProtocolName[config.ProtocolChannelName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber });
                    }
                    else
                    {
                        mapProtocolName[config.ProtocolChannelName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber, BAutoStart = config.BAutoStart.Value, BStartOrNot = config.BStartOrNot.Value });
                    }
                }
                else
                {
                    // 如果存在的Map中不包含当前的协议，新建一个
                    mapProtocolName.Add(config.ProtocolChannelName, new CProtocolPortConfig());
                    mapProtocolName[config.ProtocolChannelName].BIsDataProtocol = false; //是信道协议
                    if (bIsCom)
                    {
                        mapProtocolName[config.ProtocolChannelName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber });
                    }
                    else
                    {
                        mapProtocolName[config.ProtocolChannelName].ListPort.Add(new CXMLPort { PortNumber = config.PortNumber, BAutoStart = config.BAutoStart.Value, BStartOrNot = config.BStartOrNot.Value });
                    }
                }
            }// end of foreach

            // 遍历Dictionary ,然后写入文件
            foreach (KeyValuePair<string, CProtocolPortConfig> protocol in mapProtocolName)
            {
                if (protocol.Key != null && !protocol.Key.Equals(""))
                {
                    ResetDllComOrPortConfig(protocol.Key, protocol.Value.ListPort, protocol.Value.BIsDataProtocol, bIsCom);
                }
            }
            WriteToXml(); // 写入配置文件
            return true;
        }

        /// <summary>
        /// 重置Web服务
        /// </summary>
        /// <param name="listComs"></param>
        /// <returns></returns>
        public bool ResetWebConfig(string name, List<CXMLWeb> listWebs)
        {
            // 先清空所有协议的所有配置
            RemoveAllWebConfig();

            foreach (XmlDllInfo dll in m_dllInfo.Infos)
            {
                // 遍历每个DLL集合，根据名字找到匹配的DLL集合
                if (name == dll.Name)
                {
                    dll.Webs = listWebs;
                }
            }// end of foreach

            WriteToXml(); // 写入配置文件
            return true;
        }

        #endregion 配置页面相关

        #region 帮助方法
        /// <summary>
        /// 重置某个DLL的端口号或者串口号集合，默认是串口号集合,找到匹配，返回True,否则返回false,一般不可能吧？？返回false
        /// </summary>
        private bool ResetDllComOrPortConfig(string protocolName, List<CXMLPort> listValues, bool bIsDllData = true, bool bIsCom = true)
        {
            List<int> tmpListCom = new List<int>();
            foreach (XmlDllInfo dll in m_dllInfo.Infos)
            {
                // 遍历每个DLL集合，根据名字找到匹配的DLL集合
                if (protocolName == dll.Name)
                {
                    // 更新数据协议
                    if (bIsDllData)
                    {
                        if (bIsCom)
                        {
                            // 更新数据协议内的串口
                            tmpListCom.Clear();
                            foreach (CXMLPort port in listValues)
                            {
                                tmpListCom.Add(port.PortNumber);
                            }
                            dll.Coms = tmpListCom;
                        }
                        else
                        {
                            // 更新数据协议内的网口
                            dll.Ports = listValues;
                        }
                    }
                    else
                    {
                        // 更新信道协议
                        if (bIsCom)
                        {
                            // 更新数据协议内的串口
                            tmpListCom.Clear();
                            foreach (CXMLPort port in listValues)
                            {
                                tmpListCom.Add(port.PortNumber);
                            }
                            dll.Coms = tmpListCom;
                        }
                        else
                        {
                            // 更新数据协议内的网口
                            dll.Ports = listValues;
                        }
                    }
                    return true;
                }// end of if 
            }// end of foreach
            throw new Exception("ResetDllComOrPortConfig Error");
        }

        /// <summary>
        /// 清除所有协议的串口或者端口配置
        /// </summary>
        /// <returns></returns>
        private bool RemoveAllComOrPortConfig(bool bIsCom = true)
        {
            foreach (XmlDllInfo dll in m_dllInfo.Infos)
            {
                if (bIsCom)
                {
                    // 清除串口
                    dll.Coms.Clear();
                }
                else
                {
                    // 清除端口
                    dll.Ports.Clear();
                }
            }
            return true;
        }

        /// <summary>
        /// 清除所有协议的串口或者端口配置
        /// </summary>
        /// <returns></returns>
        private bool RemoveAllWebConfig()
        {
            foreach (XmlDllInfo dll in m_dllInfo.Infos)
            {
                // 清除串口
                dll.Webs.Clear();
            }
            return true;
        }

        #endregion 帮助方法
    }

    /// <summary>
    /// DLL程序集的协议类型
    /// </summary>
    public enum EDllType4Xml
    {
        /// <summary>
        /// GPRS协议
        /// </summary>
        gprs,
        /// <summary>
        /// GPRS协议
        /// </summary>
        hdgprs,
        /// <summary>
        /// GSM协议
        /// </summary>
        gsm,
        /// <summary>
        /// WebService接口GSM协议
        /// </summary>
        webgsm,
        /// <summary>
        /// 北斗卫星普通终端协议
        /// </summary>
        beidou_normal,
        /// <summary>
        /// 北斗卫星500型指挥机类型
        /// </summary>
        beidou_500,
        /// <summary>
        /// 光缆
        /// </summary>
        cable,
        /// <summary>
        /// 无
        /// </summary>
        none
    }

    /// <summary>
    /// 端口协议配置，用于界面配置以及更新
    /// </summary>
    public class CPortProtocolConfig
    {
        /// <summary>
        /// 端口号，或者串口号，GPRS是端口号，其它的都是串口号
        /// </summary>
        public int PortNumber { get; set; }

        /// <summary>
        /// 数据协议名字
        /// </summary>
        public string ProtocolDataName { get; set; }

        /// <summary>
        /// 信道协议名字
        /// </summary>
        public string ProtocolChannelName { get; set; }

        /// <summary>
        /// 自动启动，只对于GPRS,才有，也就是网口才有的字段
        /// </summary>
        public Nullable<bool> BAutoStart { get; set; }

        /// <summary>
        /// 是否启动，True的话，启动，False的话，不启动
        /// </summary>
        public Nullable<bool> BStartOrNot { get; set; }
    }



    ///// <summary>
    ///// 端口协议配置，用于界面配置以及更新
    ///// </summary>
    //public class CGSMProtocolConfig
    //{
    //    /// <summary>
    //    /// 对应IP地址
    //    /// </summary>
    //    public string ServerIp { get; set; }

    //    /// <summary>
    //    /// 对应IP地址
    //    /// </summary>
    //    public string port { get; set; }

    //    /// <summary>
    //    /// 通讯协议
    //    /// </summary>
    //    public int ProtocolChannel { get; set; }

    //    /// <summary>
    //    /// 数据协议
    //    /// </summary>
    //    public string ProtocolData { get; set; }

    //    /// <summary>
    //    /// 是否开启
    //    /// </summary>

    //    public Nullable<bool> BStartOrNot { get; set; }

    //}

    /// <summary>
    /// 协议的端口或者串口配置，某个协议所有配置
    /// </summary>
    class CProtocolPortConfig
    {
        private List<CXMLPort> m_listPort;
        public CProtocolPortConfig()
        {
            m_listPort = new List<CXMLPort>();
        }
        /// <summary>
        /// 是不是数据协议
        /// </summary>
        public bool BIsDataProtocol { get; set; }

        /// <summary>
        /// 端口或者串口列表
        /// </summary>
        public List<CXMLPort> ListPort
        {
            get
            {
                return m_listPort;
            }
            set
            {
                m_listPort = value;
            }
        }
    }

}
