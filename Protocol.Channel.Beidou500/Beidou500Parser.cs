using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Hydrology.Entity;
using Protocol.Channel.Interface;
using Protocol.Data.Interface;
using Protocol.Channel.Gprs;
using System.IO;

namespace Protocol.Channel.Beidou500
{
    public class Beidou500Parser : IBeidou500
    {
        public SerialPort Port { get; set; }
        public IUp Up { get; set; }
        public IDown Down { get; set; }
        public IUBatch UBatch { get; set; }
        public IFlashBatch FlashBatch { get; set; }
        public ISoil Soil { get; set; }

        public List<CEntityStation> StationLists { get; set; }
        public Boolean IsCommonWorkNormal { get; set; }

        #region 私有成员变量
        public static int num = 0;
        private List<string> m_listDatas;
        private List<byte> m_inputBuffer;
        private System.Timers.Timer m_timer;    //  定时器：时间间隔45Min

        private EChannelType m_channelType = EChannelType.Beidou500;
        private EListeningProtType m_portType = EListeningProtType.SerialPort;

        // 多线程相关
        private Mutex m_mutexListByte;      // 对InputBuffer的独占访问权
        private Semaphore m_semephoreData;  // 用来唤醒数据处理线程
        private Thread m_threadDealData;    // 处理数据的进程 
        #endregion

        #region 初始化
        public Beidou500Parser()
        {
            // 构造函数，开启数据处理进程
            m_mutexListByte = new Mutex();
            m_semephoreData = new Semaphore(0, Int32.MaxValue);

            m_threadDealData = new Thread(new ThreadStart(DealData))
            {
                Name = "北斗卫星指挥机数据处理线程"
            };
            m_threadDealData.Start();
        }

        public void Init(string portName, int baudRate)
        {
            //InvokeMessage(String.Format("初始化串口{0}...", portName), "初始化");
            //  初始化串口信息
            this.Port = new SerialPort()
            {
                PortName = portName,
                BaudRate = baudRate
            };
            this.Port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);

            //  初始化缓冲区
            this.m_listDatas = new List<string>();
            this.m_inputBuffer = new List<byte>();

            //  初始化定时器
            //  每隔一分钟判断是否需要发送QAST指令
            this.m_timer = new System.Timers.Timer()
            {
                Enabled = true,
                Interval = 1000 * 60 // 1分钟
            };
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            m_timer.Start();    //  启动定时器

            //InvokeMessage(String.Format("初始化串口{0}成功", portName), "初始化");
        }

        public void InitInterface(IUp up, IDown down, IUBatch udisk, IFlashBatch flash, ISoil soil)
        {
            this.Up = up;
            this.Down = down;
            this.UBatch = udisk;
            this.FlashBatch = flash;
            this.Soil = soil;
            Debug.WriteLine("接口初始化完成");
        }

        public void InitStations(List<CEntityStation> stations)
        {
            this.StationLists = stations;
        }
        #endregion

