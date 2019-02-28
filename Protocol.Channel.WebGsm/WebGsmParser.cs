using Hydrology.Entity;
using Protocol.Channel.Interface;
using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using System.Xml;

namespace Protocol.Channel.WebGsm
{
    public class WebGsmParser : IWebGsm
    {

        #region 成员变量

        private Semaphore m_semaphoreData;    //用来唤醒消费者处理缓存数据
        private Thread m_threadDealData;    // 处理数据线程
        private List<string> m_listDatas;   //存放data的内存缓存
        private Mutex m_mutexListDatas;     // 内存data缓存的互斥量

        private System.Timers.Timer tmrData;
        private SMSService.SMSService sMS;
        private string account;
        private string password;
        private bool inDataTick;
        private bool gsmRecv;
        #endregion 成员变量

        #region 属性
        private EChannelType m_channelType;
        private List<CEntityStation> m_stationLists;

        public IUp Up { get; set; }
        public IDown Down { get; set; }
        public IUBatch UBatch { get; set; }
        public IFlashBatch FlashBatch { get; set; }
        public ISoil Soil { get; set; }

        public Boolean IsCommonWorkNormal { get; set; }
        #endregion

        #region 事件
        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<UpEventArgs> UpDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;
        #endregion

        public WebGsmParser()
        {
            // 初始化成员变量
            inDataTick = false;
            m_semaphoreData = new Semaphore(0, Int32.MaxValue);
            m_listDatas = new List<string>();
            m_mutexListDatas = new Mutex();
            this.m_stationLists = new List<CEntityStation>();

            m_threadDealData = new Thread(new ThreadStart(this.DealData));
        }

