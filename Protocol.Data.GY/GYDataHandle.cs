using System;
using System.Collections.Generic;

using Protocol.Data.Interface;
using Hydrology.Entity;
using System.Diagnostics;
using Entity.Protocol.Channel;
using System.Text.RegularExpressions;

/// <summary>
/// 暂时通过函数调用的方式来实现，之后可以考虑采用消息队列的方式提供
/// </summary>
namespace Protocol.Data.GY
{
    public class GYDataHandle : IHandle
    {
        # region 成员变量
        private List<SendOrRecvMsgEventArgs> sendOrRecvMsgEventArgsList;
        private List<BatchEventArgs> batchEventArgsList;
        private List<ReceiveErrorEventArgs> receivedErrorEventArgsList;
        private List<UpEventArgs> upEventArgsList;
        private List<DownEventArgs> downEventArgsList;
        private List<Dictionary<string, string>> retList;


        private SendOrRecvMsgEventArgs sendOrRecvMsgEventArgs;
        private BatchEventArgs batchEventArgs;
        private ReceiveErrorEventArgs receiveErrorEventArgs;
        private UpEventArgs upEventArgs;
        private DownEventArgs downEventArgs;
        private Dictionary<string, string> ret;



        DownParser downPaesr = new DownParser();
        UpParser upParser = new UpParser();
        #endregion 

        #region 构造函数
        public GYDataHandle()
        {
            sendOrRecvMsgEventArgsList = new List<SendOrRecvMsgEventArgs>();
            batchEventArgsList = new List<BatchEventArgs>();
            receivedErrorEventArgsList = new List<ReceiveErrorEventArgs>();
            upEventArgsList = new List<UpEventArgs>();
            downEventArgsList = new List<DownEventArgs>();

            sendOrRecvMsgEventArgs = new SendOrRecvMsgEventArgs();
            batchEventArgs = new BatchEventArgs();
            receiveErrorEventArgs = new ReceiveErrorEventArgs();
            upEventArgs = new UpEventArgs();
            downEventArgs = new DownEventArgs();

            ret = new Dictionary<string, string>();
            retList = new List<Dictionary<string, string>>();

        }
        #endregion 构造函数

