using System.Diagnostics;
using System.Reflection;
using Protocol.Channel.Interface;
using Protocol.Data.Interface;
using System.IO;
using System.Collections.Generic;
using System;

namespace Protocol.Manager
{
    public class ProtocolManager
    {
        #region 静态接口成员变量
        public static IGsm GSM(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_GSM) as IGsm;
        }
        public static IWebGsm WEBGSM(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_WEBGSM) as IWebGsm;
        }
        public static IGprs Gprs(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_GPRS) as IGprs;
        }
        public static ITransparen Transparen(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_TRANSPAREN) as ITransparen;
        }
        //public static IHDGprs HDGprs(XmlDllInfo info)
        //{
        //    return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_HDGPRS) as IHDGprs;
        //}
        public static IBeidouNormal BeidouNormal(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_BEIDOU_NORMAL) as IBeidouNormal;
        }
        public static IBeidou500 Beidou500(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_CHANNEL_BEIDOU_500) as IBeidou500;
        }
        public static IUp Up(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_DATA_Up) as IUp;
        }
        public static IDown Down(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_DATA_Down) as IDown;
        }
        public static IFlashBatch Flash(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_DATA_FlashBatch) as IFlashBatch;
        }
        public static IUBatch UDisk(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.TAG_DATA_UBatch) as IUBatch;
        }
        public static ISoil Soil(XmlDllInfo info)
        {
            return Reflect.CreateInstance(info, CS_DEFINE.Tag_Data_Soil) as ISoil;
        }
        #endregion

        #region 静态公共方法
        /// <summary>
        /// 判断dll是否有效
        /// </summary>
        /// <param name="dll"></param>
        /// <returns></returns>
        public static bool AssertDllValid(XmlDllInfo dll)
        {
            try
            {
                string filepath = dll.BaseDir + "\\" + dll.FileName;
                Assembly asm = Assembly.LoadFile(filepath);
                foreach (XmlMember member in dll.Members)
                {
                    if (IsTagValid(member.Tag))
                    {
                        // tag有效，继续判断实例化类是否错误,如果没有该类型，返回null
                        if (null == asm.CreateInstance(member.ClassName))
                            return false;
                    }
                    else
                    {
                        // 遇到不认识的tag
                        return false;
                    }
                }
                // 创建实例都正确的话
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }
        /// <summary>
        ///  判断某个类的tag是否有效
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool IsTagValid(string tag)
        {
            if (tag != CS_DEFINE.TAG_CHANNEL_GSM &&
                tag != CS_DEFINE.TAG_CHANNEL_GPRS &&
                tag != CS_DEFINE.TAG_CHANNEL_HDGPRS &&
                tag != CS_DEFINE.TAG_CHANNEL_BEIDOU_NORMAL &&
                tag != CS_DEFINE.TAG_CHANNEL_CABLE &&
                tag != CS_DEFINE.TAG_CHANNEL_BEIDOU_500 &&
                tag != CS_DEFINE.TAG_DATA_Down &&
                tag != CS_DEFINE.TAG_DATA_Up &&
                tag != CS_DEFINE.TAG_DATA_UBatch &&
                tag != CS_DEFINE.TAG_DATA_FlashBatch &&
                tag != CS_DEFINE.Tag_Data_Soil)
            {
                return false;
            }
            return true;
        }

        public static bool AssertChannelProtocolDllValid(string path, string tag,
            out string className,
            out string interfaceName,
            out string dllInfoTag,
            out EDllType4Xml dllType)
        {

            className = string.Empty;
            interfaceName = string.Empty;
            dllInfoTag = string.Empty;
            dllType = EDllType4Xml.none;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(tag))
                {
                    return false;
                }

                if (CS_DEFINE.I_CHANNEL_BEIDOU_500.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_BEIDOU_500;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_BEIDOU_500;
                    dllType = EDllType4Xml.beidou_500;
                }
                else if (CS_DEFINE.I_CHANNEL_BEIDOU_NORMAL.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_BEIDOU_NORMAL;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_BEIDOU_NORMAL;
                    dllType = EDllType4Xml.beidou_normal;
                }
                else if (CS_DEFINE.I_CHANNEL_CABLE.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_CABLE;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_CABLE;
                    dllType = EDllType4Xml.cable;
                }
                else if (CS_DEFINE.I_CHANNEL_GPRS.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_GPRS;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_GPRS;
                    dllType = EDllType4Xml.gprs;
                }
                //else if (CS_DEFINE.I_CHANNEL_HDGPRS.Contains(tag))
                //{
                //    interfaceName = CS_DEFINE.I_CHANNEL_HDGPRS;
                //    dllInfoTag = CS_DEFINE.TAG_CHANNEL_HDGPRS;
                //    dllType = EDllType4Xml.gprs;
                //}
                else if (CS_DEFINE.I_CHANNEL_GSM.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_GSM;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_GSM;
                    dllType = EDllType4Xml.gsm;
                }
                else if (CS_DEFINE.I_CHANNEL_WebGSM.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_WebGSM;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_WEBGSM ;
                    dllType = EDllType4Xml.webgsm;
                }else if (CS_DEFINE.I_CHANNEL_Transparen.Contains(tag))
                {
                    interfaceName = CS_DEFINE.I_CHANNEL_Transparen;
                    dllInfoTag = CS_DEFINE.TAG_CHANNEL_TRANSPAREN;
                    dllType = EDllType4Xml.none;
                }
                else
                    return false;

                Assembly asm = Assembly.LoadFrom(path);
                var types = new List<Type>(asm.GetTypes());
                foreach (var type in types)
                {
                    var iType = new List<Type>(type.GetInterfaces());
                    foreach (var t in iType)
                    {
                        if (t.FullName == interfaceName)
                        {
                            className = type.FullName;
                            return true;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
            return false;
        }

        public static bool AssertDataProtocolDllValid(string path,
            out string up,
            out string down,
            out string udisk,
            out string flash,
            out string soil)
        {
            up = down = udisk = flash = soil = string.Empty;
            try
            {

                if (!File.Exists(path))
                {
                    return false;
                }
                Assembly asm = Assembly.LoadFile(path);
                var types = new List<Type>(asm.GetTypes());
                foreach (var type in types)
                {
                    var iType = new List<Type>(type.GetInterfaces());
                    foreach (var t in iType)
                    {
                        if (t.FullName.Contains(CS_DEFINE.I_DATA_UP))
                        {
                            up = type.FullName;
                        }
                        else if (t.FullName.Contains(CS_DEFINE.I_DATA_DOWN))
                        {
                            down = type.FullName;
                        }
                        else if (t.FullName.Contains(CS_DEFINE.I_DATA_UDISK_BATCH))
                        {
                            udisk = type.FullName;
                        }
                        else if (t.FullName.Contains(CS_DEFINE.I_DATA_FLASH_BATCH))
                        {
                            flash = type.FullName;
                        }
                        else if (t.FullName.Contains(CS_DEFINE.I_DATA_SOIL))
                        {
                            soil = type.FullName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            return !String.IsNullOrEmpty(up + down + udisk + flash);
        }

        #endregion

        /// <summary>
        /// 反射类
        /// </summary>
        internal class Reflect
        {
            /// <summary>
            /// 创建信道协议类型实例
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public static object CreateInstance(XmlDllInfo info, string tag)
            {
                try
                {
                    /* 
                     * 读取配置文件
                     * 获取程序集的完全限定名称，类名称
                     * */
                    if (!info.Enabled)
                        return null;
                    if (string.IsNullOrEmpty(info.BaseDir))
                        return null;
                    String curDir = Environment.CurrentDirectory;
                    if (string.IsNullOrEmpty(info.FileName))
                        return null;
                    string assemblyString = string.Format(@"{0}\{1}", info.BaseDir, info.FileName);

                    if (!File.Exists(assemblyString))
                        return null;

                    if (info.Members.Count <= 0)
                        return null;

                    string classname = string.Empty;
                    foreach (var item in info.Members)
                    {
                        if (item.Tag == tag)
                        {
                            classname = item.ClassName;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(classname))
                    {
                        return null;
                    }

                    /* 
                     * 加载程序集
                     * */
                    //Assembly asm = Assembly.LoadFile(assemblyString);
                    // 2017-10-11webapi修改
                    Assembly asm = Assembly.Load(AssemblyName.GetAssemblyName(assemblyString));

                    /*  
                     * 创建实例
                     * */
                    return asm.CreateInstance(classname);
                }
                catch (System.Exception exp)
                {
                    System.Diagnostics.Debug.WriteLine(exp.Message);
                }
                return null;
            }
        }

    }//end of class

    public class CS_DEFINE
    {
        public const string I_CHANNEL_BEIDOU_NORMAL = "Protocol.Channel.Interface.IBeidouNormal";
        public const string I_CHANNEL_BEIDOU_500 = "Protocol.Channel.Interface.IBeidou500";
        public const string I_CHANNEL_CABLE = "Protocol.Channel.Interface.ICable";
        public const string I_CHANNEL_GPRS = "Protocol.Channel.Interface.IGprs";
        //public const string I_CHANNEL_HDGPRS = "Protocol.Channel.Interface.IHDGprs";
        public const string I_CHANNEL_GSM = "Protocol.Channel.Interface.IGsm";
        public const string I_CHANNEL_WebGSM = "Protocol.Channel.Interface.IWebGsm";
        public const string I_CHANNEL_Transparen = "Protocol.Channel.Interface.ITransparen";

        public const string I_DATA_UP = "Protocol.Data.Interface.IUp";
        public const string I_DATA_DOWN = "Protocol.Data.Interface.IDown";
        public const string I_DATA_FLASH_BATCH = "Protocol.Data.Interface.IFlashBatch";
        public const string I_DATA_UDISK_BATCH = "Protocol.Data.Interface.IUBatch";
        public const string I_DATA_SOIL = "Protocol.Data.Interface.ISoil";

        /*
         * 信道协议Tag
         * */
        public const string TAG_CHANNEL_GSM = "IGsm";
        public const string TAG_CHANNEL_WEBGSM = "IWebGsm";
        public const string TAG_CHANNEL_GPRS = "IGprs";
        public const string TAG_CHANNEL_HDGPRS = "IHDGprs";
        public const string TAG_CHANNEL_BEIDOU_NORMAL = "IBeidou_normal";
        public const string TAG_CHANNEL_BEIDOU_500 = "IBeidou_500";
        public const string TAG_CHANNEL_CABLE = "ICable";
        public const string TAG_CHANNEL_TRANSPAREN = "ITransparen";

        /*
         * 数据协议Tag
         * */
        public const string TAG_DATA_Up = "IUp";
        public const string TAG_DATA_Down = "IDown";
        public const string TAG_DATA_FlashBatch = "IFlash";
        public const string TAG_DATA_UBatch = "IUBatch";
        public const string Tag_Data_Soil = "ISoil";

    }
}