        #region 数据维护
        private void tmrData_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (inDataTick) return;
            inDataTick = true;
            try
            {
                string gsmMsg = String.Empty;
                bool getGsm = GetSMSInbox(ref gsmMsg);

                m_mutexListDatas.WaitOne();
                m_listDatas.Add(gsmMsg);
                m_semaphoreData.Release(1);
                m_mutexListDatas.ReleaseMutex();
            }
            catch (Exception e1)
            {
                Debug.WriteLine("读取数据异常" + e1);
            }
            finally
            {
                inDataTick = false;
            }
        }

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
                List<string> dataListTmp = m_listDatas;
                m_listDatas = new List<string>(); //开辟一快新的缓存区
                m_mutexListDatas.ReleaseMutex();
                // 开始处理数据
                for (int i = 0; i < dataListTmp.Count; ++i)
                {
                    try
                    {
                        string rawGsm = dataListTmp[i];

                        if (!rawGsm.Contains("1G"))
                        {
                            continue;
                        }

                        Gprs.WriteToFileClass writeClass = new Gprs.WriteToFileClass("ReceivedLog");
                        Thread t_gsm = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                        t_gsm.Start("Web-GSM: " + "长度:" + rawGsm.Length + " " + rawGsm + "\r\n");

                        StringReader Reader = new StringReader(rawGsm);
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(Reader);

                        XmlNodeList nodeList = xmlDoc.SelectSingleNode("datalist").ChildNodes;

                        foreach (XmlNode xnf in nodeList)
                        {
                            XmlElement xe = (XmlElement)xnf;
                            string sid = xe.SelectSingleNode("id").InnerText;
                            string timeStr = xe.SelectSingleNode("uptime").InnerText;
                            DateTime recvtime = DateTime.Parse(timeStr);
                            string addr = xe.SelectSingleNode("destaddr").InnerText;
                            string report = xe.SelectSingleNode("content").InnerText;

                            CGSMStruct gsm = new CGSMStruct() { Message = report, PhoneNumber = addr, Time = recvtime };

                            DealSMSData(addr, gsm);
                        }
                    }
                    catch (Exception ee)
                    {

                    }
                }
            }
        }


        private bool GetSMSInbox(ref string gsmMsg)
        {
            try
            {
                gsmMsg = sMS.GetSMSInbox(account, password);

                if (gsmMsg == "-1")
                {
                    gsmRecv = false;
                    InvokeMessage("WebGSM用户登录失败", "读取数据");
                    return false;
                }
                else if (gsmMsg == "-2")
                {
                    gsmRecv = false;
                    InvokeMessage("WebGSM数据获取失败", "读取数据");
                    return false;
                }
                gsmRecv = true;
                return true;
            }
            catch (Exception ee)
            {
                gsmRecv = false;
                InvokeMessage("WebGSM数据异常", "读取数据");
                return false;
            }
        }

        /// <summary>
        /// 数据格式
        /// 
        ///   <?xml version="1.0" encoding="utf-8" ?> 
        ///<string xmlns = "http://tempuri.org/" ><? xml version = "1.0" encoding="UTF-8" ?>
        ///<datalist>
        ///<data>
        ///<id>1000</id>
        ///<uptime>2018-10-09 09:10:10</uptime>
        ///<destaddr>1064810588860</destaddr>
        ///<content>检测点上行短信内容测试111</content></data>
        ///<data><id>1001</id><uptime>2018-10-09 10:31:30</uptime>
        ///<destaddr>1064810589867</destaddr>
        ///<content>检测点上行短信内容测试222</content></data></datalist></string> 
        ///
        /// </summary>
        /// <returns></returns>
        private bool DealSMSData(string addr, CGSMStruct gsm)
        {
            bool isSoil = FindStationByGSMID(addr);
            string rawdata = gsm.Message;

            try
            {
                if (isSoil)
                {
                    // 是墒情站
                    CEntitySoilData soil = new CEntitySoilData();
                    CReportStruct soilReport = new CReportStruct();
                    if (Soil.Parse(rawdata, out soil, out soilReport))
                    {
                        soil.ChannelType = EChannelType.GSM;

                        if (null != this.SoilDataReceived)
                            this.SoilDataReceived(null, new CEventSingleArgs<CEntitySoilData>(soil));

                        string temp = soilReport.ReportType == EMessageType.EAdditional ? "加报" : "定时报";

                        if (null != soilReport && null != this.UpDataReceived)
                        {
                            soilReport.ChannelType = EChannelType.GSM;
                            soilReport.ListenPort = "WebService-GSM";
                            soilReport.flagId = gsm.PhoneNumber;
                            this.UpDataReceived(null, new UpEventArgs() { RawData = rawdata, Value = soilReport });
                        }
                    }
                    else
                    {
                        InvokeMessage(" " + rawdata, "接收");
                    }
                }
                else
                {
                    // 是水情站
                    CReportStruct report = new CReportStruct();
                    if (Up.Parse(rawdata, out report)) /* 解析成功 */
                    {
                        report.ChannelType = EChannelType.GSM;
                        report.ListenPort = "WebService-GSM";
                        report.flagId = gsm.PhoneNumber;
                        if (this.UpDataReceived != null)
                            this.UpDataReceived.Invoke(null, new UpEventArgs() { Value = report, RawData = rawdata });
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool FindStationByGSMID(string addr)
        {
            if (this.m_stationLists == null)
                throw new Exception("GSM模块未初始化站点！");

            bool result = true;
            foreach (var station in this.m_stationLists)
            {
                if (station.GSM.Equals(addr))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        #endregion

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

        #region 接口方法
        public bool Init(string ip, int port, string account, string password)
        {
            try
            {
                if (ip == "" || port == 0)
                {
                    return false;
                }
                ///http://223.100.11.11:7080/SMSService.asmx
                sMS = new SMSService.SMSService();
                string reference = ip;
                sMS.Url = reference;
                this.account = account;
                this.password = password;
                this.m_channelType = EChannelType.GSM;

                if (tmrData == null)
                    tmrData = new System.Timers.Timer(250);
                tmrData.Elapsed += new ElapsedEventHandler(tmrData_Elapsed);


                tmrData.Start();
                m_threadDealData.Start();
                //Start();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }

        public bool Start()
        {
            tmrData.Start();
            Thread.Sleep(500);
            InvokeMessage(String.Format("开启Web-GSM...  {0}!", gsmRecv ? "成功" : "失败"), "初始化");
            return gsmRecv;
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

        public void Close()
        {
            tmrData.Stop();
            try
            {
                m_threadDealData.Abort(); //终止处理数据线程
            }
            catch (Exception exp)
            {
                Debug.WriteLine(exp.Message);
            }
        }
        #endregion
    }
}
