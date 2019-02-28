using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Hydrology.Entity;
using Protocol.Channel.Interface;
using System.Threading;
using Protocol.Data.Interface;
using Protocol.Channel.Gprs;
using System.IO;

namespace Protocol.Channel.Beidou
{
    public class BeidouNormal : IBeidouNormal
    {
        public SerialPort Port { get; set; }
        public Dictionary<int, bool> BeidouStatus { get; set; }
        public IUp Up { get; set; }
        public IDown Down { get; set; }
        public IUBatch UBatch { get; set; }
        public IFlashBatch FlashBatch { get; set; }
        public ISoil Soil { get; set; }
        public List<CEntityStation> StationLists { get; set; }
        public Boolean IsCommonWorkNormal { get; set; }
        public String SendBackTTCAString { get; set; }

        #region 私有成员变量
        private List<string> m_listDatas;
        private List<byte> m_inputBuffer;
        private System.Timers.Timer m_timer;    //  定时器：时间间隔45Min
        private bool m_isAdjustBeam = false;      //  是否正在调整通道一的波束
        private int m_AdjustCounter = 0;

        public static int num = 0;

        private EChannelType m_channelType = EChannelType.BeidouNormal;
        private EListeningProtType m_portType = EListeningProtType.SerialPort;

        // 多线程相关
        private Mutex m_mutexListByte;      // 对InputBuffer的独占访问权
        private Semaphore m_semephoreData;  // 用来唤醒数据处理线程
        private Thread m_threadDealData;    // 处理数据的进程 
        #endregion

        #region 初始化
        public BeidouNormal()
        {
            // 构造函数，开启数据处理进程
            m_mutexListByte = new Mutex();
            m_semephoreData = new Semaphore(0, Int32.MaxValue);

            m_threadDealData = new Thread(new ThreadStart(DealData))
            {
                Name = "北斗卫星普通终端数据处理线程"
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
            this.BeidouStatus = new Dictionary<int, bool>();
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
                        Name = "北斗卫星普通终端数据处理线程"
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
            m_inputBuffer.AddRange(buf);
            m_semephoreData.Release(1); //释放信号量，通知消费者
        }
        public String SendBackTTCA()
        {
            // String resultTTCA = null;
            //if (result != "")
            //    return result;
            //else
            //    return "";
            return SendBackTTCAString;
        }
        // 处理缓冲区中的数据
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
                    // gm 1117
                    string flag = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>());

                    //1119
                    WriteToFileClass writeClass = new WriteToFileClass("ReceivedLog");
                    Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                    t.Start("Beidou: " +"长度:" + flag.Length + " "  + flag + "\r\n");

                    Debug.WriteLine("接收消息:  " + flag);
                    if (flag.Contains(CCASSStruct.CMD_PREFIX) || flag.Contains(CTSTAStruct.CMD_PREFIX) ||
                        flag.Contains(CTTCAStruct.CMD_PREFIX) || flag.Contains(CCOUTStruct.CMD_PREFIX) ||
                        flag.Contains(CTINFStruct.CMD_PREFIX))
                    {
                        if (flag.EndsWith("\r\n"))
                        {
                            int indexOfFirstCASS = 0;
                            int indexOfFirstTSTA = 0;
                            int indexOfFirstTTCA = 0;
                            int indexOfFirstCOUT = 0;
                            int indexOfFirstTINF = 0;
                            string[] a = new string[] { "\r\n" };
                            string[] ArrData = flag.Split(a, StringSplitOptions.None);
                            for (int i = 0; i < ArrData.Count(); i++)
                            {
                                if (ArrData[i].Contains(CCASSStruct.CMD_PREFIX))
                                {
                                    indexOfFirstCASS = ArrData[i].IndexOf("CASS");
                                    string msg = ArrData[i].Substring(indexOfFirstCASS);
                                    DealCASS(msg);
                                }
                                if (ArrData[i].Contains(CTSTAStruct.CMD_PREFIX))
                                {
                                    indexOfFirstTSTA = ArrData[i].IndexOf("TSTA");
                                    string msg = ArrData[i].Substring(indexOfFirstTSTA);
                                    DealTSTA(msg);
                                }
                                if (ArrData[i].Contains(CTTCAStruct.CMD_PREFIX))
                                {
                                    indexOfFirstTSTA = ArrData[i].IndexOf("TTCA");
                                    string msg = ArrData[i].Substring(indexOfFirstTTCA);
                                    DealTTCA(msg);
                                }
                                //顶时报待测
                                if (ArrData[i].Contains(CCOUTStruct.CMD_PREFIX))
                                {
                                    indexOfFirstTSTA = ArrData[i].IndexOf("COUT");
                                    string msg = ArrData[i].Substring(indexOfFirstCOUT);
                                    DealCOUT(msg);
                                }
                                if (ArrData[i].Contains(CTINFStruct.CMD_PREFIX))
                                {
                                    indexOfFirstTSTA = ArrData[i].IndexOf("TINF");
                                    string msg = ArrData[i].Substring(indexOfFirstTINF);
                                    DealTAPP(msg);
                                }
                            }
                            m_inputBuffer.Clear();
                        }
                    }

