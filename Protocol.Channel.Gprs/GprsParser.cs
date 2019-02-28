using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Hydrology.Entity;
using Protocol.Channel.Interface;
using Protocol.Data.Interface;

namespace Protocol.Channel.Gprs
{
    public class GprsParser : IGprs
    {
        #region 成员变量
        private ushort currentPort;
        private Semaphore m_semaphoreData;    //用来唤醒消费者处理缓存数据
        private List<ModemDataStruct> m_listDatas;   //存放data的内存缓存
        private Mutex m_mutexListDatas;     // 内存data缓存的互斥量

        private Thread m_threadDealData;    // 处理数据线程
        private System.Timers.Timer m_timer = new System.Timers.Timer()
        {
            Enabled = true,
            Interval = 5000
        };
        private System.Timers.Timer m_timerT = new System.Timers.Timer()
        {
            Enabled = true,
            Interval = 5 * 60 * 1000
        };
        private int GetReceiveTimeOut()
        {
            //      return (int)(m_timer.Interval / 1000);
            return (int)(m_timer.Interval);
        }
        #endregion 成员变量

        #region .ctor
        public GprsParser()
        {

            // 初始化成员变量
            m_semaphoreData = new Semaphore(0, Int32.MaxValue);
            m_listDatas = new List<ModemDataStruct>();
            m_mutexListDatas = new Mutex();

            m_threadDealData = new Thread(new ThreadStart(this.DealData));
            m_threadDealData.Start();

            DTUList = new List<ModemInfoStruct>();

            m_timer.Elapsed += new ElapsedEventHandler(m_timer_Elapsed);
            //m_timerT.Elapsed += new ElapsedEventHandler(m_timer_ElapsedT);
        }