        #region 串口操作
        /// <summary>
        /// 打开串口
        /// </summary>
        public bool Open()
        {
            try
            {
                if (m_threadDealData.ThreadState == System.Threading.ThreadState.Aborted ||
                    m_threadDealData.ThreadState == System.Threading.ThreadState.AbortRequested)
                {
                    m_threadDealData = new Thread(new ThreadStart(DealData))
                    {
                        Name = "北斗卫星指挥机数据处理线程"
                    };
                    m_threadDealData.Start();

                    Debug.WriteLine("恢复" + m_threadDealData.Name);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
            }
            try
            {
                //InvokeMessage(String.Format("开启串口{0}", Port.PortName), "初始化");
                Port.Open();
                InvokeMessage(String.Format("开启串口{0}成功", Port.PortName), "初始化");

                return true;
            }
            catch (Exception ex)
            {
                InvokeMessage(String.Format("开启串口{0}失败", Port.PortName), "初始化");
                Debug.WriteLine(ex.ToString());
                return false;
            }

        }
        /// <summary>
        /// 关闭函数，记得调用
        /// </summary>
        public void Close()
        {
            // 关闭线程处理函数
            try
            {
                m_threadDealData.Abort();
                Debug.WriteLine("挂起" + m_threadDealData.Name);
            }
            catch (Exception exp) { Debug.WriteLine(exp); }

            try
            {
                bool isOpen = this.Port.IsOpen;
                int portNum = Int32.Parse(Port.PortName.Replace("COM", ""));
                //  关闭串口
                Port.Close();
            }
            catch (Exception exp) { Debug.WriteLine(exp); }
        }
        #endregion

        #region 监听串口，并处理卫星数据
        //  监听串口
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //  读取串口内容
            int n = Port.BytesToRead;
            if (n != 0)
            {
                if (this.SerialPortStateChanged != null)
                    this.SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                    {
                        BNormal = true,
                        PortNumber = Int32.Parse(Port.PortName.Replace("COM", "")),
                        PortType = this.m_portType
                    }));
            }

            byte[] buf = new byte[n];
            Port.Read(buf, 0, n);
            //  多线程写入缓冲区中
            m_mutexListByte.WaitOne();
            m_inputBuffer.AddRange(buf);
            m_semephoreData.Release(1); //释放信号量，通知消费者
            m_mutexListByte.ReleaseMutex();
        }

        // 处理缓冲区中的数据
        //private void DealData()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            m_semephoreData.WaitOne();
        //            m_mutexListByte.WaitOne(); //等待内存互斥量
                    
        //            var count = (from r in m_inputBuffer where (r == 13) select r).Count();

        //            if (count == 0)
        //            {
        //                m_mutexListByte.ReleaseMutex(); //内存操作完毕，释放互斥量
        //                continue;
        //            }

        //            int indexOfFirst13 = 0;
        //            indexOfFirst13 = m_inputBuffer.IndexOf(13);
        //            var msgBytes = m_inputBuffer.GetRange(0, indexOfFirst13 + 1);
        //            this.m_inputBuffer.RemoveRange(0, indexOfFirst13 + 1);
        //            m_mutexListByte.ReleaseMutex(); //内存操作完毕，释放互斥量

        //            string msg = System.Text.Encoding.Default.GetString(msgBytes.ToArray());
        //           // InvokeMessage(msg.ToString()+"..."+msg.Length,"测试");
        //            int indexOfDollar = msg.IndexOf('$');
        //            string tag = msg.Substring(indexOfDollar + 1, 4);

        //            msg = msg.Replace("\r", "").Replace("\n", "");
        //            Debug.WriteLine("接收消息:  " + msg);

        //            switch (tag)
        //            {
        //                case CBeiDouBJXX.CMD_PREFIX: DealBJXX(msg); break;
        //                case CBeiDouZTXX.CMD_PREFIX: DealZTXX(msg); break;
        //                case CBeiDouSJXX.CMD_PREFIX: DealSJXX(msg); break;
        //                case CBeiDouCOUT.CMD_PREFIX: DealCOUT(msg); break;
        //                default: break;
        //            }
        //        }
        //        catch (Exception exp)
        //        {
        //            Debug.WriteLine(exp.Message);
        //        }
        //    }//end of while
        //}

        //  处理本机信息  BJXX数据类型

        private void DealData()
        {
            while (true)
            {
                try
                {
                    m_semephoreData.WaitOne();
                    m_mutexListByte.WaitOne(); //等待内存互斥量

                    var count = (from r in m_inputBuffer where (r == 13) select r).Count();

                    if (count == 0)
                    {
                        m_mutexListByte.ReleaseMutex(); //内存操作完毕，释放互斥量
                        continue;
                    }

                    m_mutexListByte.ReleaseMutex(); //内存操作完毕，释放互斥量
                    //gm1118
                    string flag = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>());

                    //1119
                    WriteToFileClass writeClass = new WriteToFileClass("ReceivedLog");
                    Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                    t.Start("Beidou500: " + "长度：" + " "  + flag + "\r\n");

                    Debug.WriteLine("接收消息:  " + flag);
                    if (flag.Contains(CBeiDouBJXX.CMD_PREFIX) || flag.Contains(CBeiDouZTXX.CMD_PREFIX) ||
                        flag.Contains(CBeiDouSJXX.CMD_PREFIX) || flag.Contains(CBeiDouCOUT.CMD_PREFIX))
                    {
                        if (flag.EndsWith("\r\n"))
                        {
                            int indexOfFirstBJXX = 0;
                            int indexOfFirstZTXX = 0;
                            int indexOfFirstSJXX = 0;
                            int indexOfFirstCOUT = 0;
                            string[] a = new string[] { "\r\n" };
                            string[] ArrData = flag.Split(a, StringSplitOptions.None);
                            for (int i = 0; i < ArrData.Count(); i++)
                            {
                                if (ArrData[i].Contains(CBeiDouBJXX.CMD_PREFIX))
                                {
                                    indexOfFirstBJXX = ArrData[i].IndexOf("BJXX");
                                    string msg = ArrData[i].Substring(indexOfFirstBJXX);
                                    DealBJXX(msg);
                                }
                                if (ArrData[i].Contains(CBeiDouZTXX.CMD_PREFIX))
                                {
                                    indexOfFirstZTXX = ArrData[i].IndexOf("ZTXX");
                                    string msg = ArrData[i].Substring(indexOfFirstZTXX);
                                    DealZTXX(msg);
                                }
                                if (ArrData[i].Contains(CBeiDouSJXX.CMD_PREFIX))
                                {
                                    indexOfFirstSJXX = ArrData[i].IndexOf("SJXX");
                                    string msg = ArrData[i].Substring(indexOfFirstSJXX);
                                    DealSJXX(msg);
                                }
                                //顶时报待测
                                if (ArrData[i].Contains(CBeiDouCOUT.CMD_PREFIX))
                                {
                                    indexOfFirstCOUT = ArrData[i].IndexOf("COUT");
                                    string msg = ArrData[i].Substring(indexOfFirstCOUT);
                                    DealCOUT(msg);
                                }
                            }
                            m_inputBuffer.Clear();
                        }
                    }

                }
                catch (Exception exp)
                {
                    Debug.WriteLine(exp.Message);
                }
            }//end of while
        }


        private void DealBJXX(string msg)
        {
            InvokeMessage("本机信息  " + msg.Trim(), "接收");
            var bjxx = Beidou500Helper.GetBJXXInfo(msg);
            if (null != Beidou500BJXXReceived)
                Beidou500BJXXReceived(null, new Beidou500BJXXEventArgs() { BJXXInfo = bjxx, RawMsg = msg });
        }
        //  处理状态检测 ZTXX数据类型
        private void DealZTXX(string msg)
        {
            InvokeMessage("状态检测  " + msg.Trim(), "接收");
            var ztxx = Beidou500Helper.GetZTXXInfo(msg);
            if (null != Beidou500ZTXXReceived)
                Beidou500ZTXXReceived(null, new Beidou500ZTXXEventArgs() { ZTXXInfo = ztxx, RawMsg = msg });
        }
        //  处理时间信息 SJXX数据类型
        private void DealSJXX(string msg)
        {
            InvokeMessage("时间信息  " + msg.Trim(), "接收");
            var sjxx = Beidou500Helper.GetSJXXInfo(msg);
            if (null != Beidou500SJXXReceived)
                Beidou500SJXXReceived(null, new Beidou500SJXXEventArgs() { SJXXInfo = sjxx, RawMsg = msg });

        }
        // 处理通信输出 COUT类型数据
        private void DealCOUT(string msg)
        {
            InvokeMessage("通信输出  " + msg.Trim(), "接收");
            string str = msg.Trim();
            if (str.Contains("COUT"))
            {
                if (str.Contains("COUT"))
                {
                    num = num + 1;
                    FileStream fs = new FileStream("numbd.txt", FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    //开始写入
                    sw.Write(num);
                    //清空缓冲区
                    sw.Flush();
                    //关闭流
                    sw.Close();
                    fs.Close();

                }
            }

            var cout = Beidou500Helper.GetCOUTInfo(msg);
            //InvokeMessage("gm1  " + "通过截取", "接收");
            if (cout == null)
            {
                //InvokeMessage("gm2  " + "通过截取", "接收");
                return;
            }
               

            //  解析通信输出中的内容
            string content = cout.MsgContent;
            try
            {
                string rawMsg = content;
                //InvokeMessage("gm3  " + content, "接收");
                string reportType = rawMsg.Substring(6, 2);
                //InvokeMessage("gm4  " + reportType, "接收");
                if (reportType == "21" || reportType == "22")   //   定时报，加报
                {
                    //  YAC设备的墒情协议：
                    string stationType = rawMsg.Substring(8, 2);
                    //InvokeMessage("gm5  " + stationType, "接收");
                    switch (stationType)
                    {
                        //  站类为04时墒情站 05墒情雨量站 06，16墒情水位站 07，17墒情水文站
                        case "04":
                        case "05":
                        case "06":
                        case "07":
                        case "17":
                            {
                                //var station = FindStationByBeidouID(cout.SenderAddr);
                                //string currentMsg = rawMsg.Insert(0, "$" + station.StationID + "1G");
                                //CEntitySoilData soil = new CEntitySoilData();
                                //CReportStruct soilReport = new CReportStruct();
                                //if (Soil.Parse(currentMsg, out soil, out soilReport))
                                //{
                                //    soil.ChannelType = EChannelType.BeiDou;

                                //    if (null != this.SoilDataReceived)
                                //        this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                                //    if (null != soilReport && null != this.UpDataReceived)
                                //    {
                                //        soilReport.ChannelType = EChannelType.BeiDou;
                                //        soilReport.ListenPort = "COM" + this.Port.PortName;
                                //        this.UpDataReceived(null, new UpEventArgs() { RawData = rawMsg, Value = soilReport });
                                //    }
                                //}
                               // string newMsg = rawMsg.Substring(1, rawMsg.Length - 1);
                                CEntitySoilData soil = new CEntitySoilData();
                                CReportStruct soilReport = new CReportStruct();
                                if (Soil.Parse(rawMsg, out soil, out soilReport))
                                {
                                    soilReport.ChannelType = EChannelType.BeiDou;
                                    if (null != this.SoilDataReceived)
                                        this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                                    if (null != soilReport && null != this.UpDataReceived)
                                    {
                                        soilReport.ChannelType = EChannelType.BeiDou;
                                        soilReport.ListenPort = "COM" + this.Port.PortName;
                                        soilReport.flagId = cout.SenderAddr;
                                        this.UpDataReceived(null, new UpEventArgs() { RawData = rawMsg, Value = soilReport });
                                    }
                                }
                            }
                            break;
                        //  站类为01,02,03,12,13时，不是墒情站
                        case "01":
                        case "02":
                        case "03":
                        case "12":
                        case "13":
                            {
                                //var station = FindStationByBeidouID(cout.SenderAddr);
                                //rawMsg = rawMsg.Insert(0, station.StationID + "  ");

                                //CReportStruct report = new CReportStruct();
                                //if (Up.Parse(rawMsg, out report))
                                //{
                                //    report.ChannelType = EChannelType.BeiDou;
                                //    report.ListenPort = "COM" + this.Port.PortName;
                                //    if (this.UpDataReceived != null)
                                //        this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = msg });
                                //}
                                //string newMsg = rawMsg.Substring(1, rawMsg.Length - 1);
                                CReportStruct report = new CReportStruct();
                                if (Up.Parse(rawMsg, out report))
                                {
                                    //InvokeMessage("gm6  " + rawMsg, "接收");
                                    //$60131G2201161111040003046112271367
                                    report.ChannelType = EChannelType.BeiDou;
                                    report.ListenPort = "COM" + this.Port.PortName;
                                    report.flagId = cout.SenderAddr;
                                    if (this.UpDataReceived != null)
                                    {
                                        //InvokeMessage("Cout Test  " + rawMsg, "接收");
                                        this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = rawMsg });
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }



                }
                else if (reportType == "11")    //  人工水位
                {

                }
                else if (reportType == "23")    //  人工流量
                {

                }
                else if (reportType == "25")
                {
                    var station = FindStationByBeidouID(cout.SenderAddr);
                    string currentMsg = rawMsg.Insert(0, "$" + station.StationID + "1G");

                    CEntitySoilData soil = new CEntitySoilData();
                    CReportStruct soilReport = new CReportStruct();
                    if (Soil.Parse(currentMsg, out soil, out soilReport))
                    {
                        soil.ChannelType = EChannelType.BeiDou;

                        if (null != this.SoilDataReceived)
                            this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                        if (null != soilReport && null != this.UpDataReceived)
                        {
                            soilReport.ChannelType = EChannelType.BeiDou;
                            soilReport.ListenPort = "COM" + this.Port.PortName;
                            this.UpDataReceived(null, new UpEventArgs() { RawData = rawMsg, Value = soilReport });
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("北斗卫星指挥机 数据解析出错 ！" + content + "\r\n" + exp.Message);
            }
        }

        private CEntityStation FindStationByBeidouID(string bid)
        {
            if (this.StationLists == null)
                throw new Exception("北斗卫星模块未初始化站点！");

            CEntityStation result = null;
            foreach (var station in this.StationLists)
            {
                if (station.BDSatellite.Equals(bid, StringComparison.OrdinalIgnoreCase))
                {
                    result = station;
                    break;
                }
            }
            return result;
        }
        #endregion

        #region 日志
        /// <summary>
        /// 日志记录，用事件返回出去
        /// </summary>
        /// <param name="msg">接受，发送的数据</param>
        /// <param name="description">对数据的描述，一般为:接受，发送，初始化</param>
        private void InvokeMessage(string msg, string description)
        {
            if (this.MessageSendCompleted != null)
                this.MessageSendCompleted(null, new SendOrRecvMsgEventArgs()
                {
                    ChannelType = this.m_channelType,
                    Msg = msg,
                    Description = description
                });
        }
        #endregion

        #region 发送指令
        /// <summary>
        /// 查询北斗卫星所有状态信息
        /// 包括：用户信息(HQYH)、时间输出(CXSJ)、状态检测(ZTJC)
        /// </summary>
        public void Query()
        {
            SendHQYH();// 获取用户信息
            SendZTJC();// 状态检测
            SendCXSJ();// 请求时间输出
            //SendZTJC();// 状态检测
        }

        public String Send500TTCA(CBeiDouTTCA param)
        {
            string text = Beidou500Helper.GetTTCAStr(param);
            Debug.WriteLine("发送TTCA：" + text);
            InvokeMessage("自发自收  " + text, "发送");
            SendText(text);
            //    return SendBackTTCA();
              return "";
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        private void SendHQYH()
        {
            string text = Beidou500Helper.GetHQYHStr();
            Debug.WriteLine("发送HQYH：" + text);
            InvokeMessage("获取用户信息  " + text, "发送");
            SendText(text);
        }
        /// <summary>
        /// 请求时间输出
        /// </summary>
        private void SendCXSJ()
        {
            string text = Beidou500Helper.GetCXSJStr();
            Debug.WriteLine("发送CXSJ：" + text);
            InvokeMessage("请求时间输出  " + text, "发送");
            SendText(text);
        }
        /// <summary>
        /// 请求时间输出
        /// </summary>
        public void Send500CXSJ()
        {
            string text = Beidou500Helper.GetCXSJStr();
            Debug.WriteLine("发送CXSJ：" + text);
            InvokeMessage("授时申请  " + text, "发送");
            SendText(text);
        }
        /// <summary>
        /// 状态检测($ZTJC)
        /// </summary>
        private void SendZTJC()
        {
            string text = Beidou500Helper.GetZTJCStr();
            Debug.WriteLine("发送ZTJC：" + text);
            InvokeMessage("状态检测  " + text, "发送");
            SendText(text);
        }
        private void SendText(string msg)
        {
            if (this.SerialPortStateChanged != null)
                this.SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                {
                    BNormal = false,
                    PortNumber = Int32.Parse(Port.PortName.Replace("COM", "")),
                    PortType = this.m_portType
                }));
            try
            {
                this.Port.Write(msg);
            }
            catch (Exception exp)
            { }
        }
        #endregion

        #region 计时器
        /// <summary>
        /// 计时器事件，每隔45Min读取一次参数
        /// 如果有参数返回，正常的显示绿灯，不正常的显示红灯
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Port.IsOpen)
            {
                return;
            }
            var time = DateTime.Now;
            if (time.Minute == 45 || time.Minute == 26)
            {
                if (this.SerialPortStateChanged != null)
                    this.SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                    {
                        BNormal = false,
                        PortNumber = Int32.Parse(Port.PortName.Replace("COM", "")),
                        PortType = this.m_portType
                    }));

                Query();
            }
        }
        #endregion

        #region 事件定义
        //public event EventHandler<TSTAEventArgs> TSTACompleted;
        //public event EventHandler<COUTEventArgs> COUTCompleted;
        //public event EventHandler<ReceiveErrorEventArgs> BeidouErrorReceived;

        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<Beidou500BJXXEventArgs> Beidou500BJXXReceived;
        public event EventHandler<Beidou500ZTXXEventArgs> Beidou500ZTXXReceived;
        public event EventHandler<Beidou500SJXXEventArgs> Beidou500SJXXReceived;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;

        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;

        public event EventHandler<UpEventArgs> UpDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        #endregion

        /// <summary>
        /// 查询指令与解析数据类
        /// </summary>
        internal class Beidou500Helper
        {
            /// <summary>
            /// 2.1.	获取用户信息($HQYH)
            /// </summary>
            public static string GetHQYHStr()
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CBeiDouHQYH.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append("0");
                rawStr.Append(",");
                rawStr.Append("0");
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 2.2.	返回本机用户信息($BJXX)
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            public static CBeiDouBJXX GetBJXXInfo(string msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);

                    if (cmd != CBeiDouBJXX.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');

                    if (segs.Length != 9)
                        throw new Exception("数据长度不对！");

                    return new CBeiDouBJXX()
                    {
                        CardNum = segs[1],
                        LocalAddr = segs[2],
                        BroadCastAddr = segs[3],
                        ServiceFrequency = segs[4],
                        ConfidentFlag = segs[5],
                        CommLevel = segs[6],
                        ValidFlag = segs[7],
                        CheckSum = segs[8]
                    };
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }

            /// <summary>
            /// 3.1.	状态检测($ZTJC)
            /// </summary>
            public static String GetZTJCStr()
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CBeiDouZTJC.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append("0");
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 3.2.	状态输出($ZTXX)
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            public static CBeiDouZTXX GetZTXXInfo(string msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);

                    if (cmd != CBeiDouZTXX.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');

                    if (segs.Length != 15)
                        throw new Exception("数据长度不对！");

                    return new CBeiDouZTXX()
                    {
                        CardStatus = segs[1],
                        WholeMachineState = segs[2],
                        InStationStatus = segs[3],
                        Electricity = segs[4],
                        ResponseBeam = segs[5],
                        DifferenceBeam = segs[6],
                        SignalStrength1 = segs[7],
                        SignalStrength2 = segs[8],
                        SignalStrength3 = segs[9],
                        SignalStrength4 = segs[10],
                        SignalStrength5 = segs[11],
                        SignalStrength6 = segs[12],
                        ReceiptStatus = segs[13],
                        CheckSum = segs[14]
                    };
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }
            /// <summary>
            /// 3.3.	请求时间输出($CXSJ)
            /// </summary>
            public static String GetCXSJStr()
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CBeiDouCXSJ.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append("0");
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 3.4.	返回时间信息($SJXX)
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            public static CBeiDouSJXX GetSJXXInfo(string msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);

                    if (cmd != CBeiDouSJXX.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');

                    if (segs.Length != 8)
                        throw new Exception("数据长度不对！");

                    return new CBeiDouSJXX()
                    {
                        Year = segs[1],
                        Month = segs[2],
                        Day = segs[3],
                        Hour = segs[4],
                        Minute = segs[5],
                        Second = segs[6],
                        CheckSum = segs[7],
                    };
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }
            /// <summary>
            /// 4.1.	终端到终端通信申请（Terminal to Terminal Communication Application）
            /// </summary>
            /// <param name="param"></param>
            /// <returns></returns>
            public static String GetTTCAStr(CBeiDouTTCA param)
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CTTCAStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(param.PeripheralReportNum);         // 外设报文序号
                rawStr.Append(",");
                rawStr.Append(param.SenderID);         // 发信方ID
                rawStr.Append(",");
                rawStr.Append(param.ReceiverAddr);     // 收信方地址
                rawStr.Append(",");
                rawStr.Append(param.ConfidentialityRequirements);// 保密要求
                rawStr.Append(",");
                rawStr.Append(param.ReceiptFlag);// 回执标志
                rawStr.Append(",");
                rawStr.Append(param.MsgLength);       // 电文长度
                rawStr.Append(",");
                rawStr.Append(param.MsgContent);       // 电文内容
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 4.2.	通信输出（Communication OUTput）
            /// </summary>
            /// <param name="msg"></param> 
            /// <returns></returns>
            //public static CBeiDouCOUT GetCOUTInfo(string msg)
            //{
            //    try
            //    {
            //        String cmd = msg.Substring(0, 4);
            //        if (cmd != CBeiDouCOUT.CMD_PREFIX)
            //            throw new Exception("数据格式不对！");
            //        msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
            //        String[] segs = msg.Split(',');

            //        if (segs.Length != 9)
            //            throw new Exception("数据长度不对！");
            //        return new CBeiDouCOUT()
            //        {
            //            CRCFlag = segs[1],
            //            CommType = segs[2],
            //            SenderType = segs[3],
            //            SenderAddr = segs[4],
            //            ReceiptFlag = segs[5],
            //            ReportNum = segs[6],
            //            MsgLength = segs[7],
            //            MsgContent = segs[8],
            //            //RecipientID = segs[9],
            //            //ChannelNum = segs[10],
            //            //BeamNum = segs[11],
            //            //CheckSum = segs[12]
            //        };
            //    }
            //    catch (Exception exp) { Debug.WriteLine(exp); }
            //    return null;
            //}
            public static CBeiDouCOUT GetCOUTInfo(string msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);
                    if (cmd != CBeiDouCOUT.CMD_PREFIX)
                        throw new Exception("数据格式不对！");
                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');

                    if (segs.Length != 13)
                        throw new Exception("数据长度不对！");
                    return new CBeiDouCOUT()
                    {
                        CRCFlag = segs[1],
                        CommType = segs[2],
                        SenderType = segs[3],
                        SenderAddr = segs[4],
                        ReceiptFlag = segs[5],
                        ReportNum = segs[6],
                        MsgLength = segs[7],
                        MsgContent = segs[8],
                        RecipientID = segs[9],
                        ChannelNum = segs[10],
                        BeamNum = segs[11],
                        CheckSum = segs[12]
                    };
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }
            /// <summary>
            /// 生成CRC校验码
            /// </summary>
            /// <param name="rawStr">CRC校验码之前的字符串</param>
            /// <returns>CRC校验码</returns>
            private static String GenerateCRC(string rawStr)
            {
                byte[] byteArray = System.Text.Encoding.Default.GetBytes(rawStr);
                byte crc = byteArray[0];
                for (int i = 1; i < byteArray.Length; i++)
                {
                    crc = (byte)(crc ^ byteArray[i]);
                }
                return ((char)crc).ToString();
            }
        }
    }
}