                    //int indexOfFirst13 = 0;


                    //  // int indexOfFirst13 = 0;
                    //   //indexOfFirst13 = m_inputBuffer.IndexOf(13);
                    //   //var msgBytes = m_inputBuffer.GetRange(0, indexOfFirst13 + 1);

                    //   //this.m_inputBuffer.RemoveRange(0, indexOfFirst13 + 1);
                    //   m_mutexListByte.ReleaseMutex(); //内存操作完毕，释放互斥量

                    //   //string msg = System.Text.Encoding.Default.GetString(msgBytes.ToArray());

                    //   int indexOfDollar = msg.IndexOf('$');
                    //   string tag = msg.Substring(indexOfDollar + 1, 4);

                    //   msg = msg.Replace("\r", "").Replace("\n", "");
                    //   Debug.WriteLine("接收消息:  " + msg);
                    ////   InvokeMessage("dealdata  " + msg.Trim().Length, ",,,接收");
                    //   switch (tag)
                    //   {
                    //       case CCASSStruct.CMD_PREFIX: DealCASS(msg); break;
                    //       case CTSTAStruct.CMD_PREFIX: DealTSTA(msg); break;
                    //       case CTTCAStruct.CMD_PREFIX: DealTTCA(msg); break;
                    //       case CCOUTStruct.CMD_PREFIX: { DealCOUT(msg);  break; }
                    //       case CTINFStruct.CMD_PREFIX: { DealTAPP(msg); InvokeMessage("TAPP...", "接收"); break; }
                    //       default: break;
                    //   }
                }
                catch (Exception exp)
                {
                    Debug.WriteLine(exp.Message);
                }
            }//end of while
        }
        // 处理CASS类型数据
        private void DealCASS(string msg)
        {
            InvokeMessage("通信申请成功状态  " + msg.Trim(), "接收");
            var cass = BeidouHelper.GetCASSInfo(msg);
            if (cass == null)
                return;
            if (!cass.SuccessStatus)
                SendQSTA();
        }
        // 处理TSTA类型数据
        private void DealTSTA(string msg)
        {
            try
            {
                InvokeMessage("终端状态信息  " + msg, "接收");
                var tsta = BeidouHelper.GetTSTAInfo(msg);
                if (tsta == null)
                    return;

                if (this.TSTACompleted != null)
                    this.TSTACompleted(null, new TSTAEventArgs() { TSTAInfo = tsta, RawMsg = msg });
                Debug.WriteLine("通道一波束强度  :     " + tsta.Channel1RecvPowerLevel);
                Debug.WriteLine("通道一波束      :     " + tsta.Channel1LockingBeam);

                if (this.m_AdjustCounter > 6)
                {
                    this.m_AdjustCounter = 0;
                    if (this.BeidouErrorReceived != null)
                        this.BeidouErrorReceived(null, new ReceiveErrorEventArgs() { Msg = "波束1~6的波束强度均小于3" });
                    return;
                }

                if (tsta.Channel1RecvPowerLevel < 3)
                {
                    m_isAdjustBeam = true;
                    m_AdjustCounter += 1;
                    var temp = tsta.Channel1RecvPowerLevel;
                    var beam = tsta.Channel1LockingBeam % 6 + 1;
                    Debug.WriteLine("调整通道一波束  :     " + beam);
                    InvokeMessage(String.Format("调整通道一波束为{0}号波束", beam), "接收");
                    var baudrate = tsta.SerialBaudRate;
                    AdjustBeam(beam, baudrate);
                    //  查询终端状态
                    SendQSTA();
                }
                else
                {
                    if (m_isAdjustBeam)
                    {
                        Debug.WriteLine("自发自收" + tsta.TerminalID);
                        //  自发自收
                        SendTTCA(tsta.TerminalID);
                    }
                    this.m_isAdjustBeam = false;
                    this.m_AdjustCounter = 0;
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }
        // 处理TTCA类型数据
        private void DealTTCA(string msg)
        {
            var ttca = BeidouHelper.GetTTCAInfo(msg);
            if (ttca == null)
                return;
            ttca.SenderID = "1";        //  本机ID
            ttca.RecvAddr = "ID";       //  神州天鸿终端ID号
            ttca.Requirements = "1";    //  不保密
            ttca.ReceiptSign = false;   //  不回执
            ttca.MsgLength = 75;        //  电文长度
            //  拼包
            var bytes = new List<byte>();
            for (int index = 0; index < this.BeidouStatus.Count; index = index + 8)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(this.BeidouStatus[index + 0] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 1] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 2] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 3] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 4] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 5] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 6] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 7] ? "1" : "0");
                byte byt = byte.Parse(builder.ToString());
                bytes.Add(byt);
            }
            ttca.MsgContent = System.Text.Encoding.Default.GetString(bytes.ToArray());//  电文内容
        }
        // 处理COUT类型数据
        private void DealCOUT(string msg)
        //  private String DealCOUT(string msg)
        {
            string result = null;
            InvokeMessage("通信输出  " + msg, "接收");
            SendBackTTCAString = msg;
            if (msg.Contains("COUT"))
            {
                if (msg.Contains("COUT"))
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
                // InvokeString(msg);
                var cout = BeidouHelper.GetCOUTInfo(msg);
            if (cout == null)
            {
                return;
            }

            //  return result;
            //  发送COSS指令
            //  必须在1秒内发送给卫星终端，否则会重新发送“通信输出”
            var coss = new CCOSSStruct();
            coss.SuccessStatus = true;          //  终端接收到外设通信申请，并校验成功
            SendCOSS(coss);
            //  发送CACA指令
            //  每隔一分钟发送回执$CACA
            //  65秒左右发一条
            var caca = new CCACAStruct();
            caca.SenderID = "1";                //  发信方ID为1，表示本机ID，默认
            caca.RecvType = cout.SenderType;    //  回执的收信方类型  ==  通信输出中的发信方类型
            caca.RecvAddr = cout.SenderAddr;    //  回执的收信方地址  ==  通信输出中的发信方地址
            caca.Requirements = "1";            //  不保密
            caca.ReceiptMsgSequenceNum = cout.MsgSequenceNum;// 回执的报文顺序号  ==  通信输出中的报文顺序号
            caca.ReceiptContent = "1";
            SendCACA(caca);

            //  解析通信输出中的内容
            string content = cout.MsgContent;
            //通信输出gm8  $60131G2201161111040003046112271367
            try
            {
                string rawMsg = content;
                //rawMsg = $60131G2201161111040003046112271367
                string reportType = rawMsg.Substring(7, 2);
                if (reportType == "21" || reportType == "22")   //   定时报，加报
                {

                    //  YAC设备的墒情协议：
                    string stationType = rawMsg.Substring(9, 2);
                    switch (stationType)
                    {
                        //  站类为04时墒情站 05墒情雨量站 06，16墒情水位站 07，17墒情水文站
                        case "04":
                        case "05":
                        case "06":
                        case "07":
                        case "17":
                            {
                                //    var station = FindStationByBeidouID(cout.SenderAddr);
                                //    string currentMsg = rawMsg.Insert(0, "$" + station.StationID + "1G");
                                //    CEntitySoilData soil = new CEntitySoilData();
                                //    CReportStruct soilReport = new CReportStruct();
                                //    if (Soil.Parse(currentMsg, out soil, out soilReport))
                                //    {
                                //        soil.ChannelType = EChannelType.BeiDou;

                                //        if (null != this.SoilDataReceived)
                                //            this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                                //        if (null != soilReport && null != this.UpDataReceived)
                                //        {
                                //            soilReport.ChannelType = EChannelType.BeiDou;
                                //            soilReport.ListenPort = "COM" + this.Port.PortName;
                                //            this.UpDataReceived(null, new UpEventArgs() { RawData = rawMsg, Value = soilReport });
                                //        }
                                //    }

                                //}
                                //1111gm
                                string newMsg = rawMsg.Substring(1, rawMsg.Length - 1);
                                CEntitySoilData soil = new CEntitySoilData();
                                CReportStruct soilReport = new CReportStruct();
                                if (Soil.Parse(newMsg, out soil, out soilReport))
                                {
                                    soilReport.ChannelType = EChannelType.BeiDou;
                                    if (null != this.SoilDataReceived)
                                        this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                                    if (null != soilReport && null != this.UpDataReceived)
                                    {
                                        soilReport.ChannelType = EChannelType.GPRS;
                                        soilReport.ListenPort = "COM" + this.Port.PortName;
                                        soilReport.flagId = cout.SenderAddr;
                                        this.UpDataReceived(null, new UpEventArgs() { RawData = newMsg, Value = soilReport });
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
                                //1111gm
                                string newMsg = rawMsg.Substring(1, rawMsg.Length - 1);
                                CReportStruct report = new CReportStruct();
                                if (Up.Parse(newMsg, out report))
                                {
                                    //6013 $60131G2201161111040003046112271367
                                    report.ChannelType = EChannelType.BeiDou;
                                    report.ListenPort = "COM" + this.Port.PortName;
                                    report.flagId = cout.SenderAddr;
                                    if (this.UpDataReceived != null)
                                    {
                                        this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = newMsg });
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
                            soilReport.flagId = cout.SenderAddr;
                            this.UpDataReceived(null, new UpEventArgs() { RawData = rawMsg, Value = soilReport });
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("Beidou 数据解析出错 ！" + content + "\r\n" + exp.Message);
            }
            //result = msg;
            //return result;
        }

        //TAPP,9.28
        private void DealTAPP(string msg)
        {
            InvokeMessage("通信输出  " + msg, "接收");
        }
        // 调整波束
        private void AdjustBeam(int beam, int baudrate = 3)
        {
            var stst = new CSTSTStruct();
            stst.ServiceFrequency = "60";       //  服务频度
            stst.SerialBaudRate = baudrate;     //  串口波特率，默认为为3，9.6Kbps
            stst.ResponseOfBeam = beam;         //  响应波束为: 1~7波束，
            stst.AcknowledgmentType = false;    //  默认打开系统回执，关闭通信回执
            SendSTST(stst); //  发送调整波束命令    
        }
        public void SendTTCA(string terminalID)
        {
            var ttca = new CTTCAStruct();
            ttca.SenderID = "1";    //  本机ID
            ttca.RecvAddr = terminalID;
            ttca.Requirements = "1";    //  不保密
            ttca.ReceiptSign = false;   //  不回执
            ttca.MsgLength = 75;        //  电文长度
            //  拼包
            var bytes = new List<byte>();
            for (int index = 0; index < this.BeidouStatus.Count; index = index + 8)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(this.BeidouStatus[index + 0] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 1] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 2] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 3] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 4] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 5] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 6] ? "1" : "0");
                builder.Append(this.BeidouStatus[index + 7] ? "1" : "0");
                byte byt = byte.Parse(builder.ToString());
                bytes.Add(byt);
            }
            ttca.MsgContent = System.Text.Encoding.Default.GetString(bytes.ToArray());//  电文内容
            SendTTCA(ttca);
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
        ////返回到主程序String
        //private void InvokeString(string msg)
        //{
        //    if (this.MessageSendCompleted != null)
        //        this.MessageSendCompleted(null, new SendOrRecvMsgEventArgs1()
        //        {
        //          //  ChannelType = this.m_channelType,
        //            Msg = msg,
        //          //  Description = description
        //        });
        //}

        #endregion

        #region 发送指令
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
            {

            }
        }



        public void SendQSTA()
        {
            string text = BeidouHelper.GetQSTAStr(null);
            Debug.WriteLine("发送QSTA：" + text);
            InvokeMessage("查询终端状态  " + text, "发送");
            SendText(text);
        }
        public void SendSTST(CSTSTStruct param)
        {
            string text = BeidouHelper.GetSTSTStr(param);
            Debug.WriteLine("发送STST：" + text);
            InvokeMessage("设置终端状态  " + text, "发送");
            SendText(text);
        }
        public String SendTTCA(CTTCAStruct param)
        {
            string text = BeidouHelper.GetTTCAStr(param);
            Debug.WriteLine("发送TTCA：" + text);
            InvokeMessage("自发自收  " + text, "发送");
            SendText(text);
            //    return SendBackTTCA();
            return "";
        }
        private void SendCOSS(CCOSSStruct param)
        {
            string text = BeidouHelper.GetCCOSSStr(param);
            Debug.WriteLine("发送COSS：" + text);
            InvokeMessage("通信输出成功状态  " + text, "发送");
            SendText(text);
        }
        private void SendCACA(CCACAStruct param)
        {
            string text = BeidouHelper.GetCACAStr(param);
            Debug.WriteLine("发送CACA：" + text);
            InvokeMessage("通信回执申请  " + text, "发送");
            SendText(text);
        }
        //TAPP
        //  public void SendTAPP(CTAPPStruct param)
        public void SendTAPP()
        {
            string text = BeidouHelper.GetTAPPStr();
            Debug.WriteLine("发送CTAPP：");
            //InvokeMessage("授时申请  " , "发送");
            InvokeMessage("授时申请  " + text, "发送");
            SendText(text);
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

                SendQSTA();
            }
        }
        #endregion

        #region 事件定义
        public event EventHandler<TSTAEventArgs> TSTACompleted;
        public event EventHandler<COUTEventArgs> COUTCompleted;
        public event EventHandler<ReceiveErrorEventArgs> BeidouErrorReceived;
        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;

        public event EventHandler<UpEventArgs> UpDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        #endregion

        /// <summary>
        /// 查询指令与解析数据类
        /// </summary>
        internal class BeidouHelper
        {
            /// <summary>
            /// 查询终端状态,读取参数
            /// $QSTA
            /// </summary>
            public static String GetQSTAStr(CQSTAStruct param)
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CQSTAStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 解析查询终端成功状态信息
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            public static CCASSStruct GetCASSInfo(String msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);

                    if (cmd != CCASSStruct.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');

                    if (segs.Length != 3)
                        throw new Exception("数据长度不对！");

                    CCASSStruct result = new CCASSStruct();
                    result.SuccessStatus = segs[1].Equals("1") ? true : false;
                    result.CheckSum = segs[2];
                    return result;
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }
            /// <summary>
            /// 解析终端状态信息
            /// TSTA
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            public static CTSTAStruct GetTSTAInfo(String msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);
                    if (cmd != CTSTAStruct.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');
                    if (segs.Length != 15)
                        throw new Exception("数据长度不对！");


                    CTSTAStruct result = new CTSTAStruct();
                    result.Channel1RecvPowerLevel = int.Parse(segs[1]);
                    result.Channel2RecvPowerLevel = int.Parse(segs[2]);
                    result.Channel1LockingBeam = int.Parse(segs[3]);
                    result.Channel2LockingBeam = int.Parse(segs[4]);
                    result.ResponseOfBeam = int.Parse(segs[5]);
                    result.SignalSuppression = segs[6].Equals("1") ? true : false;
                    result.PowerState = segs[7].Equals("1") ? true : false;
                    result.TerminalID = segs[8];
                    result.BroadcastAddr = segs[9];
                    result.ServiceFrequency = segs[10];
                    result.SerialBaudRate = int.Parse(segs[11]);
                    result.SecurityModuleState = int.Parse(segs[12]);
                    result.BarometricAltimetryModuleState = int.Parse(segs[13]);
                    result.CheckSum = segs[14];
                    return result;
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }

            /// <summary>
            /// 设置终端状态，设置参数
            /// $STST
            /// </summary>
            public static String GetSTSTStr(CSTSTStruct param)
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CSTSTStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(param.ServiceFrequency);     // 服务频度
                rawStr.Append(",");
                rawStr.Append(param.SerialBaudRate);       // 串口波特率
                rawStr.Append(",");
                rawStr.Append(param.ResponseOfBeam);       // 响应波束
                rawStr.Append(",");
                rawStr.Append(param.AcknowledgmentType ? "1" : "0");   // 回执类型
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 终端到终端通信申请,自发自收
            /// 获取自发自收查询字符串
            /// $TTCA
            /// </summary>
            public static String GetTTCAStr(CTTCAStruct param)
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CTTCAStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(param.SenderID);         // 发信方ID
                rawStr.Append(",");
                rawStr.Append(param.RecvAddr);         // 收信方地址
                rawStr.Append(",");
                rawStr.Append(param.Requirements);     // 保密要求
                rawStr.Append(",");
                rawStr.Append(param.ReceiptSign ? "1" : "0");// 回执标志
                rawStr.Append(",");
                rawStr.Append(param.MsgLength.ToString());// 电文长度
                rawStr.Append(",");
                rawStr.Append(param.MsgContent);       // 电文内容
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 终端到终端通信申请,自发自收
            /// 解析自发自收返回后的结果
            /// $TTCA
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            public static CTTCAStruct GetTTCAInfo(String msg)
            {
                try
                {
                    String cmd = msg.Substring(0, 4);
                    if (cmd != CTTCAStruct.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("$", "").Replace("\r", "").Replace("\n", "");
                    String[] segs = msg.Split(',');
                    if (segs.Length != 8)
                        throw new Exception("数据长度不对！");

                    CTTCAStruct result = new CTTCAStruct();
                    result.SenderID = segs[1];
                    result.RecvAddr = segs[2];
                    result.Requirements = segs[3];
                    result.ReceiptSign = segs[4].Equals("1") ? true : false;
                    result.MsgLength = int.Parse(segs[5]);
                    result.MsgContent = segs[6];
                    result.CheckSum = segs[7];
                    return result;
                }
                catch (Exception exp) { Debug.WriteLine(exp); }
                return null;
            }

            /// <summary>
            /// 通信输出，接收数据
            /// $COUT
            /// </summary>
            public static CCOUTStruct GetCOUTInfo(String msg)
            {
                BeidouNormal bdn = new BeidouNormal();
                try
                {
                    String cmd = msg.Substring(1, 4);
                    if (cmd != CCOUTStruct.CMD_PREFIX)
                        throw new Exception("数据格式不对！");

                    msg = msg.Replace("\r", "").Replace("\n", "");
                    if (msg.StartsWith("$"))
                    {
                        msg = msg.Substring(1);
                    }
                    String[] segs = msg.Split(',');
                    Debug.WriteLine(segs.Length.ToString() + "testlength");
                    if (segs.Length != 10)
                        throw new Exception("数据长度不对！");
                    CCOUTStruct result = new CCOUTStruct();
                    result.CRCCheckMark = segs[1].Equals("1") ? true : false;
                    result.CommType = segs[2];
                    result.SenderType = segs[3];
                    result.SenderAddr = segs[4];
                    result.ReceiptSign = segs[5].Equals("1") ? true : false;
                    //Debug.WriteLine(segs.Length.ToString() + "testlength2");
                    result.MsgSequenceNum = segs[6];
                    result.MsgLength = int.Parse(segs[7]);
                    result.MsgContent = segs[8];
                    //Debug.WriteLine(segs.Length.ToString() + "testlength3");
                    //bdn.InvokeMessage("通信输出gm3.2 " + segs.ToString(), "接收");
                    result.CheckSum = segs[9];

                    return result;
                }
                catch (Exception exp) { Debug.WriteLine(exp); Debug.WriteLine("testlength4"); }
                return null;
            }
            /// <summary>
            /// 通信申请成功状态，发成功状态
            /// $COSS
            /// </summary>
            public static String GetCCOSSStr(CCOSSStruct param)
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CCOSSStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(param.SuccessStatus ? "1" : "0");    // 成功状态
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }
            /// <summary>
            /// 通信回执申请，发送回执
            /// $CACA
            /// </summary>
            public static String GetCACAStr(CCACAStruct param)
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CCACAStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(param.SenderID);       // 发信方ID
                rawStr.Append(",");
                rawStr.Append(param.RecvType);       // 收信方类型
                rawStr.Append(",");
                rawStr.Append(param.RecvAddr);       // 收信方地址
                rawStr.Append(",");
                rawStr.Append(param.Requirements);   // 保密要求
                rawStr.Append(",");
                rawStr.Append(param.ReceiptMsgSequenceNum);  // 回执的报文顺序号
                rawStr.Append(",");
                rawStr.Append(param.ReceiptContent); // 回执内容
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();
            }

            public static String GetTAPPStr()
            {
                StringBuilder rawStr = new StringBuilder();
                rawStr.Append("$");
                rawStr.Append(CTAPPStruct.CMD_PREFIX);
                rawStr.Append(",");
                rawStr.Append(GenerateCRC(rawStr.ToString()));// 校验和
                rawStr.Append("\r\n");
                return rawStr.ToString();

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