        void m_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            int second = GetReceiveTimeOut();
            InvokeMessage(String.Format("系统接收数据时间超过{0}毫秒", second), "系统超时");
            if (this.ErrorReceived != null)
                this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs()
                {
                    Msg = String.Format("系统接收数据时间超过{0}秒", second)
                });
            if (null != this.GPRSTimeOut)
            {
                this.GPRSTimeOut(null, new ReceivedTimeOutEventArgs() { Second = second });
            }
            Debug.WriteLine("系统超时,停止计时器");
            m_timer.Stop();
        }
        void m_timer_ElapsedT(object sender, ElapsedEventArgs e)
        {
            int num = DTUdll.Instance.getOnlionCount();
            InvokeMessage(num.ToString(), "在线DTU数");

        }
        #endregion

        #region 接口方法
        /// <summary>
        /// 开启服务
        /// </summary>
        public bool DSStartService(ushort port)
        {
            //InvokeMessage(String.Format("开启端口{0}...", port), "初始化");
            currentPort = port;
            bool started = DTUdll.Instance.StartService(port);
            tmrData.Start();
            tmrDTU.Start();

            if (SerialPortStateChanged != null)
                SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                {
                    PortType = this.m_portType,
                    PortNumber = port,
                    BNormal = started
                }));
            InvokeMessage(String.Format("开启端口{0}   {1}!", port, started ? "成功" : "失败"), "初始化");
            return started;


        }
        public bool addPort(ushort port)
        {
            currentPort = port;
            bool flag = false;
            try
            {
                flag = DTUdll.Instance.addPort(port);
                if (flag)
                {
                    InvokeMessage(String.Format("开启端口成功...", port), "初始化");
                }
            }
            catch
            {
                return flag;
            }
            return flag;
        }
        /// <summary>
        /// 停止服务
        /// </summary>
        public bool DSStopService()
        {
            bool stoped = false;
            tmrData.Stop();
            tmrDTU.Stop();
            int port = DTUdll.Instance.ListenPort;
            //InvokeMessage(String.Format("关闭端口{0}...", port), "      ");
            if (DTUdll.Instance.Started)
                stoped = DTUdll.Instance.StopService();

            if (SerialPortStateChanged != null)
                SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                {
                    PortType = this.m_portType,
                    PortNumber = port,
                    BNormal = stoped
                }));
            InvokeMessage(String.Format("关闭端口{0}   {1}!", port, stoped ? "成功" : "失败"), "      ");
            return stoped;
        }
        /// <summary>
        /// 获取服务是否启动
        /// </summary>
        public bool GetStarted()
        {
            return DTUdll.Instance.Started;
        }
        /// <summary>
        /// 获取最新错误信息
        /// </summary>
        public string GetLastError()
        {
            return DTUdll.Instance.LastError;
        }
        /// <summary>
        /// 获取监听端口
        /// </summary>
        public ushort GetListenPort()
        {
            return DTUdll.Instance.ListenPort;
        }

        public ushort GetCurrentPort()
        {
            return currentPort;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        public bool SendHex(uint id, byte[] bts)
        {
            string msg = System.Text.Encoding.Default.GetString(bts);
            //Debug.WriteLine("GPRS发送数据:" + msg);
            InvokeMessage(msg + " " + id.ToString("X").PadLeft(8, '0'), "发送");
            return DTUdll.Instance.SendHex(id, bts);
            // return DTUdll.Instance.SendText(id, msg);
        }

        public bool SendData(uint id, string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return false;
            }
            //      Debug.WriteLine("GPRS发送数据:" + msg);
            InvokeMessage(msg, "发送");
            //      Debug.WriteLine("先停止计时器，然后在启动计时器");
            //  先停止计时器，然后在启动计时器
            m_timer.Stop();
            m_timer.Start();
            return DTUdll.Instance.SendHex(id, System.Text.Encoding.Default.GetBytes(msg));
        }

        static bool s_isFirstSend = true;
        static bool s_isFirstSend_1 = true;
        internal class MyMessage
        {
            public uint ID;
            public string MSG;
        }

        internal class MyMessage_1
        {
            public uint ID;
            public byte[] MSG;
        }

        public void SendDataTwiceForBatchTrans(uint id, string msg)
        {
            m_timer.Interval = 60000;
            SendData(id, msg);
            if (s_isFirstSend)
            {
                MyMessage myMsg = new MyMessage() { ID = id, MSG = msg };
                s_isFirstSend = false;
                Thread t = new Thread(new ParameterizedThreadStart(ResendRead))
                {
                    Name = "重新发送读取线程",
                    IsBackground = true
                };
                t.Start(myMsg);
            }
        }

        public void SendDataTwice(uint id, string msg)
        {
            m_timer.Interval = 600;
            SendData(id, msg);
            if (s_isFirstSend)
            {
                MyMessage myMsg = new MyMessage() { ID = id, MSG = msg };
                s_isFirstSend = false;
                Thread t = new Thread(new ParameterizedThreadStart(ResendRead))
                {
                    Name = "重新发送读取线程",
                    IsBackground = true
                };
                t.Start(myMsg);
            }
        }

        public bool SendTru(uint dtuID)
        {
            try
            {

                byte[] bts = new byte[] { 84, 82, 85, 13, 10 };
                SendHex(dtuID, bts);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool SendHexTwice(uint id, byte[] bts)
        {
            m_timer.Interval = 5000;
            SendHex(id, bts);
            if (s_isFirstSend_1)
            {
                MyMessage_1 myMsg = new MyMessage_1() { ID = id, MSG = bts };
                s_isFirstSend = false;
                Thread t = new Thread(new ParameterizedThreadStart(ResendRead_1))
                {
                    Name = "重新发送读取线程",
                    IsBackground = true
                };
                t.Start(myMsg);
            }
            //string msg = System.Text.Encoding.Default.GetString(bts);
            //Debug.WriteLine("GPRS发送数据:" + msg);
            //InvokeMessage(msg, "发送");
            return DTUdll.Instance.SendHex(id, bts);
        }
        private void ResendRead(object obj)
        {
            Debug.WriteLine(System.Threading.Thread.CurrentThread.Name + "休息1秒!");
            System.Threading.Thread.Sleep(1000);
            try
            {
                MyMessage myMsg = obj as MyMessage;
                if (null != myMsg)
                {
                    SendData(myMsg.ID, myMsg.MSG);
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
            finally { s_isFirstSend = true; }
        }

        private void ResendRead_1(object obj)
        {
            Debug.WriteLine(System.Threading.Thread.CurrentThread.Name + "休息1秒!");
            System.Threading.Thread.Sleep(1000);
            try
            {
                MyMessage_1 myMsg = obj as MyMessage_1;
                if (null != myMsg)
                {
                    SendHex(myMsg.ID, myMsg.MSG);
                }
            }
            catch (Exception exp) { Debug.WriteLine(exp.Message); }
            finally { s_isFirstSend = true; }
        }

        /// <summary>
        /// 初始化方法
        /// </summary>
        public void Init()
        {
            this.m_channelType = EChannelType.GPRS;
            this.m_portType = EListeningProtType.Port;

            if (tmrData == null)
                tmrData = new System.Timers.Timer(250);
            tmrData.Elapsed += new ElapsedEventHandler(tmrData_Elapsed);

            if (tmrDTU == null)
                tmrDTU = new System.Timers.Timer(2000);
            tmrDTU.Elapsed += new ElapsedEventHandler(tmrDTU_Elapsed);

            if (DTUList == null)
                DTUList = new List<ModemInfoStruct>();
        }
        /// <summary>
        /// 关闭服务
        /// </summary>
        public void Close()
        {
            // 停止监听
            this.DSStopService();
            try
            {
                m_threadDealData.Abort(); //终止处理数据线程
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        public void InitInterface(IUp up, IDown down, IUBatch udisk, IFlashBatch flash, ISoil soil)
        {
            this.Up = up;
            this.Down = down;
            this.UBatch = udisk;
            this.FlashBatch = flash;
            this.Soil = soil;
        }
        public void InitStations(List<CEntityStation> stations)
        {
            this.m_stationLists = stations;
        }
        public bool FindByID(string userID, out uint dtuID)
        {
            dtuID = 0;
            List<ModemInfoStruct> DTUList_1 = DTUList;
            //foreach (var item in DTUList_1)
            for (int i = 0; i < DTUList_1.Count; i++)
            {
                ModemInfoStruct item = DTUList_1[i];
                if (item.m_modemId.ToString("X").PadLeft(8, '0') == userID)
                {
                    dtuID = item.m_modemId;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 私有方法
        // 获取DTU列表
        private bool GetDTUList(out Dictionary<uint, ModemInfoStruct> dtuList)
        {
            return DTUdll.Instance.GetDTUList(out dtuList);
        }
        // 获取下一条指令或者数据
        private bool GetNextData(out ModemDataStruct dat)
        {
            return DTUdll.Instance.GetNextData(out dat);
        }
        // 发送数据
        //private bool SendData(uint id, string text)
        //{
        //    return DTUdll.Instance.SendText(id, text);
        //}
        // 发送文件
        private bool SendFile(uint id, byte[] fileBytes)
        {
            try
            {
                /*  每次发送1450个字节 */
                ushort length = 1450;

                if (fileBytes == null || fileBytes.Length == 0)
                    return false;
                int flen = fileBytes.Length;
                int startIndex = 0;

                while (startIndex < flen)
                {
                    if (!DTUdll.Instance.SendHex(id, fileBytes, startIndex, length))
                        return false;
                    startIndex += length;
                }
                return true;
            }
            catch (System.Exception ee)
            {
                Debug.WriteLine(ee.Message);
                return false;
            }
        }
        // 发送控制命令
        private bool SendControl(uint id, string text)
        {
            return DTUdll.Instance.SendControl(id, text);
        }
        //  发送数据
        private bool SendHex(uint id, byte[] bts, int startIndex, ushort length)
        {
            return DTUdll.Instance.SendHex(id, bts, startIndex, length);
        }
        private bool SendHex(uint id, byte[] bts, ushort length)
        {
            return DTUdll.Instance.SendHex(id, bts, length);
        }

        private bool ParseData(string msg, string gprs)
        {
            //           InvokeMessage("协议。。。 ", "进入函数7");   
            try
            {
                string rawData = msg;

                if (msg.Contains("$"))
                {
                    string data = string.Empty;
                    if (!ProtocolHelpers.DeleteSpecialChar(msg, out data))
                        return false;
                    msg = data;

                    string sid = msg.Substring(0, 4);
                    string type = msg.Substring(4, 2);

                    #region 1G
                    if (type == "1G")
                    {
                        string reportType = msg.Substring(6, 2);
                        if (reportType == "21" || reportType == "22")   //   定时报，加报
                        {
                            //  YAC设备的墒情协议：
                            string stationType = msg.Substring(8, 2);
                            switch (stationType)
                            {
                                //  站类为04时墒情站 05墒情雨量站 06，16墒情水位站 07，17墒情水文站
                                case "04":
                                case "05":
                                case "06":
                                case "07":
                                case "17":
                                    {
                                        //CEntitySoilData soilStruct = new CEntitySoilData();
                                        //if (Soil.Parse(msg, out soilStruct))
                                        //{
                                        //    soilStruct.ChannelType = EChannelType.GPRS;
                                        //    //soilStruct.ListenPort = this.GetListenPort().ToString();

                                        //    //string temp = soilStruct.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                        //    //InvokeMessage(String.Format("{0,-10}   ", temp) + rawData, "接收");

                                        //    //  抛出YAC设备墒情事件
                                        //    if (null != this.SoilDataReceived)
                                        //        this.SoilDataReceived.Invoke(null, new YACSoilEventArg()
                                        //        {
                                        //            RawData = rawData,
                                        //            Value = soilStruct
                                        //        });
                                        //}

                                        CEntitySoilData soil = new CEntitySoilData();
                                        CReportStruct soilReport = new CReportStruct();
                                        if (Soil.Parse(rawData, out soil, out soilReport))
                                        {
                                            soil.ChannelType = EChannelType.GPRS;

                                            if (null != this.SoilDataReceived)
                                                this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));
                                            //1111gm
                                            string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            InvokeMessage("  gprs号码:  " + gprs + String.Format("  {0,-10}   ", temp) + rawData, "接收");

                                            if (null != soilReport && null != this.UpDataReceived)
                                            {
                                                soilReport.ChannelType = EChannelType.GPRS;
                                                soilReport.ListenPort = this.GetListenPort().ToString();
                                                soilReport.flagId = gprs;
                                                this.UpDataReceived(null, new UpEventArgs() { RawData = rawData, Value = soilReport });
                                            }
                                        }
                                        else
                                        {
                                            //string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            InvokeMessage("  gprs号码:  " + gprs + "  " + rawData, "接收");
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
                                        CReportStruct report = new CReportStruct();
                                        if (Up.Parse(msg, out report))
                                        {
                                            report.ChannelType = EChannelType.GPRS;
                                            report.ListenPort = this.GetListenPort().ToString();
                                            report.flagId = gprs;

                                            string temp = report.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            InvokeMessage("  gprs号码:  " + gprs + String.Format("  {0,-10}   ", temp) + rawData, "接收");
                                            
                                            if (this.UpDataReceived != null)
                                                this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = rawData });
                                            //   InvokeMessage("12333", "接收");
                                        }
                                        else
                                        {
                                            //string temp = report.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            InvokeMessage("  gprs号码:  " + gprs + "  可疑数据 " + rawData, "接收");
                                        }
                                    }
                                    break;
                                case "11":
                                    {
                                        CReportStruct report = new CReportStruct();
                                        //CReportArtificalWater report = new CReportArtificalWater();
                                        if (Up.Parse_1(msg, out report))
                                        {
                                            report.ChannelType = EChannelType.GPRS;
                                            report.ListenPort = this.GetListenPort().ToString();
                                            report.ReportType = EMessageType.Batch;
                                            string temptype = "人工水位";
                                            InvokeMessage(String.Format("{0,-10}   ", temptype) + rawData, "接收");
                                            //if (this.UpDataReceived != null)
                                            this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = rawData });
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (reportType == "23")    //  人工流量
                        {
                            CReportStruct report = new CReportStruct();
                            if (Up.Parse_2(msg, out report))
                            {
                                report.ChannelType = EChannelType.GPRS;
                                report.ListenPort = this.GetListenPort().ToString();
                                report.ReportType = EMessageType.Batch;
                                string temptype = "人工流量";
                                InvokeMessage(String.Format("{0,-10}   ", temptype) + rawData, "接收");
                                this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = rawData });
                            }
                        }
                        else if (reportType == "32")    //  人工报送水位
                        {
                            CReportStruct report = new CReportStruct();
                            report.ChannelType = EChannelType.GPRS;
                            report.ListenPort = this.GetListenPort().ToString();
                            string temptype = "人工报送水位";
                            InvokeMessage(String.Format("{0,-10}   ", temptype) + rawData, "接收");
                            WriteToFileClass writeClass = new WriteToFileClass("RGwater");
                            //Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                            //t.Start("GPRS： " + "长度：" + data.Length + " " + rawData + "\r\n");
                            string a1 = rawData.Substring(9, 1);
                            string a2 = rawData.Substring(10, 1);
                            if (a1 == "P" || a1 == "H" || a1 == "K" || a1 == "Z" || a1 == "D" || a1 == "T" || a1 == "M" || a1 == "G" || a1 == "Y" || a1 == "F" || a1 == "R")
                            {
                                if (a2 == "A")
                                {
                                    if (rawData.Contains("ST"))
                                    {
                                        WriteToFileClass writeClass1 = new WriteToFileClass("sharewater");
                                        Thread t1 = new Thread(new ParameterizedThreadStart(writeClass1.WriteInfoToFile));
                                        t1.Start("GPRS： " + "长度：" + data.Length + " " + rawData + "\r\n");
                                    }
                                }
                            }
                        }
                        else if (reportType == "53")    // 人工报送时段雨量 日雨量 旬雨量 水库水位 蓄水量 入库流量 出库流量
                        {
                            CReportStruct report = new CReportStruct();
                            report.ChannelType = EChannelType.GPRS;
                            report.ListenPort = this.GetListenPort().ToString();
                            string temptype = "人工报送雨量";
                            InvokeMessage(String.Format("{0,-10}   ", temptype) + rawData, "接收");
                            WriteToFileClass writeClass = new WriteToFileClass("RGRain");
                            Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                            t.Start("GPRS： " + "长度：" + data.Length + " " + rawData + "\r\n");
                        }
                        else if (reportType == "25")
                        {
                            //CEntitySoilData readSoilStruct = new CEntitySoilData();
                            //if (Soil.Parse(msg, out readSoilStruct))
                            //{
                            //    readSoilStruct.ChannelType = EChannelType.GPRS;
                            //    //readSoilStruct.ListenPort = this.GetListenPort().ToString();

                            //    //string temp = readSoilStruct.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                            //    //InvokeMessage(String.Format("{0,-10}   ", temp) + rawData, "接收");
                            //    //  抛出读墒情事件
                            //    if (null != this.SoilDataReceived)
                            //        this.SoilDataReceived.Invoke(null, new YACSoilEventArg()
                            //        {
                            //            RawData = rawData,
                            //            Value = readSoilStruct
                            //        });
                            //}

                            CEntitySoilData soil = new CEntitySoilData();
                            CReportStruct soilReport = new CReportStruct();
                            if (Soil.Parse(rawData, out soil, out soilReport))
                            {
                                soil.ChannelType = EChannelType.GPRS;

                                if (null != this.SoilDataReceived)
                                    this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                                if (null != soilReport && null != this.UpDataReceived)
                                {
                                    soilReport.ChannelType = EChannelType.GPRS;
                                    soilReport.ListenPort = this.GetListenPort().ToString();
                                    soilReport.flagId = gprs;
                                    this.UpDataReceived(null, new UpEventArgs() { RawData = rawData, Value = soilReport });
                                }
                            }
                        }
                        else //  下行指令
                        {
                            CDownConf downconf = new CDownConf();
                            if (Down.Parse(msg, out downconf))
                            {
                                InvokeMessage(String.Format("{0,-10}   ", "下行指令读取参数") + rawData, "接收");
                                if (this.DownDataReceived != null)
                                    this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = rawData });
                            }
                        }
                    }
                    #endregion

                    #region 1K
                    if (type == "1K")
                    {
                        var station = FindStationBySID(sid);
                        if (station == null)
                            throw new Exception("批量传输，站点匹配错误");
                        //EStationBatchType batchType = station.BatchTranType;

                        //if (batchType == EStationBatchType.EFlash)
                        //{
                        //    CBatchStruct batch = new CBatchStruct();
                        //    if (FlashBatch.Parse(msg, out batch))
                        //    {
                        //        InvokeMessage(String.Format("{0,-10}   ", "Flash批量传输") + rawData, "接收");

                        //        if (this.BatchDataReceived != null)
                        //            this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = rawData });
                        //    }
                        //}
                        //else if (batchType == EStationBatchType.EUPan)
                        //{
                        //    CBatchStruct batch = new CBatchStruct();
                        //    if (UBatch.Parse(msg, out batch))
                        //    {
                        //        InvokeMessage(String.Format("{0,-10}   ", "U盘批量传输") + rawData, "接收");

                        //        if (this.BatchDataReceived != null)
                        //            this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = rawData });
                        //    }
                        //}

                        CBatchStruct batch = new CBatchStruct();
                        if (FlashBatch.Parse(msg, out batch))
                        {
                            InvokeMessage(String.Format("{0,-10}   ", "批量传输") + rawData, "接收");

                            if (this.BatchDataReceived != null)
                                this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = rawData });
                        }

                    }
                    #endregion

                    #region 1S
                    if (type == "1S")
                    {
                        CDownConf downconf = new CDownConf();
                        if (Down.Parse(msg, out downconf))
                        {
                            InvokeMessage(String.Format("{0,-10}   ", "下行指令设置参数") + rawData, "接收");

                            if (this.DownDataReceived != null)
                                this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = rawData });
                        }
                    }
                    #endregion
                }
                else if (msg.Contains("#"))
                {
                    CEntitySoilData soil = new CEntitySoilData();
                    CReportStruct soilReport = new CReportStruct();
                    if (Soil.Parse(rawData, out soil, out soilReport))
                    {
                        soil.ChannelType = EChannelType.GPRS;

                        if (null != this.SoilDataReceived)
                            this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                        if (null != soilReport && null != this.UpDataReceived)
                        {
                            soilReport.ChannelType = EChannelType.GPRS;
                            soilReport.ListenPort = this.GetListenPort().ToString();
                            soilReport.flagId = gprs;
                            this.UpDataReceived(null, new UpEventArgs() { RawData = rawData, Value = soilReport });
                        }
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception exp)
            {
                //System.Diagnostics.Debug.WriteLine("GPRS 数据解析出错 ！" + msg + "\r\n" + exp.Message);
            }
            return false;
        }


        // 处理内存缓存中的数据
        private void DealData()
        {
            //InvokeMessage("协议。。。 ", "进入函数6");   
            while (true)
            {
                //阻塞0.05s，防止过快获取数据
                Thread.Sleep(50);
                m_semaphoreData.WaitOne(); //阻塞当前线程，知道被其它线程唤醒
                // 获取对data内存缓存的访问权
                m_mutexListDatas.WaitOne();
                List<ModemDataStruct> dataListTmp = m_listDatas;
                m_listDatas = new List<ModemDataStruct>(); //开辟一快新的缓存区
                m_mutexListDatas.ReleaseMutex();
                // 开始处理数据
                for (int i = 0; i < dataListTmp.Count; ++i)
                {
                    try
                    {
                        ModemDataStruct dat = dataListTmp[i];
                        string data = System.Text.Encoding.Default.GetString(dat.m_data_buf).Trim();
                        string gprsId = ((uint)dat.m_modemId).ToString("X").PadLeft(8, '0');
                        //1119
                        //WriteToFileClass writeClass = new WriteToFileClass("ReceivedLog");
                        //Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                        //string logStr = data.Replace("00000000000", "");
                        //t.Start("GPRS： " + "长度：" + logStr.Length + " " + logStr + "\r\n");

                        if (data.Contains("TRU"))
                        {
                            Debug.WriteLine("接收数据TRU完成,停止计时器");
                            //m_timer.Stop();
                            InvokeMessage("TRU " + ((uint)dat.m_modemId).ToString("X").PadLeft(8, '0'), "接收");
                            if (this.ErrorReceived != null)
                                this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs()
                                {
                                    //   Msg = "TRU " + dat.m_modemId
                                    Msg = "TRU " + gprsId
                                });
                        }
                        if (data.Contains("ATE0"))
                        {
                            Debug.WriteLine("接收数据ATE0完成,停止计时器");
                            //m_timer.Stop();
                            // InvokeMessage("ATE0", "接收");
                            if (this.ErrorReceived != null)
                                this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs()
                                {
                                    Msg = "ATE0"
                                });
                        }
                        if (data.Contains("$"))
                        {
                            if (data.Contains("1G21") || data.Contains("1G22") || data.Contains("1G23"))
                            {
                                // 发送回执
                                SendTru(dat.m_modemId);
                            }

                            if (data.Contains("$TS"))
                            {
                                Debug.WriteLine("接收数据正确完成,停止计时器");
                                int indexStart = data.IndexOf("$");

                                data = data.Substring(indexStart + 3);
                                int length = data.IndexOf(CSpecialChars.ENTER_CHAR);
                                data = data.Substring(0, length);
                                data = "$" + data;
                                data = data.Trim();
                                data = data + "TS";
                                if (this.ParseData(data,gprsId))
                                {
                                    if (this.ModemDataReceived != null)
                                        this.ModemDataReceived.Invoke(this, new ModemDataEventArgs()
                                        {
                                            Msg = data,
                                            Value = dat
                                        });
                                }
                            }
                            else
                            {
                                Debug.WriteLine("接收数据正确完成,停止计时器");
                                //m_timer.Stop();
                                int indexStart = data.IndexOf("$");
                                data = data.Substring(indexStart);
                                int length = data.IndexOf(CSpecialChars.ENTER_CHAR);
                                data = data.Substring(0, length);
                                data = data.Trim();
                                //this.ParseData(data,gprsId);
                                if (this.ParseData(data,gprsId))
                                {
                                    if (this.ModemDataReceived != null)
                                        this.ModemDataReceived.Invoke(this, new ModemDataEventArgs()
                                        {
                                            Msg = data,
                                            Value = dat
                                        });
                                }
                            }
                        }
                        if (data.Contains("#"))
                        {
                            Debug.WriteLine("接收数据正确完成,停止计时器");
                            //m_timer.Stop();
                            int indexStart = data.IndexOf("#");
                            data = data.Substring(indexStart);
                            int length = data.IndexOf(CSpecialChars.ENTER_CHAR);
                            data = data.Substring(0, length);
                            data = data.Trim();
                            //this.ParseData(data,gprsId);
                            if (this.ParseData(data,gprsId))
                            {
                                if (this.ModemDataReceived != null)
                                    this.ModemDataReceived.Invoke(this, new ModemDataEventArgs()
                                    {
                                        Msg = data,
                                        Value = dat
                                    });
                            }
                        }
                        m_timer.Stop();
                    }
                    catch (Exception exp) { Debug.WriteLine(exp.Message); }
                } //end of for
            }//end of while
        }

        private CEntityStation FindStationBySID(string sid)
        {
            if (this.m_stationLists == null)
                throw new Exception("GPRS模块未初始化站点！");

            CEntityStation result = null;
            foreach (var station in this.m_stationLists)
            {
                if (station.StationID.Equals(sid))
                {
                    result = station;
                    break;
                }
            }
            return result;
        }
        #endregion

        #region 属性
        public IUp Up { get; set; }
        public IDown Down { get; set; }
        public IUBatch UBatch { get; set; }
        public IFlashBatch FlashBatch { get; set; }
        public ISoil Soil { get; set; }

        public List<ModemInfoStruct> DTUList { get; set; }
        public Boolean IsCommonWorkNormal { get; set; }

        private List<CEntityStation> m_stationLists;
        private EChannelType m_channelType;
        private EListeningProtType m_portType;
        private System.Timers.Timer tmrData;
        private System.Timers.Timer tmrDTU;
        #endregion

        #region 日志记录
        /// <summary>
        /// 日志记录，用事件返回出去
        /// </summary>
        /// <param name="msg">接受，发送的数据</param>
        /// <param name="description">对数据的描述，一般为:接受，发送，初始化</param>
        public void InvokeMessage(string msg, string description)
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

        #region 数据维护
        private string LastData = string.Empty;
        private bool inDataTicks = false;
        private void tmrData_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (inDataTicks || inDtuTicks) return;
            inDataTicks = true;
            try
            {
                //读取数据
                ModemDataStruct dat = new ModemDataStruct();
                while (this.GetNextData(out dat))
                {
                    if (!this.GetStarted())
                        return;
                    //byte[] bts = new byte[] { 84, 82, 85, 13, 10 };
                    String str = System.Text.Encoding.Default.GetString(dat.m_data_buf);
                    //if (str.Contains("1G21") || str.Contains("1G22"))
                    //{
                    //    InvokeMessage("TRU,modemId: " + dat.m_modemId, "发送");
                    //    InvokeMessage("bts: " + bts, "发送");
                    //    InvokeMessage("bts.length: " + (ushort)bts.Length, "发送");
                    //    // 发送回执
                    //    DTUdll.Instance.SendHex(dat.m_modemId, bts, (ushort)bts.Length);
                    //}
                    // //发送回执
                    //DTUdll.Instance.SendHex(dat.m_modemId, bts, (ushort)bts.Length);     
                    m_mutexListDatas.WaitOne();
                    String a = System.Text.Encoding.Default.GetString(dat.m_data_buf);
                    // Debug.WriteLine("协议接收数据: " + System.Text.Encoding.Default.GetString(dat.m_data_buf));
                    m_listDatas.Add(dat);
                    m_semaphoreData.Release(1);
                    m_mutexListDatas.ReleaseMutex();
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine("读取数据", ee.Message);
            }
            finally
            {
                inDataTicks = false;
            }
        }
        #endregion

        #region 用户列表维护
        private bool inDtuTicks = false;
        private void tmrDTU_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (inDtuTicks)
                {
                    Manager.WriteToDebugFlie wt = new Manager.WriteToDebugFlie();
                    wt.WriteToDebugFile(new Manager.WriteToDebugFlie.DebugInfo() { Time = DateTime.Now, Info = "定时器已关闭 : " + this.GetLastError() });
                    return;
                }
                inDtuTicks = true;
                if (!this.GetStarted())
                {
                    Manager.WriteToDebugFlie wt = new Manager.WriteToDebugFlie();
                    wt.WriteToDebugFile(new Manager.WriteToDebugFlie.DebugInfo() { Time = DateTime.Now, Info = "gprs服务已关闭 : " + this.GetLastError() });
                    return;
                }

                Dictionary<uint, ModemInfoStruct> dtuList;
                if (this.GetDTUList(out dtuList))
                {
                    this.DTUList.Clear();
                    foreach (var item in dtuList)
                    {
                        this.DTUList.Add(item.Value);
                    }

                    if (this.ModemInfoDataReceived != null)
                        this.ModemInfoDataReceived(this, null);
                }
                else
                {
                    Debug.WriteLine("读取DTU列表错误 : " + this.GetLastError());
                    Manager.WriteToDebugFlie wt = new Manager.WriteToDebugFlie();
                    wt.WriteToDebugFile(new Manager.WriteToDebugFlie.DebugInfo() { Time = DateTime.Now, Info = "读取DTU列表错误 : " + this.GetLastError() });
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine("Error in DtuTicks : " + exp.Message);
                Manager.WriteToDebugFlie wt = new Manager.WriteToDebugFlie();
                wt.WriteToDebugFile(new Manager.WriteToDebugFlie.DebugInfo() { Time = DateTime.Now, Info = "Error in DtuTicks : " + exp.Message });
            }
            finally
            {
                inDtuTicks = false;
            }
        }
        #endregion

        #region 事件定义
        public event EventHandler<UpEventArgs> UpDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;
        public event EventHandler<ReceivedTimeOutEventArgs> GPRSTimeOut;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;

        public event EventHandler<ModemDataEventArgs> ModemDataReceived;
        public event EventHandler ModemInfoDataReceived;
        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        #endregion
    }
}
