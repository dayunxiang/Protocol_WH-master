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
using Protocol.Manager;

namespace Protocol.Channel.Gsm
{
    public class GsmParser : IGsm
    {
        public static int num = 0;
        public static int num1 = 0;
        #region 初始化函数
        public GsmParser()
        {
            this.m_inputBuffer = new List<byte>();
            this.m_channelType = EChannelType.GSM;
            this.m_portType = EListeningProtType.SerialPort;

            this.m_stationLists = new List<CEntityStation>();

            IsCommonWorkNormal = false;
            
                 
            //this.Parser_3("::+8613212710080:::::17/02/26:15:00:00:::$50221G2204170226090000000000001200051902060903");
        }

        public void InitPort(string comPort, int baudRate)
        {
            //InvokeMessage(String.Format("开始初始化串口{0}...", comPort), "初始化");
            ListenPort = new SerialPort()
            {
                PortName = comPort,
                BaudRate = baudRate,
                //ReadTimeout = 3000,             //读超时时间 发送短信时间的需要
                RtsEnable = true                //必须为true 这样串口才能接收到数据
                //NewLine = "/r/n"
            };
            //InvokeMessage("......", "初始化");
            ListenPort.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);
            //InvokeMessage(String.Format("完成初始化串口{0}.", comPort), "初始化");
            //Debug.WriteLine("串口初始化完成");
        }
        public bool InitGsm()
        {
            try
            {
                bool isSendSuccess = false;
                bool isClearSuccess = true;
                //int k = 1;
                for (int k = 1; k <= 40; k++)
                {
                    SendCmpd(out isClearSuccess, k);
                    //k++;
                }
                //int k = 1;
                //bool isClearSuccess = true;
                //while (isClearSuccess)
                //{
                //    for (int k = 1; k <= 40; k++)
                //    {
                //        SendCmpd(out isClearSuccess, k);
                //        k++;
                //    }
                //}
                string sendat = SendAT(out isSendSuccess);

                int i = 0;
                bool isOk = false;
                while (i != 4)
                {
                    if (sendat.Contains("OK"))
                    {
                        isOk = true;
                        break;
                    }
                    else
                    {
                        sendat = SendAT(out isSendSuccess);
                    }
                    i++;
                }
                if (!isOk)
                {
                    InvokeMessage(String.Format("初始化串口{0}失败", ListenPort.PortName), "初始化");
                    return false;
                }


                SendSetCSMP(out isSendSuccess);
                if (!isSendSuccess) return false;
                SendQueryCSMP(out isSendSuccess);
                if (!isSendSuccess) return false;
                SendSetCNMI(out isSendSuccess);
                if (!isSendSuccess) return false;
                SendQueryCNMI(out isSendSuccess);
                if (!isSendSuccess) return false;
                SendSave(out isSendSuccess);
                if (!isSendSuccess) return false;

                //Debug.WriteLine("GSM参数初始化完成");
                InvokeMessage(String.Format("初始化串口{0}完成", ListenPort.PortName), "初始化");
                return true;
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
            return false;
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
            this.m_stationLists = stations;
        }
        #endregion

        #region 常用GSM配置信息
        private string SendAT(out bool isSendSuccess)
        {
            return Send(GsmHelper.AT, out isSendSuccess);
        }
        private string SendQueryCSMP(out bool isSendSuccess)
        {
            return Send(GsmHelper.AT_CSMP_QUERY, out isSendSuccess);
        }
        private string SendQueryCNMI(out bool isSendSuccess)
        {
            return Send(GsmHelper.AT_CNMI_QUERY, out isSendSuccess);
        }
        private string SendSetCSMP(out bool isSendSuccess)
        {
            return Send(GsmHelper.AT_CSMP_SET, out isSendSuccess);
        }
        private string SendSetCNMI(out bool isSendSuccess)
        {
            return Send(GsmHelper.AT_CNMI_SET, out isSendSuccess);
        }
        private string SendSave(out bool isSendSuccess)
        {
            // return Send(GsmHelper.AT_SAVE + (char)(26), out isSendSuccess);
            return Send("at&w", out isSendSuccess);
        }
        private string SendCmpr(out bool isSendSuccess, int i)
        {
            return Send(GsmHelper.AT_CMPR + i, out isSendSuccess);
        }
        private string SendCmpd(out bool isSendSuccess, int i)
        {
            return Send(GsmHelper.AT_CMPD + i, out isSendSuccess);
        }

        private string Send(string msg, out bool isSendSuccess)
        {
            //InvokeMessage(msg, "发送");
            var result = SendMsg(msg, out isSendSuccess);
            //InvokeMessage(result, "接收");


            if (SerialPortStateChanged != null)
                SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                {
                    PortNumber = Int32.Parse(ListenPort.PortName.Replace("COM", "")),
                    BNormal = isSendSuccess,
                    PortType = m_portType
                }));
            return result;
        }
        #endregion

        #region 属性
        private List<byte> m_inputBuffer;
        private EChannelType m_channelType;
        private EListeningProtType m_portType;
        private List<CEntityStation> m_stationLists;

        public IUp Up { get; set; }
        public IDown Down { get; set; }
        public IUBatch UBatch { get; set; }
        public IFlashBatch FlashBatch { get; set; }
        public ISoil Soil { get; set; }
        public string LastError { get; set; }
        public SerialPort ListenPort { get; set; }
        public Boolean IsCommonWorkNormal { get; set; }
        #endregion

        public bool OpenPort()
        {
            try
            {
                ListenPort.Open();
                Debug.WriteLine("串口状态:" + (ListenPort.IsOpen ? "Open" : "Closed"));
                InvokeMessage(String.Format("开启串口{0}成功", ListenPort.PortName), "初始化");
                return true;
            }
            catch (Exception exp)
            {
                //InvokeMessage("串口" + ListenPort.PortName + "打开失败!", "初始化");
                InvokeMessage(String.Format("开启串口{0}失败", ListenPort.PortName), "初始化");
                Debug.WriteLine("[GSM] Model " + exp.Message);
            }
            //if (SerialPortStateChanged != null)
            //    SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
            //    {
            //        PortNumber = Int32.Parse(ListenPort.PortName.Replace("COM", "")),
            //        BNormal = ListenPort.IsOpen,
            //        PortType = m_portType
            //    }));
            return false;

        }
        public void ClosePort()
        {
            try
            {
                ListenPort.Close();
                Debug.WriteLine("串口状态:" + (ListenPort.IsOpen ? "Open" : "Closed"));
            }
            catch (Exception exp)
            {
                Debug.WriteLine("[GSM] Model " + exp.Message);
            }
            //if (SerialPortStateChanged != null)
            //    SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
            //     {
            //         PortNumber = Int32.Parse(ListenPort.PortName.Replace("COM", "")),
            //         BNormal = ListenPort.IsOpen,
            //         PortType = m_portType
            //     }));
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
          //  int n = ListenPort.BytesToRead;
          //  byte[] buf = new byte[n];
          //  ListenPort.Read(buf, 0, n);

          //  m_inputBuffer.AddRange(buf);
          //  var count = (from r in m_inputBuffer where (r == 13) select r).Count();
          //  //while (count < 3)
          //  //{
          //  //    m_inputBuffer.AddRange(buf);
          //  //}

          //  Debug.WriteLine(count + " ----- " + Encoding.ASCII.GetString(buf));
          //  InvokeMessage(Encoding.ASCII.GetString(buf), "接收");
          // // if(m_inputBuffer.FindIndex)
          ////  InvokeMessage(count + " ----- " + Encoding.ASCII.GetString(buf), "接收");
          //  //if (count == 3)
          //  //{
          //   //   string data = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>());
          //      string data = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>()).Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
          //      Debug.WriteLine("data...",data);
          //      string[] arrstr = data.Split('\"');
          //      String strTime="";
          //      String strPhone="";
          //      String data_1 = "";
          //      if (arrstr.Count() >= 4)
          //      {
          //          for (int i = 0; i < arrstr.Count(); i++)
          //          {
          //              //电话号码
          //              if (arrstr[i].Contains("+86"))
          //              {
          //                  //Thread t = new Thread(new ParameterizedThreadStart(Parser));
          //                  //t.Start(arrstr[i]);

          //                  // Parser(arrstr[i]);
          //                  strTime = arrstr[i];
          //                  Debug.WriteLine(arrstr[i]);
          //                  continue;
          //              }
          //              if (arrstr[i].Contains("+32"))
          //              {
          //                  //Thread t = new Thread(new ParameterizedThreadStart(Parser));
          //                  //t.Start(arrstr[i]);
          //                  //Parser(arrstr[i]);
          //                  strPhone = arrstr[i];
          //                  Debug.WriteLine(arrstr[i]);
          //                  continue;
          //              }
          //              if (arrstr[i].Contains("1G"))
          //              {
          //                  //Thread t = new Thread(new ParameterizedThreadStart(Parser));
          //                  //t.Start(arrstr[i]);
          //                  //Parser(arrstr[i]);
          //                  data_1 = arrstr[i];
          //                  Debug.WriteLine(arrstr[i]);
          //                  continue;
          //              }
          //              if (strTime != "" && strPhone != "" && data_1 != "")
          //              {
          //                  Object a = strTime + "," + strPhone + "," + data_1;
          //                  Thread t = new Thread(new ParameterizedThreadStart(Parser));
          //                  t.Start(a);
          //                  strTime = "";
          //                  strPhone = "";
          //                  data_1 = "";
          //                  m_inputBuffer.Clear();
          //                  continue;
          //              }
          //          }
          //      }
               
          //      return;
          //     // data_1 = data.Trim();
             
          //  //}

            int n = ListenPort.BytesToRead;
            byte[] buf = new byte[n];
            ListenPort.Read(buf, 0, n);
            m_inputBuffer.AddRange(buf);
            var count = (from r in m_inputBuffer where (r == 13) select r).Count();
            Debug.WriteLine(count + " ----- " + Encoding.ASCII.GetString(buf));
            string flag = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>());
            //if (flag.Contains("$") && flag.Contains("CMT"))
            //{
            //    num = num + 1;
            //    WriteToFileNum writeClass1 = new WriteToFileNum("gsmnum");
            //    Thread t_gsm1 = new Thread(new ParameterizedThreadStart(writeClass1.WriteInfoToFile));
            //    t_gsm1.Start(num);
            //}
            //1119
            WriteToFileClass writeClass = new WriteToFileClass("ReceivedLog");
            Thread t_gsm = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
            t_gsm.Start("GSM: " + "长度:" + flag.Length + " " +  flag + "\r\n");


            if (flag.Contains("1G22") || flag.Contains("1G21"))
            {
                //if (count == 3)
                //{
                    string data = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>());
                  //  data=data.Substring(data.IndexOf("OK"));
                    if (data.Contains("CMT"))
                    {
                        if (data.EndsWith("\r\n"))
                        {
                            string[] a = new string[] { "CMT" };
                            string[] ArrData = data.Split(a, StringSplitOptions.None);
                            for (int i = 1; i < ArrData.Count(); i++)
                            {
                                Thread t = new Thread(new ParameterizedThreadStart(Parser_3));
                                t.Start(ArrData[i]);
                                //  Parser_2(ArrData[i]);
                                Debug.WriteLine(ArrData[i]);
                                m_inputBuffer.Clear();
                            }
                        }
                    }
                //}
            }
            else
            {
              //  string data = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>()).Replace("\n", "").Replace("\t", "").Replace("\r", "");
                string data = Encoding.ASCII.GetString(m_inputBuffer.ToArray<byte>());    
                if (data.Contains("CMT") && data.Contains("1G"))
                {
                    string data_1 = data.Substring(data.IndexOf("1G"));
                    if (data_1.EndsWith("\r\n"))
                    {
                        string[] a = new string[] { "CMT" };
                        string[] ArrData = data.Split(a, StringSplitOptions.None);
                        for (int i = 1; i < ArrData.Count(); i++)
                        {
                            Thread t = new Thread(new ParameterizedThreadStart(Parser_2));
                            t.Start(ArrData[i]);
                            //  Parser_2(ArrData[i]);
                            Debug.WriteLine(ArrData[i]);
                            m_inputBuffer.Clear();
                        }
                    }
                }
                else if (data.Contains("CMT") && data.Contains("TRU"))
                {
                    string data_1 = data.Substring(data.IndexOf("TRU"));
                    if (data_1.EndsWith("\r\n"))
                    {
                        string[] a = new string[] { "CMT" };
                        string[] ArrData = data.Split(a, StringSplitOptions.None);
                        for (int i = 1; i < ArrData.Count(); i++)
                        {
                            Thread t = new Thread(new ParameterizedThreadStart(Parser_2));
                            t.Start(ArrData[i]);
                            //  Parser_2(ArrData[i]);
                            Debug.WriteLine(ArrData[i]);
                            m_inputBuffer.Clear();
                        }
                    }
                }
                else if (data.Contains("CMT") && data.Contains("\r\n"))
                {
                    if (data.EndsWith("\r\n"))
                    {
                        InvokeMessage(data, "接收");
                        m_inputBuffer.Clear();
                    }
                }
            }

        }
        private void Parser_1(Object str)
        {
            try
            {
                string data = str as string;
                string[] arrstr = data.Split(',');
                /* 删除 '\r\n' 字符串 */
                //while (data.StartsWith("\r\n"))
                //{
                //    data = data.Substring(2);
                //}
                /*  
                 * 解析数据，获取CGSMStruct 
                 */
                var gsm = new CGSMStruct();
                //if (!GsmHelper.Parse(data, out gsm))
                //    return;
                if (!GsmHelper.ParseGsm(arrstr[0], arrstr[1], arrstr[2], out gsm))
                    return;
                /*  如果解析成功，触发GSM数据接收完成事件  */
                InvokeMessage(data, "接收");

                string msg = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(gsm.Message, out msg))
                    return;

                if (!msg.ToUpper().Contains("TRU"))
                {
                    string sid = msg.Substring(0, 4);
                    string type = msg.Substring(4, 2);

                    /* 
                     *  上行指令信息，
                     *  或者读取参数返回的信息 
                     */
                    #region 1G
                    if (type == "1G")
                    {
                        string reportType = msg.Substring(6, 2);
                        /*  定时报，加报 */
                        if (reportType == "21" || reportType == "22")
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
                                        if (Up.Parse(msg, out report)) /* 解析成功 */
                                        {
                                            report.ChannelType = EChannelType.GSM;
                                            report.ListenPort = "COM" + this.ListenPort.PortName;
                                            report.flagId = gsm.PhoneNumber;
                                            if (this.UpDataReceived != null)
                                                this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = data });
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
                        }
                        else /* 下行指令 */
                        {
                            CDownConf downconf = new CDownConf();
                            if (Down.Parse(msg, out downconf)) /* 解析成功 */
                            {
                                if (this.DownDataReceived != null)
                                    this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                            }
                        }
                    }

                    #endregion

                    #region 1K
                    if (type == "1K") /* 批量传输 */
                    {
                        var station = FindStationBySID(sid);
                        if (station == null)
                            throw new Exception("批量传输，站点传输类型匹配错误");
                      //  EStationBatchType batchType = station.BatchTranType;

                        //if (batchType == EStationBatchType.EFlash)          /* Flash传输 */
                        //{
                            CBatchStruct batch = new CBatchStruct();
                            if (FlashBatch.Parse(gsm.Message, out batch)) /* 解析成功 */
                            {
                                if (this.BatchDataReceived != null)
                                    this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                            }
                        //}
                        //else if (batchType == EStationBatchType.EUPan)  /* U盘传输 */
                        //{
                        //    CBatchStruct batch = new CBatchStruct();
                        //    if (UBatch.Parse(gsm.Message, out batch))   /* 解析成功 */
                        //    {
                        //        if (this.BatchDataReceived != null)
                        //            this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        //    }
                        //}
                        else
                            throw new Exception("批量传输，站点传输类型匹配错误");
                    }
                    #endregion

                    #region 1S
                    if (type == "1S") /* 远地下行指令，设置参数 */
                    {
                        CDownConf downconf = new CDownConf();
                        if (Down.Parse(gsm.Message, out downconf))/* 解析成功 */
                        {
                            if (this.DownDataReceived != null)
                                this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                        }
                    }
                    #endregion
                }
                else
                {
                    if (this.ErrorReceived != null)
                        this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs() { Msg = data });
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        private void Parser(object str)
        {
            try
            {
                string data = str as string;
                /* 删除 '\r\n' 字符串 */
                while (data.StartsWith("\r\n"))
                {
                    data = data.Substring(2);
                }
                /*  
                 * 解析数据，获取CGSMStruct 
                 */
                var gsm = new CGSMStruct();
                if (!GsmHelper.Parse(data, out gsm))
                    return;
                /*  如果解析成功，触发GSM数据接收完成事件  */
                InvokeMessage(data, "接收");

                string msg = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(gsm.Message, out msg))
                    return;

                if (!msg.ToUpper().Contains("TRU"))
                {
                    string sid = msg.Substring(0, 4);
                    string type = msg.Substring(4, 2);

                    /* 
                     *  上行指令信息，
                     *  或者读取参数返回的信息 
                     */
                    #region 1G
                    if (type == "1G")
                    {
                        string reportType = msg.Substring(6, 2);
                        /*  定时报，加报 */
                        if (reportType == "21" || reportType == "22")
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
                                        if (Up.Parse(msg, out report)) /* 解析成功 */
                                        {
                                            report.ChannelType = EChannelType.GSM;
                                            report.ListenPort = "COM" + this.ListenPort.PortName;
                                            report.flagId = gsm.PhoneNumber;
                                            if (this.UpDataReceived != null)
                                                this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = data });
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
                        }
                        else /* 下行指令 */
                        {
                            CDownConf downconf = new CDownConf();
                            if (Down.Parse(msg, out downconf)) /* 解析成功 */
                            {
                                if (this.DownDataReceived != null)
                                    this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                            }
                        }
                    }

                    #endregion

                    #region 1K
                    if (type == "1K") /* 批量传输 */
                    {
                        var station = FindStationBySID(sid);
                        if (station == null)
                            throw new Exception("批量传输，站点传输类型匹配错误");
                        //  EStationBatchType batchType = station.BatchTranType;

                        //if (batchType == EStationBatchType.EFlash)          /* Flash传输 */
                        //{
                        CBatchStruct batch = new CBatchStruct();
                        if (FlashBatch.Parse(gsm.Message, out batch)) /* 解析成功 */
                        {
                            if (this.BatchDataReceived != null)
                                this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        }
                        //}
                        //else if (batchType == EStationBatchType.EUPan)  /* U盘传输 */
                        //{
                        //    CBatchStruct batch = new CBatchStruct();
                        //    if (UBatch.Parse(gsm.Message, out batch))   /* 解析成功 */
                        //    {
                        //        if (this.BatchDataReceived != null)
                        //            this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        //    }
                        //}
                        else
                            throw new Exception("批量传输，站点传输类型匹配错误");
                    }
                    #endregion

                    #region 1S
                    if (type == "1S") /* 远地下行指令，设置参数 */
                    {
                        CDownConf downconf = new CDownConf();
                        if (Down.Parse(gsm.Message, out downconf))/* 解析成功 */
                        {
                            if (this.DownDataReceived != null)
                                this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                        }
                    }
                    #endregion
                }
                else
                {
                    if (this.ErrorReceived != null)
                        this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs() { Msg = data });
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        private void Parser_2(object str)
        {
            try
            {
                string data = str as string;
                /* 删除 '\r\n' 字符串 */
                while (data.StartsWith("\r\n"))
                {
                    data = data.Substring(2);
                }
                /*  
                 * 解析数据，获取CGSMStruct 
                 */
                var gsm = new CGSMStruct();
                if (!GsmHelper.Parse_2(data, out gsm))
                    return;
                /*  如果解析成功，触发GSM数据接收完成事件  */
                //string rawdata = "";
                //string rawdata1 = gsm.Message;
                //if (!rawdata1.Contains('$'))
                //{
                //    rawdata = "$" + rawdata1;
                //}
                InvokeMessage(data, "接收");

                string msg = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(gsm.Message, out msg))
                    return;
                msg = gsm.Message;
                string rawdata = msg;
                if (msg.Contains('$'))
                {
                    msg = msg.Substring(1);
                }
                //if (msg.Contains('9'))
                //{
                //    msg = msg.Substring(1);
                //}
                if (!msg.ToUpper().Contains("TRU"))
                {
                    string sid = msg.Substring(0, 4);
                    string type = msg.Substring(4, 2);

                    /* 
                     *  上行指令信息，
                     *  或者读取参数返回的信息 
                     */
                    #region 1G
                    if (type == "1G")
                    {
                        string reportType = msg.Substring(6, 2);
                        /*  定时报，加报 */
                        if (reportType == "21" || reportType == "22")
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
                                        CEntitySoilData soil = new CEntitySoilData();
                                        CReportStruct soilReport = new CReportStruct();
                                        if (Soil.Parse(rawdata, out soil, out soilReport))
                                        {
                                            soil.ChannelType = EChannelType.GSM;

                                            if (null != this.SoilDataReceived)
                                                this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));
                                            //1111gm
                                            string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            //InvokeMessage(String.Format("{0,-10}   ", temp) + rawdata, "接收");

                                            if (null != soilReport && null != this.UpDataReceived)
                                            {
                                                soilReport.ChannelType = EChannelType.GSM;
                                                soilReport.ListenPort = "COM" + this.ListenPort.PortName;
                                                soilReport.flagId = gsm.PhoneNumber;
                                                this.UpDataReceived(null, new UpEventArgs() { RawData = rawdata, Value = soilReport });
                                            }
                                        }
                                        else
                                        {
                                            //string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            InvokeMessage(" " + rawdata, "接收");
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
                                        if (Up.Parse(msg, out report)) /* 解析成功 */
                                        {
                                            report.ChannelType = EChannelType.GSM;
                                            report.ListenPort = "COM" + this.ListenPort.PortName;
                                            report.flagId = gsm.PhoneNumber;
                                            if (this.UpDataReceived != null)
                                                this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = data });
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
                        }
                        else /* 下行指令 */
                        {
                            CDownConf downconf = new CDownConf();
                            if (Down.Parse(msg, out downconf)) /* 解析成功 */
                            {
                                if (this.DownDataReceived != null)
                                    this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                            }
                        }
                    }

                    #endregion

                    #region 1K
                    if (type == "1K") /* 批量传输 */
                    {
                        var station = FindStationBySID(sid);
                        if (station == null)
                            throw new Exception("批量传输，站点传输类型匹配错误");
                        //  EStationBatchType batchType = station.BatchTranType;

                        //if (batchType == EStationBatchType.EFlash)          /* Flash传输 */
                        //{
                        CBatchStruct batch = new CBatchStruct();
                        if (FlashBatch.Parse(gsm.Message, out batch)) /* 解析成功 */
                        {
                            if (this.BatchDataReceived != null)
                                this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        }
                        //}
                        //else if (batchType == EStationBatchType.EUPan)  /* U盘传输 */
                        //{
                        //    CBatchStruct batch = new CBatchStruct();
                        //    if (UBatch.Parse(gsm.Message, out batch))   /* 解析成功 */
                        //    {
                        //        if (this.BatchDataReceived != null)
                        //            this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        //    }
                        //}
                        else
                            throw new Exception("批量传输，站点传输类型匹配错误");
                    }
                    #endregion

                    #region 1S
                    if (type == "1S") /* 远地下行指令，设置参数 */
                    {
                        CDownConf downconf = new CDownConf();
                        if (Down.Parse(gsm.Message, out downconf))/* 解析成功 */
                        {
                            if (this.DownDataReceived != null)
                                this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                        }
                    }
                    #endregion
                }
                else
                {
                    if (this.ErrorReceived != null)
                        this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs() { Msg = data });
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }

        private void Parser_3(object str)
        {
            try
            {
                string data = str as string;
                /* 删除 '\r\n' 字符串 */
                while (data.StartsWith("\r\n"))
                {
                    data = data.Substring(2);
                }
                /*  
                 * 解析数据，获取CGSMStruct 
                 */
                var gsm = new CGSMStruct();
                if (!GsmHelper.Parse_3(data, out gsm))
                    return;
                /*  如果解析成功，触发GSM数据接收完成事件  */
                //string rawdata = "";
                //string rawdata1 = gsm.Message;
                //if (!rawdata1.Contains('$'))
                //{
                //    rawdata = "$" + rawdata1;
                //}

                if (data.EndsWith("+"))
                {
                    data = data.Substring(0, data.Length - 1);
                }
                InvokeMessage(data, "接收");
                
                string msg = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(gsm.Message, out msg))
                    return;
                msg = gsm.Message;
                string rawdata = msg;
                if (msg.Contains('$'))
                {
                    msg = msg.Substring(1);
                }
                //if (msg.Contains('9'))
                //{
                //    msg = msg.Substring(1);
                //}


                if (!msg.ToUpper().Contains("TRU"))
                {
                    string sid = msg.Substring(0, 4);
                    string type = msg.Substring(4, 2);

                    /* 
                     *  上行指令信息，
                     *  或者读取参数返回的信息 
                     */
                    #region 1G
                    if (type == "1G")
                    {
                        string reportType = msg.Substring(6, 2);
                        /*  定时报，加报 */
                        if (reportType == "21" || reportType == "22")
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
                                        CEntitySoilData soil = new CEntitySoilData();
                                        CReportStruct soilReport = new CReportStruct();
                                        Soil = new Protocol.Data.Lib.SoilParser();
                                        if (Soil.Parse(rawdata, out soil, out soilReport))
                                        {
                                            soil.ChannelType = EChannelType.GSM;

                                            if (null != this.SoilDataReceived)
                                                this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));
                                            //1111gm
                                            string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            //InvokeMessage(String.Format("{0,-10}   ", temp) + rawdata, "接收");

                                            if (null != soilReport && null != this.UpDataReceived)
                                            // if (null != soilReport)
                                            {
                                                soilReport.ChannelType = EChannelType.GSM;
                                                soilReport.ListenPort = "COM" + this.ListenPort.PortName;
                                                soilReport.flagId = gsm.PhoneNumber;
                                                this.UpDataReceived(null, new UpEventArgs() { RawData = rawdata, Value = soilReport });
                                            }
                                        }
                                        else
                                        {
                                            //string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";
                                            InvokeMessage(" " + rawdata, "接收");
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
                                        if (Up.Parse(msg, out report)) /* 解析成功 */
                                        {
                                            report.ChannelType = EChannelType.GSM;
                                            report.ListenPort = "COM" + this.ListenPort.PortName;
                                            report.flagId = gsm.PhoneNumber;
                                            if (this.UpDataReceived != null)
                                                this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = data });
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
                        }
                        else /* 下行指令 */
                        {
                            CDownConf downconf = new CDownConf();
                            if (Down.Parse(msg, out downconf)) /* 解析成功 */
                            {
                                if (this.DownDataReceived != null)
                                    this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                            }
                        }
                    }

                    #endregion

                    #region 1K
                    if (type == "1K") /* 批量传输 */
                    {
                        var station = FindStationBySID(sid);
                        if (station == null)
                            throw new Exception("批量传输，站点传输类型匹配错误");
                        //  EStationBatchType batchType = station.BatchTranType;

                        //if (batchType == EStationBatchType.EFlash)          /* Flash传输 */
                        //{
                        CBatchStruct batch = new CBatchStruct();
                        if (FlashBatch.Parse(gsm.Message, out batch)) /* 解析成功 */
                        {
                            if (this.BatchDataReceived != null)
                                this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        }
                        //}
                        //else if (batchType == EStationBatchType.EUPan)  /* U盘传输 */
                        //{
                        //    CBatchStruct batch = new CBatchStruct();
                        //    if (UBatch.Parse(gsm.Message, out batch))   /* 解析成功 */
                        //    {
                        //        if (this.BatchDataReceived != null)
                        //            this.BatchDataReceived.Invoke(null, new BatchEventArgs() { Value = batch, RawData = data });
                        //    }
                        //}
                        else
                            throw new Exception("批量传输，站点传输类型匹配错误");
                    }
                    #endregion

                    #region 1S
                    if (type == "1S") /* 远地下行指令，设置参数 */
                    {
                        CDownConf downconf = new CDownConf();
                        if (Down.Parse(gsm.Message, out downconf))/* 解析成功 */
                        {
                            if (this.DownDataReceived != null)
                                this.DownDataReceived.Invoke(null, new DownEventArgs() { Value = downconf, RawData = data });
                        }
                    }
                    #endregion
                }
                else
                {
                    if (this.ErrorReceived != null)
                        this.ErrorReceived.Invoke(null, new ReceiveErrorEventArgs() { Msg = data });
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
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

        /// <summary>
        /// 发送AT指令 逐条发送AT指令 调用一次发送一条指令
        /// 能返回一个OK或ERROR算一条指令
        /// </summary>
        /// <param name="ATCom">AT指令</param>
        /// <returns>发送指令后返回的字符串</returns>
        public string SendMsg(string atCom, out bool isSendSuccesse)
        {
            isSendSuccesse = false;
            ListenPort.ReadTimeout = 3000;
            string result = string.Empty;
            //忽略接收缓冲区内容，准备发送
            ListenPort.DiscardInBuffer();

            //注销事件关联，为发送做准备
            ListenPort.DataReceived -= Port_DataReceived;

            try
            {
                /* 写入指令 */
                ListenPort.Write(atCom + "\r");

                /* 接收数据 循环读取数据 直至收到“OK”或“ERROR”*/
                string temp = string.Empty;
                if (!atCom.Contains(GsmHelper.AT_SAVE))
                {
                    while (!temp.Trim().Contains("OK") && !temp.Trim().Contains("ERROR"))
                    {
                        temp = ListenPort.ReadLine();
                        result += temp;
                    }
                    Debug.WriteLine(result);
                }
                else
                {
                    var str = ListenPort.ReadExisting();
                    //while (str.Length != 9)
                    //{
                    //    str += ListenPort.ReadExisting();
                    //}
                    result = str;
                }
                /* 如果缓冲区中包含已发送的指令，则清除 */
                if (result.Contains(atCom))
                {
                    result = result.Replace(atCom, string.Empty);
                }
                isSendSuccesse = true;
            }
            catch (TimeoutException exp)
            {
                isSendSuccesse = false;
                //InvokeMessage("设置参" + atCom + "超时", "接收");
                if (null != GSMTimeOut)
                {
                    GSMTimeOut(this, new ReceivedTimeOutEventArgs() { Second = ListenPort.ReadTimeout / 1000 });
                }
            }
            catch (Exception exp)
            {
                isSendSuccesse = false;
                Debug.WriteLine(String.Format("发送指令:{0} 错误\r{1}", atCom, exp.Message));
            }
            finally
            {
                //事件重新绑定 正常监视串口数据
                ListenPort.DataReceived += Port_DataReceived;
            }
            return result;
        }

        internal class GsmMsgStruct
        {
            public String Phone { get; set; }
            public String Msg { get; set; }
        }
        private void SendMsg_Thead(object obj)
        {
            var gsmStruct = obj as GsmMsgStruct;
            if (null == gsmStruct)
                return;
            string phone = gsmStruct.Phone;
            string msg = gsmStruct.Msg;

            ListenPort.ReadTimeout = 60000;

            string returnMsg = string.Empty;
            try
            {
                //注销事件关联，为发送做准备
                ListenPort.DataReceived -= Port_DataReceived;

                ListenPort.Write("AT+CMGS=" + phone + "\r");
                ListenPort.ReadTo(">");
                ListenPort.Write(msg + (char)(26));
                ListenPort.DiscardInBuffer();

                string wirteMessage = "AT+CMGS=" + phone + "\r" + ">" + msg + (char)(26);
                InvokeMessage(wirteMessage, "发送");

                string temp = string.Empty;

                while (!temp.Trim().Contains("OK") && !temp.Trim().Contains("ERROR"))
                {
                    Debug.WriteLine("temp = " + temp);
                    temp = ListenPort.ReadLine();
                    returnMsg += temp;
                }
                Debug.WriteLine("result = " + returnMsg);

                if (returnMsg.Contains(msg + (char)(26)))
                {
                    returnMsg = returnMsg.Replace((msg + (char)(26)), string.Empty);
                }
                InvokeMessage(returnMsg, "接收");
                IsCommonWorkNormal = true;
            }
            catch (TimeoutException exp)
            {
                IsCommonWorkNormal = false;
                if (null != GSMTimeOut)
                {
                    GSMTimeOut(this, new ReceivedTimeOutEventArgs() { Second = ListenPort.ReadTimeout / 1000 });
                }
                InvokeMessage("短信接收超时", "接收");
                Debug.WriteLine("短信接收失败");
            }
            catch (Exception exp)
            {
                IsCommonWorkNormal = false;
                InvokeMessage("短信接收失败", "接收");
                if (null != GSMTimeOut)
                {
                    GSMTimeOut(this, new ReceivedTimeOutEventArgs() { Second = ListenPort.ReadTimeout / 1000 });
                }
                Debug.WriteLine("短信接收失败");
            }
            finally
            {
                //事件重新绑定 正常监视串口数据
                ListenPort.DataReceived += Port_DataReceived;
            }
            //  GSM通讯口状态监测
            if (SerialPortStateChanged != null)
                SerialPortStateChanged(this, new CEventSingleArgs<CSerialPortState>(new CSerialPortState()
                {
                    PortNumber = Int32.Parse(ListenPort.PortName.Replace("COM", "")),
                    BNormal = IsCommonWorkNormal,
                    PortType = m_portType
                }));

        }
        public void SendMsg(string phone, string msg)
        {
            GsmMsgStruct obj = new GsmMsgStruct() { Phone = phone, Msg = msg };
            Thread sendThead = new Thread(new ParameterizedThreadStart(SendMsg_Thead))
            {
                Name = "发送短信线程"
            };
            sendThead.Start(obj);
        }
        public bool DeleteAll()
        {
            bool result = false;
            string setEmpty = "at+cmgd=1,4";
            Send(setEmpty,out result);
            return result;
        }
        public void Close()
        {
            // 关闭方法
            this.ListenPort.Close(); //关闭监听
        }

        #region log
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

        #region 事件
        public event EventHandler<UpEventArgs> UpDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;

        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;
        public event EventHandler<ReceivedTimeOutEventArgs> GSMTimeOut;
        #endregion

        /// <summary>
        /// Gsm帮助类
        /// </summary>
        internal class GsmHelper
        {
            public static string AT = "at";
            public static string AT_CSMP_QUERY = "at+csmp?";
            public static string AT_CSMP_SET = "at+csmp=17,167,0,240";
            public static string AT_CNMI_QUERY = "at+cnmi?";
            public static string AT_CNMI_SET = "at+cnmi=2,2,0,0,1";
            // public static string AT_SAVE = "at&w";
            public static string AT_SAVE = "at&w";
            public static string AT_CMPR = "at+cmpr=";
            public static string AT_CMPD = "at+cmpd=";

            /// <summary>
            /// GSM短信数据包解析
            /// 数据包格式:
            /// +CMT: "+8612345678901",,"14/02/11,14:31:21+32"
            ///  hhagfh
            ///  T: "+8613886090477",,"16/11/15,16:46:35+32"

            ///  10071G03161115163055
            /// </summary>
            public static bool Parse(String data, out CGSMStruct gsm)
            {
                gsm = new CGSMStruct();
                try
                {
                    //  解析GSM号码
                    gsm.PhoneNumber = data.Substring(9, 11);
                    //  解析时间
                    string time = data.Substring(24, 20);
                    int year = Int32.Parse("20" + time.Substring(0, 2));  //  年       yy
                    int month = Int32.Parse(time.Substring(3, 2)); //  月       mm
                    int day = Int32.Parse(time.Substring(6, 2));   //  日       dd
                    int hour = Int32.Parse(time.Substring(9, 2));  //  小时      hh
                    int minute = Int32.Parse(time.Substring(12, 2));//  分钟   mm  
                    int second = Int32.Parse(time.Substring(15, 2));//  秒   second  
                    gsm.Time = new DateTime(year, month, day, hour, minute, second);
                    //  解析数据包
                    gsm.Message = data.Substring(45).Trim();

                    return true;
                }
                catch (Exception ex){ }
                return false;
            }


            public static bool ParseGsm(String strTime,String strPhone,String data, out CGSMStruct gsm)
            {
                gsm = new CGSMStruct();
                try
                {
                    //  解析GSM号码
                  //  gsm.PhoneNumber = data.Substring(9, 11);
                    gsm.PhoneNumber = strPhone;
                    //  解析时间
                  //  string time = data.Substring(24, 20);
                    string time = strTime;
                    int year = Int32.Parse("20" + time.Substring(0, 2));  //  年       yy
                    int month = Int32.Parse(time.Substring(3, 2)); //  月       mm
                    int day = Int32.Parse(time.Substring(6, 2));   //  日       dd
                    int hour = Int32.Parse(time.Substring(9, 2));  //  小时      hh
                    int minute = Int32.Parse(time.Substring(12, 2));//  分钟   mm  
                    int second = Int32.Parse(time.Substring(15, 2));//  秒   second  
                    gsm.Time = new DateTime(year, month, day, hour, minute, second);
                    //  解析数据包
                    gsm.Message = data.Trim();

                    return true;
                }
                catch (Exception ex) { }
                return false;
            }

            public static bool Parse_2(String data, out CGSMStruct gsm)
            {
                gsm = new CGSMStruct();
                try
                {
                    //  解析GSM号码
                    gsm.PhoneNumber = data.Substring(5, 11);
                    //  解析时间
                    string time = data.Substring(21, 20);
                    int year = Int32.Parse("20" + time.Substring(0, 2));  //  年       yy
                    int month = Int32.Parse(time.Substring(3, 2)); //  月       mm
                    int day = Int32.Parse(time.Substring(6, 2));   //  日       dd
                    int hour = Int32.Parse(time.Substring(9, 2));  //  小时      hh
                    int minute = Int32.Parse(time.Substring(12, 2));//  分钟   mm  
                    int second = Int32.Parse(time.Substring(15, 2));//  秒   second  
                    gsm.Time = new DateTime(year, month, day, hour, minute, second);
                    //  解析数据包
                    gsm.Message = data.Substring(42).Trim();

                    return true;
                }
                catch (Exception ex) { }
                return false;
            }

            public static bool Parse_3(String data, out CGSMStruct gsm)
            {
                gsm = new CGSMStruct();
                try
                {
                    data = data.Replace(" ", "");
                    //  解析GSM号码
                    gsm.PhoneNumber = data.Substring(data.IndexOf("+86") + 3, 11);
                    //  解析时间
                    gsm.Time = DateTime.Now;
                    string message = data.Substring(data.IndexOf("$")).Trim();
                    if (message.EndsWith("+"))
                    {
                        message = message.Substring(0, message.Length - 1);
                    }
                    //  解析数据包
                    //gsm.Message = data.Substring(42).Trim();
                    gsm.Message = message;
                    return true;
                }
                catch (Exception ex) { }
                return false;
            }
        }
    }
}