        public Dictionary<string,Object> getHandledData(CRouter router,string ascOrASCII)
        {
            Dictionary<string, object> resMap = new Dictionary<string, object>();
           
            retList.Clear();
            sendOrRecvMsgEventArgsList.Clear();
            upEventArgsList.Clear();


            resMap["isTru"] = null;
            #region 处理16进制的规约报文
            if (ascOrASCII == "Hex")
            {
                //存储报文数据
                string data = string.Empty;

                byte[] inputBytes = router.rawData;
                int inputLength = router.dataLength; ;

                if (inputBytes != null)
                {
                    for (int j = 0; j < inputBytes.Length; j++)
                    {
                        if (j == 1024)
                        {
                            inputLength = Int32.Parse(inputBytes[j].ToString()) * 2;
                        }
                        data += inputBytes[j].ToString("X2");
                    }
                }

                //如果数据部分不足三位 则直接返回
                string tempData = data.Replace("0", "");
                if (tempData.Length < 3)
                {
                    return null;
                }

                string temp = data.Trim();

                if (router.data.Contains("TRU"))
                {
                    Debug.WriteLine("接收数据TRU完成");

                    //将日志信息放入需打印的日志信息结构
                    sendOrRecvMsgEventArgs.Msg = "TRU" + router.dutid;
                    sendOrRecvMsgEventArgs.Description = "接收";
                    sendOrRecvMsgEventArgsList.Add(sendOrRecvMsgEventArgs);

                    receiveErrorEventArgs.Msg = "TRU" + router.dutid;
                    receivedErrorEventArgsList.Add(receiveErrorEventArgs);

                }
                if (router.data.Contains("ATE0"))
                {
                    Debug.WriteLine("接收数据ATE0完成");
                    receiveErrorEventArgs.Msg = "ATE0";
                    receivedErrorEventArgsList.Add(receiveErrorEventArgs);
                }
                if (router.data.Contains("7E7E"))
                {
                    //是否需要回执信息
                    string messageListStr = temp.Substring(temp.IndexOf("7E7E"));
                    messageListStr.Replace("7E7E", "$");
                    var lists = messageListStr.Split('$');
                    //发送回执
                    
                    resMap["isTru"] = true;

                    //2.解析上行报文
                    CReportStruct report = new CReportStruct();
                    IUp upParser = new UpParser();
                    
                    
                    foreach(var list in lists)
                    {
                        string message = "7E7E" + list;
                        #region 判定是否需要回执，如果需要则回执
                        string funcCode = data.Substring(20, 2);
                        if(funcCode == "30" || funcCode == "31" || funcCode == "32" || funcCode == "33" || funcCode == "34" || funcCode == "35")
                        {
                            string head = data.Substring(0, 4);
                            string stationAddr = data.Substring(6, 10);
                            string centerAddr = data.Substring(4, 2);
                            string passAndFunc = data.Substring(16, 6);
                            int dataLength = 16;
                            string length = Convert.ToString(dataLength, 16);
                            string flag = "1" + length;
                            string serialNumber = data.Substring(22, 4);
                            string time = data.Substring(26, 12);
                            string dataMessage = head + centerAddr + stationAddr + passAndFunc + flag + "02" + serialNumber + time + "03";
                            string CRCMessage = CRC.ToCRC16(dataMessage, false);
                            string downMessage = dataMessage + CRCMessage;
                            ret[stationAddr] = downMessage;
                            retList.Add(new Dictionary<string, string>(ret)); //深拷贝进行赋值，防止后续clear后值不存在
                            ret.Clear();
                        }
                        #endregion

                        string rtype = "";
                        
                        upParser.Parse(message, out report);
                        switch (report.ReportType)
                        {
                            case EMessageType.EUinform:
                                rtype = "均匀时段报";
                                break;
                            case EMessageType.EAdditional:
                                rtype = "加报";
                                break;
                            case EMessageType.ETimed:
                                rtype = "定时报";
                                break;
                            case EMessageType.EHour:
                                rtype = "小时报";
                                break;
                        }
                        sendOrRecvMsgEventArgs.Msg = String.Format("{0,-10}   ", rtype) + message;
                        Debug.WriteLine(message);
                        sendOrRecvMsgEventArgs.Description = "接收";
                        sendOrRecvMsgEventArgsList.Add(sendOrRecvMsgEventArgs);

                        upEventArgs.Value = report;
                        upEventArgs.RawData = message;
                        upEventArgsList.Add(upEventArgs);
                    }
                }
            }
            #endregion

            #region 处理规约ASCII报文 000011223344123430004D0787181218192911ST 0011223344 H TT 1812181925 PJ 0.0 PT 3.5 Z 1.234 VT 12.29 4F49
            if(ascOrASCII == "ASCII")
            {
                CReportStruct report = new CReportStruct();
                IUp upParser = new UpParser();
                string data = string.Empty;
                byte[] inputBytes = router.rawData;
                int inputLength = router.dataLength;
                byte[] dataByteList = new byte[inputLength];

                for (int i = 0; i < inputLength; i++)
                {
                    dataByteList[i] = inputBytes[i];
                    if (i == 2048)
                    {
                        inputLength = Int32.Parse(inputBytes[i].ToString()) * 2;
                    }
                }
                string gprsid = string.Empty;
                try
                {
                    string messageStr = System.Text.Encoding.ASCII.GetString(dataByteList);
                    if (messageStr.Contains("\u0001") && messageStr.Contains("\u0002") && messageStr.Contains("\u0003"))
                    {
                        string[] messageList = Regex.Split(messageStr, "\u0001", RegexOptions.IgnoreCase);
                        for (int i = 0; i < messageList.Length; i++)
                        {
                            string ondData = messageList[i];
                            //范例报文 00 0011223344 1234 30 004D 0787 181218192911 ST 0011223344 H TT 1812181925 PJ 0.0 PT 3.5 Z 1.234 VT 12.29 4F49
                            if (ondData.Contains("\u0002") && ondData.Contains("\u0003"))
                            {
                                string[] oneDataList = Regex.Split(ondData, "\u0002", RegexOptions.IgnoreCase);
                                if (oneDataList.Length == 2)
                                {
                                    string header = oneDataList[0];
                                    string funcCode = header.Substring(16, 2);
                                    string body = oneDataList[1];
                                    if (funcCode == "30" || funcCode == "31" || funcCode == "32" || funcCode == "33" || funcCode == "34" || funcCode == "35")
                                    {
                                        #region 根据header的内容进行回复 000011223344123430004D
                                        string meaageFlag = messageStr.Substring(0, 1);
                                        string stationAddr = header.Substring(2, 10);
                                        string centerAddr = header.Substring(0, 2);
                                        string passAndFunc = header.Substring(12, 6);
                                        int dataLength = 16;
                                        string length = Convert.ToString(dataLength, 16);
                                        string flag = "1" + "0" + length;
                                        string serialNumber = body.Substring(0, 4);
                                        string time = body.Substring(4, 12);
                                        string dataMessage = meaageFlag + centerAddr + stationAddr + passAndFunc + flag + "\u0002" + serialNumber + time + "\u0003";
                                        string CRCMessage = CRC.ToCRC16(dataMessage, false);
                                        string downMessage = dataMessage + CRCMessage;
                                        ret[router.sessionid] = downMessage;
                                        retList.Add(new Dictionary<string, string>(ret)); //深拷贝进行赋值，防止后续clear后值不存在
                                        ret.Clear();
                                        #endregion
                                    }
                                    string[] bodyList = Regex.Split(messageStr, "\u0003", RegexOptions.IgnoreCase);
                                    if (bodyList.Length == 2)
                                    {
                                        string dataGram = bodyList[0];
                                        string check = bodyList[1];
                                        if (check.Length != 4)
                                        {
                                            Debug.WriteLine("校验位错误：" + ondData);
                                            return null;
                                        }
                                        upParser.Parse(ondData, out report);
                                        string rtype = "";
                                        switch (report.ReportType)
                                        {
                                            case EMessageType.EUinform:
                                                rtype = "均匀时段报";
                                                break;
                                            case EMessageType.EAdditional:
                                                rtype = "加报";
                                                break;
                                            case EMessageType.ETimed:
                                                rtype = "定时报";
                                                break;
                                            case EMessageType.EHour:
                                                rtype = "小时报";
                                                break;
                                            case EMessageType.ETest:
                                                rtype = "测试报";
                                                break;
                                        }
                                        sendOrRecvMsgEventArgs.Msg = String.Format("{0,-10}   ", rtype) + ondData;
                                        Debug.WriteLine(ondData);
                                        sendOrRecvMsgEventArgs.Description = "接收";
                                        sendOrRecvMsgEventArgsList.Add(sendOrRecvMsgEventArgs);

                                        upEventArgs.Value = report;
                                        upEventArgs.RawData = ondData;
                                        upEventArgsList.Add(upEventArgs);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch(Exception e)
                {

                }
                

                }

            #endregion

            resMap["SORMEA"] = sendOrRecvMsgEventArgsList;
            resMap["BEA"] = batchEventArgsList;
            resMap["REEA"] = receivedErrorEventArgsList;
            resMap["UEA"] = upEventArgsList;
            resMap["DEA"] = downEventArgsList;
            resMap["RET"] = retList;
            return resMap;
        }
    }
}
