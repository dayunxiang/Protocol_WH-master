using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydrology.Entity;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.IO;

namespace Protocol.Data.GY
{
    public class UpParser : IUp
    {
        /// <summary>
        /// 规约协议上行数据解析
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="report"></param>
        /// <returns></returns>
        public bool Parse(string msg, out CReportStruct report)
        {
            report = null;
            try
            {
                if (msg == "")
                {
                    return false;
                }
               
                /// 数据格式结束
                string data = msg;
                // 2018-12-11 gaoming 添加
                string funcCode = data.Substring(20, 2);
                //丢弃包头 (4位)
                //data = data.Substring(4);
                //丢弃中心站地址（2位）
                data = data.Substring(2);
                //遥测站地址（10位）00+8位站号
                string id = data.Substring(0, 10);
                data = data.Substring(10);
                //丢弃密码（4位）
                data = data.Substring(4);
                //功能码即报类（2位） 31、32、33、34 分别是均匀时段报、定时报、加报、小时报
                string type = data.Substring(0, 2);
                data = data.Substring(2);
                //报文上下文标识及长度（4位）
                string context = data.Substring(0, 1);
                string contextLengthString = data.Substring(1, 3);
                if (context != "0")
                {
                    // 不是上行报文
                    return false;
                }
                // 报文正文长度 16进制转10进制（字节数）*2
                int contextLength = (int.Parse(contextLengthString, System.Globalization.NumberStyles.AllowHexSpecifier));
                data = data.Substring(4);
                // 丢弃起始符 02
                data = data.Substring(1);

                // 报文数据
                string message = data.Substring(0, contextLength);
                data = data.Substring(contextLength);
                bool result = DealData(message, type, out report);

                report.Stationid = id;
                if(type == "2F" || type == "30" || type == "31" || type == "32" || type == "33" || type == "34" || type == "35")
                {
                    report.Type = "1G";
                }
                else
                {
                    report.Type = "1S";
                }
                
                report.RecvTime = DateTime.Now;
                switch (type)
                {
                    case "2F":
                        break; //链路维持报
                    case "30": //测试报
                        report.ReportType = EMessageType.ETest;
                        break;
                    case "31":
                        report.ReportType = EMessageType.EUinform;
                        break;
                    case "32":
                        report.ReportType = EMessageType.ETimed;
                        break;
                    case "33":
                        report.ReportType = EMessageType.EAdditional;
                        break;
                    case "34":
                        report.ReportType = EMessageType.EHour;
                        break;
                    case "35": // 人工置数报
                        report.ReportType = EMessageType.EArtifNReport;
                        break;
                    case "36": // 遥测站图片报或中心站查询遥测站图片采集信息
                        report.ReportType = EMessageType.EPicture;
                        break;
                    case "37": // 查询遥测站实时数据
                        report.ReportType = EMessageType.ERdata;
                        break;
                    case "38": // 查询遥测站时段数据
                        report.ReportType = EMessageType.EPdata;
                        break;
                    case "39": // 查询遥测站人工置数
                        report.ReportType = EMessageType.EArtifN;
                        break;
                    case "40": // 修改遥测站基本数据
                        report.ReportType = EMessageType.EbasicConfigModify;
                        break;
                    case "41": // 读取遥测站基本配置表
                        report.ReportType = EMessageType.EbasicConfigRead;
                        break;
                    case "42": // 修改遥测站运行参数配置表
                        report.ReportType = EMessageType.EoperatingParaModify;
                        break;
                    case "43": // 读取遥测站运行参数配置表
                        report.ReportType = EMessageType.EoperatingParaRead;
                        break;
                    case "44": // 查询水泵电机实时工作数据
                        report.ReportType = EMessageType.EpumpRead;
                        break;
                    case "45": // 查询遥测终端软件版本
                        report.ReportType = EMessageType.Eversion;
                        break;
                    case "46": // 查询遥测站状态和报警信息
                        report.ReportType = EMessageType.Ealarm;
                        break;
                    case "47": // 初始化固态存储数据
                        report.ReportType = EMessageType.EmemoryReset;
                        break;
                    case "48":// 恢复终端出厂设置
                        report.ReportType = EMessageType.EReset;
                        break;
                    case "49":// 修改密码
                        report.ReportType = EMessageType.EChangepwd;
                        break;
                    case "4A":// 设置遥测站时钟
                        report.ReportType = EMessageType.Eclockset;
                        break;
                    case "4B":// 设置遥测终端IC卡状态
                        report.ReportType = EMessageType.EICconfig;
                        break;
                    case "4C":// 控制水泵开关命令/水泵状态信息自报
                        report.ReportType = EMessageType.EpumpCtrl;
                        break;
                    case "4D":// 控制阀门开关命令/阀门状态信息自报
                        report.ReportType = EMessageType.EvalveCtrl;
                        break;
                    case "4E":// 控制闸门开关命令/闸门状态信息自报
                        report.ReportType = EMessageType.EgateCtrl;
                        break;
                    case "4F":// 水量定值控制命令
                        report.ReportType = EMessageType.EwaterYield;
                        break;
                    case "50":// 中心站查询遥测站事件记录
                        report.ReportType = EMessageType.Ehistory;
                        break;
                    default:
                        report.ReportType = EMessageType.Manual;
                        break;

                }

                // 丢弃结束符 03
                data = data.Substring(1);
                // 丢弃校验
                data = data.Substring(4);

                return result;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("数据：" + msg);
                System.Diagnostics.Debug.WriteLine("规约协议解析不完整" + e.Message);
            }
            return false;
        }
        #region 帮助函数
        /// <summary>
        /// CRC16校验  byte[] 转b byte[];
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] CRC16(byte[] data)
        {
            byte[] returnVal = new byte[2];
            byte CRC16Lo, CRC16Hi, CL, CH, SaveHi, SaveLo;
            int i, Flag;
            CRC16Lo = 0xFF;
            CRC16Hi = 0xFF;
            CL = 0x86;
            CH = 0x68;
            for (i = 0; i < data.Length; i++)
            {
                CRC16Lo = (byte)(CRC16Lo ^ data[i]);//每一个数据与CRC寄存器进行异或
                for (Flag = 0; Flag <= 7; Flag++)
                {
                    SaveHi = CRC16Hi;
                    SaveLo = CRC16Lo;
                    CRC16Hi = (byte)(CRC16Hi >> 1);//高位右移一位
                    CRC16Lo = (byte)(CRC16Lo >> 1);//低位右移一位
                    if ((SaveHi & 0x01) == 0x01)//如果高位字节最后一位为
                    {
                        CRC16Lo = (byte)(CRC16Lo | 0x80);//则低位字节右移后前面补 否则自动补0
                    }
                    if ((SaveLo & 0x01) == 0x01)//如果LSB为1，则与多项式码进行异或
                    {
                        CRC16Hi = (byte)(CRC16Hi ^ CH);
                        CRC16Lo = (byte)(CRC16Lo ^ CL);
                    }
                }
            }
            returnVal[0] = CRC16Hi;//CRC高位
            returnVal[1] = CRC16Lo;//CRC低位
            return returnVal;
        }

        #endregion


        public bool Parse_beidou(string sid, EMessageType type, string msg, out CReportStruct upReport)
        {
            throw new NotImplementedException();
        }

        public bool DealData(string msg, string reportType, out CReportStruct report)
        {
            EStationType type = new EStationType();
            report = new CReportStruct();
            try
            {
                string data = msg;
                // 丢弃流水号（4位）
                #region 处理公共部分
                data = data.Substring(4);
                // 丢弃发报时间（12位）
                DateTime sendTime = new DateTime(
                    year: int.Parse("20" + data.Substring(0, 2)),
                    month: int.Parse(data.Substring(2, 2)),
                    day: int.Parse(data.Substring(4, 2)),
                    hour: int.Parse(data.Substring(6, 2)),
                    minute: int.Parse(data.Substring(8, 2)),
                    second: int.Parse(data.Substring(10, 2))
                    );
                //删除发送时间
                data = data.Substring(12);
                //删除人工置数符号、空格
                if (data.Substring(0, 4).Equals("RGZS"))
                {
                    data = data.Substring(5);
                }
                //删除ST  站号引导符号
                data = data.Substring(2);
                //删除空格
                data = data.Substring(1);
                //获取站点ID
                string stationId = data.Substring(0, 10);
                //删除站点ID
                data = data.Substring(10);
                //删除空格
                data = data.Substring(1);
                #endregion

                #region 数据初始话
                // report中的数据初始化
                List<CReportData> datas = new List<CReportData>();
                TimeSpan span = new TimeSpan(0);

                DateTime dataTime = sendTime;

                Nullable<Decimal> currentRain = null;
                Nullable<Decimal> totalRain = null;
                Nullable<Decimal> waterStage = null;
                Nullable<Decimal> voltage = null;

                Nullable<Decimal> diffRain = null;
                Nullable<Decimal> dayrain = null;
                Nullable<Decimal> heavyRain = null;
                Nullable<Decimal> stepnumD = null;
                Nullable<Decimal> stepnumH = null;
                Nullable<Decimal> stepnumN = null;
                Nullable<Decimal> vta = null;
                Nullable<Decimal> vtb = null;
                Nullable<Decimal> vtc = null;
                Nullable<Decimal> via = null;
                Nullable<Decimal> vib = null;
                Nullable<Decimal> vic = null;
                string version = null;
                string picture = null;
                string alarm = null;
                string icConfig = null;
                string pumpstate = null;
                string valvestate = null;
                string gatestate = null;
                string waterctrl = null;
                string history = null;
                Nullable<Decimal> sectionArea = null;
                Nullable<Decimal> airTemp = null;
                Nullable<Decimal> waterTemp = null;
                Nullable<Decimal> evaporationDay = null;
                Nullable<Decimal> pevaporation = null;
                Nullable<Decimal> airpressure = null;
                Nullable<Decimal> openheight = null;
                Nullable<Decimal> waterequipNum = null;
                Nullable<Decimal> waterequipType = null;
                Nullable<Decimal> holenum = null;
                Nullable<Decimal> groundtemp = null;
                Nullable<Decimal> groundwaterdepth = null;
                Nullable<Decimal> waveheight = null;
                Nullable<Decimal> soilhum10 = null;
                Nullable<Decimal> soilhum20 = null;
                Nullable<Decimal> soilhum30 = null;
                Nullable<Decimal> soilhum40 = null;
                Nullable<Decimal> soilhum50 = null;
                Nullable<Decimal> soilhum60 = null;
                Nullable<Decimal> soilhum80 = null;
                Nullable<Decimal> soilhum100 = null;
                Nullable<Decimal> humidity = null;
                Nullable<Decimal> rain12hour = null;
                Nullable<Decimal> rain6hour = null;
                Nullable<Decimal> rain3hour = null;
                Nullable<Decimal> rain2hour = null;
                Nullable<Decimal> rain1hour = null;
                Nullable<Decimal> rain30 = null;
                Nullable<Decimal> rain10 = null;
                Nullable<Decimal> rain05 = null;
                Nullable<Decimal> rain01 = null;

                //遥测站基本配置
                string sid = null;//中心站地址
                string yid = null;//遥测站地址
                string password = null;//密码
                string desinfo1 = null;//目的地 1 信道类型及地址
                string desinfo2 = null;//目的地 2 信道类型及地址
                string desinfo3 = null;//目的地 3 信道类型及地址
                string desinfo4 = null;//目的地 4 信道类型及地址
                Nullable<Decimal> channelset = null;//主备信道设置
                Nullable<Decimal> workmode = null;//工作方式
                string collectelements = null;//遥测站采集要素设置
                string addressRange = null;//中继站（集合转发站）服务地址范围
                string idnum = null;//遥测站通信设备识别号
                Nullable<Decimal>[] oparameters = new Nullable<Decimal>[138];//运行参数初始化
                #endregion


                #region 处理查询回复报文
                CReportData downData = new CReportData();
                //修改密码
                if (reportType.Equals("49"))
                {
                    string str = data.Substring(0, 2);
                    if (str.Equals("03"))
                    {
                        password = data.Substring(3, 4);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("遥测站密码信息读取错误");
                    }
                }
                //查询软件版本
                if (reportType.Equals("45"))
                {
                    string length = data.Substring(0, 2); //获取版本信息长度
                    version = data.Substring(3, Convert.ToInt32(length, 16));//获取版本信息
                    downData.Version = version;
                }
                //查询遥测站状态及报警信息
                else if (reportType.Equals("46"))
                {
                    string str = data.Substring(0, 2);
                    if (str.Equals("ZT"))
                    {
                        alarm = data.Substring(3, 8);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("遥测站状态及报警信息读取错误");
                    }
                    downData.Alarm = alarm;
                }
                //设罝遥测站IC卡状态
                else if (reportType.Equals("4B"))
                {
                    string str = data.Substring(0, 2);
                    if (str.Equals("ZT"))
                    {
                        icConfig = data.Substring(3, 8);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("遥测站状态及报警信息读取错误");
                    }
                }
                //控制水泵状态
                else if (reportType.Equals("4C"))
                {
                    pumpstate = data.Substring(0, 4);
                }
                //控制阀门状态
                else if (reportType.Equals("4D"))
                {
                    valvestate = data.Substring(0, 4);
                }
                //控制闸门状态
                else if (reportType.Equals("4E"))
                {
                    gatestate = data.Substring(0, 4);
                }
                //水量定值控制
                else if (reportType.Equals("4F"))
                {
                    waterctrl = data.Substring(0, 2);
                }
                //查询事件记录
                else if (reportType.Equals("50"))
                {
                    history = data.Substring(0, 64);
                }
                //读取/修改基本配置
                else if (reportType.Equals("41") || reportType.Equals("40"))
                {
                    string[] endStrList = data.Split(' ');
                    for (int j = 0; j < endStrList.Length; j++)
                    {
                        switch (endStrList[j])
                        {
                            case "01":
                                sid = endStrList[++j];
                                break;
                            case "02":
                                yid = endStrList[++j];
                                break;
                            case "03":
                                password = endStrList[++j];
                                break;
                            case "04":
                                desinfo1 = endStrList[++j];
                                break;
                            case "05":
                                desinfo2 = endStrList[++j];
                                break;
                            case "06":
                                desinfo3 = endStrList[++j];
                                break;
                            case "07":
                                desinfo4 = endStrList[++j];
                                break;
                            case "08":
                                try
                                {
                                    channelset = Decimal.Parse(endStrList[++j]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                                break;
                            case "09":
                                try
                                {
                                    workmode = Decimal.Parse(endStrList[++j]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                                break;
                            case "0A":
                                collectelements = endStrList[++j];
                                break;
                            case "0B":
                                addressRange = endStrList[++j];
                                break;
                            case "0C":
                                idnum = endStrList[++j];
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine("规约协议遥测站基本配置读取错误");
                                break;
                        }
                    }
                }
                //设置/修改运行参数
                else if (reportType.Equals("42") || reportType.Equals("43"))
                {
                    string[] endStrList = data.Split(' ');
                    int pnum;
                    for (int j = 0; j < endStrList.Length - 1; j++)
                    {
                        try
                        {
                            pnum = Convert.ToInt32(endStrList[j]);
                            oparameters[pnum] = Decimal.Parse(endStrList[j + 1]);
                            j++;
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                        }
                    }
                }
                //else
                //{
                //    dataTime = new DateTime(
                //                                    year: int.Parse("20" + data.Substring(0, 2)),
                //                                    month: int.Parse(data.Substring(2, 2)),
                //                                    day: int.Parse(data.Substring(4, 2)),
                //                                    hour: int.Parse(data.Substring(6, 2)),
                //                                    minute: int.Parse(data.Substring(8, 2)),
                //                                    second: 0
                //                                );
                //}
                datas.Add(downData);
                #endregion

                #region 处理上行报文
                if (reportType.Equals("30") || reportType.Equals("31") ||  reportType.Equals("32") 
                    || reportType.Equals("33") || reportType.Equals("34"))
                {
                    string stationTypeString = data.Substring(0, 1);
                    if (stationTypeString == "H")
                    {
                        type = EStationType.EH;
                    }
                    else if (stationTypeString == "P")
                    {
                        type = EStationType.ERainFall;
                    }
                    else if (stationTypeString == "K")
                    {
                        type = EStationType.RE;
                    }
                    else if (stationTypeString == "Z")
                    {
                        type = EStationType.GT;
                    }
                    else if (stationTypeString == "D")
                    {
                        type = EStationType.RP;
                    }
                    else
                    {
                        type = EStationType.EHydrology;
                    }
                    //删除站点类型
                    data = data.Substring(1);

                    if (data.Length >= 10)
                    {
                        string[] dataArr = Regex.Split(data, "TT", RegexOptions.IgnoreCase);
                        for (int i = 0; i < dataArr.Length; i++)
                        {
                            string oneGram = dataArr[i].Trim();
                            List<decimal> rainList = new List<decimal>();
                            List<decimal> waterList = new List<decimal>();
                            if (oneGram.Length < 10)
                            {
                                continue;
                            }


                            //观测时间引导符

                            /*if (oneGram.Contains("RGZS"))
                           {
                               int index = oneGram.IndexOf("RGZS");
                               //string RGZSstr = oneGram.Substring(index+4,).Trim();

                           }*/
                            //交流A相电压
                            if (oneGram.Contains("VTA"))
                            {
                                int index = oneGram.IndexOf("VTA");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                try
                                {
                                    vta = Decimal.Parse(endStrList[1]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //交流B相电压
                            if (oneGram.Contains("VTB"))
                            {
                                int index = oneGram.IndexOf("VTB");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                try
                                {
                                    vtb = Decimal.Parse(endStrList[1]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //交流C相电压
                            if (oneGram.Contains("VTC"))
                            {
                                int index = oneGram.IndexOf("VTC");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                try
                                {
                                    vtc = Decimal.Parse(endStrList[1]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //交流A相电流
                            if (oneGram.Contains("VIA"))
                            {
                                int index = oneGram.IndexOf("VIA");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                try
                                {
                                    via = Decimal.Parse(endStrList[1]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //交流B相电流
                            if (oneGram.Contains("VIB"))
                            {
                                int index = oneGram.IndexOf("VIB");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                try
                                {
                                    vib = Decimal.Parse(endStrList[1]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //交流B相电流
                            if (oneGram.Contains("VIC"))
                            {
                                int index = oneGram.IndexOf("VIC");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                try
                                {
                                    vic = Decimal.Parse(endStrList[1]);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }

                            //图片信息
                            if (oneGram.Contains("PIC"))
                            {
                                int index = oneGram.IndexOf("PIC");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                picture = endStrList[1];
                            }

                            if (oneGram.Contains("DRP"))
                            {
                                int index = oneGram.IndexOf("DRP");
                                string rainListStr = oneGram.Substring(index + 4, 24).Trim();
                                for (int j = 0; j < 12; j++)
                                {
                                    rainList.Add((System.Int32.Parse(rainListStr.Substring(j * 2, 2), System.Globalization.NumberStyles.HexNumber)) * (decimal)(0.1));
                                }

                            }
                            if (oneGram.Contains("DRZ"))
                            {

                            }
                            if (oneGram.Contains("DRZ1"))
                            {
                                int index = oneGram.IndexOf("DRZ1");
                                string waterListStr = oneGram.Substring(index + 5, 48).Trim();
                                for (int j = 0; j < 12; j++)
                                {
                                    waterList.Add((System.Int32.Parse(waterListStr.Substring(j * 4, 4), System.Globalization.NumberStyles.HexNumber)) * (decimal)0.01);
                                }
                            }
                            if (oneGram.Contains("DATA"))
                            {
                                //int index = oneGram.IndexOf("DATA");                       
                            }
                            //断面面积
                            if (oneGram.Contains("AC"))
                            {
                                int index = oneGram.IndexOf("AC");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string sectionAreaString = endStrList[1];
                                try
                                {
                                    sectionArea = Decimal.Parse(sectionAreaString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //瞬时气温
                            if (oneGram.Contains("AI"))
                            {
                                int index = oneGram.IndexOf("AI");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string airTempString = endStrList[1];
                                try
                                {
                                    airTemp = Decimal.Parse(airTempString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //瞬时水温
                            if (oneGram.Contains("C "))
                            {
                                int index = oneGram.IndexOf("C ");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string waterTempString = endStrList[1];
                                try
                                {
                                    waterTemp = Decimal.Parse(waterTempString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            if (oneGram.Contains("DRD"))
                            {
                                int index = oneGram.IndexOf("DRD");
                                string stepNumStr = oneGram.Substring(index + 4, 2).Trim();
                                try
                                {
                                    stepnumD = Decimal.Parse(stepNumStr);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            if (oneGram.Contains("DRH"))
                            {
                                int index = oneGram.IndexOf("DRH");
                                string stepNumStr = oneGram.Substring(index + 4, 2).Trim();
                                try
                                {
                                    stepnumH = Decimal.Parse(stepNumStr);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            if (oneGram.Contains("DRN"))
                            {
                                int index = oneGram.IndexOf("DRN");
                                string stepNumStr = oneGram.Substring(index + 4, 2).Trim();
                                try
                                {
                                    stepnumN = Decimal.Parse(stepNumStr);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            /*//时段长,降水、引排水、抽水历时
                            if (oneGram.Contains("DT"))
                            {
                                int index = oneGram.IndexOf("DT");
                                string periodStr = oneGram.Substring(index+3, 2).Trim();


                            }*/
                            //日蒸发量
                            if (oneGram.Contains("ED"))
                            {
                                int index = oneGram.IndexOf("ED");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string evaporationDayString = endStrList[1];
                                try
                                {
                                    evaporationDay = Decimal.Parse(evaporationDayString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //当前蒸发
                            if (oneGram.Contains("EJ"))
                            {
                                int index = oneGram.IndexOf("EJ");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string pevaporationString = endStrList[1];
                                try
                                {
                                    pevaporation = Decimal.Parse(pevaporationString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //气压
                            if (oneGram.Contains("FL"))
                            {
                                int index = oneGram.IndexOf("FL");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string airpressureString = endStrList[1];
                                try
                                {
                                    airpressure = Decimal.Parse(airpressureString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //闸坝、水库闸门开启高度
                            if (oneGram.Contains("GH"))
                            {
                                int index = oneGram.IndexOf("GH");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string openheightString = endStrList[1];
                                try
                                {
                                    openheight = Decimal.Parse(openheightString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //输水设备、闸门(组)编号
                            if (oneGram.Contains("GN"))
                            {
                                int index = oneGram.IndexOf("GN");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string waterequipNumString = endStrList[1];
                                try
                                {
                                    waterequipNum = Decimal.Parse(waterequipNumString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //输水设备类别
                            if (oneGram.Contains("GS"))
                            {
                                int index = oneGram.IndexOf("GS");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string waterequipTypeString = endStrList[1];
                                try
                                {
                                    waterequipType = Decimal.Parse(waterequipTypeString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //水库、闸坝闸门开启孔数
                            if (oneGram.Contains("GT "))
                            {
                                int index = oneGram.IndexOf("GT ");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string holenumString = endStrList[1];
                                try
                                {
                                    holenum = Decimal.Parse(holenumString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //地温
                            if (oneGram.Contains("GTP"))
                            {
                                int index = oneGram.IndexOf("GTP");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string groundtempString = endStrList[1];
                                try
                                {
                                    groundtemp = Decimal.Parse(groundtempString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //地下水瞬时埋深
                            if (oneGram.Contains("H "))
                            {
                                int index = oneGram.IndexOf("H ");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string groundwaterdepthString = endStrList[1];
                                try
                                {
                                    groundwaterdepth = Decimal.Parse(groundwaterdepthString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //波浪高度
                            if (oneGram.Contains("HW"))
                            {
                                int index = oneGram.IndexOf("HW");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string waveheightString = endStrList[1];
                                try
                                {
                                    waveheight = Decimal.Parse(waveheightString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //10 厘米处土壤含水量
                            if (oneGram.Contains("M10"))
                            {
                                int index = oneGram.IndexOf("M10");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum10String = endStrList[1];
                                try
                                {
                                    soilhum10 = Decimal.Parse(soilhum10String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //20 厘米处土壤含水量
                            if (oneGram.Contains("M20"))
                            {
                                int index = oneGram.IndexOf("M20");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum20String = endStrList[1];
                                try
                                {
                                    soilhum20 = Decimal.Parse(soilhum20String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //30 厘米处土壤含水量
                            if (oneGram.Contains("M30"))
                            {
                                int index = oneGram.IndexOf("M30");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum30String = endStrList[1];
                                try
                                {
                                    soilhum30 = Decimal.Parse(soilhum30String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //40 厘米处土壤含水量
                            if (oneGram.Contains("M40"))
                            {
                                int index = oneGram.IndexOf("M40");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum40String = endStrList[1];
                                try
                                {
                                    soilhum40 = Decimal.Parse(soilhum40String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //50 厘米处土壤含水量
                            if (oneGram.Contains("M50"))
                            {
                                int index = oneGram.IndexOf("M50");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum50String = endStrList[1];
                                try
                                {
                                    soilhum50 = Decimal.Parse(soilhum50String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //60 厘米处土壤含水量
                            if (oneGram.Contains("M60"))
                            {
                                int index = oneGram.IndexOf("M60");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum60String = endStrList[1];
                                try
                                {
                                    soilhum60 = Decimal.Parse(soilhum60String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //80 厘米处土壤含水量
                            if (oneGram.Contains("M80"))
                            {
                                int index = oneGram.IndexOf("M80");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum80String = endStrList[1];
                                try
                                {
                                    soilhum80 = Decimal.Parse(soilhum80String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //100 厘米处土壤含水量
                            if (oneGram.Contains("M100"))
                            {
                                int index = oneGram.IndexOf("M100");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string soilhum100String = endStrList[1];
                                try
                                {
                                    soilhum100 = Decimal.Parse(soilhum100String);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //湿度
                            if (oneGram.Contains("MST"))
                            {
                                int index = oneGram.IndexOf("MST");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string humidityString = endStrList[1];
                                try
                                {
                                    humidity = Decimal.Parse(humidityString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //1 小时时段降水量
                            if (oneGram.Contains("P1"))
                            {
                                int index = oneGram.IndexOf("P1");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string rain1hourString = endStrList[1];
                                try
                                {
                                    rain1hour = Decimal.Parse(rain1hourString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //2 小时时段降水量
                            if (oneGram.Contains("P2"))
                            {
                                int index = oneGram.IndexOf("P2");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string rain2hourString = endStrList[1];
                                try
                                {
                                    rain2hour = Decimal.Parse(rain2hourString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //3 小时时段降水量
                            if (oneGram.Contains("P3"))
                            {
                                int index = oneGram.IndexOf("P3");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string rain3hourString = endStrList[1];
                                try
                                {
                                    rain3hour = Decimal.Parse(rain3hourString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //6 小时时段降水量
                            if (oneGram.Contains("P6"))
                            {
                                int index = oneGram.IndexOf("P6");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string rain6hourString = endStrList[1];
                                try
                                {
                                    rain6hour = Decimal.Parse(rain6hourString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //12 小时时段降水量
                            if (oneGram.Contains("P12"))
                            {
                                int index = oneGram.IndexOf("P12");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string rain12hourString = endStrList[1];
                                try
                                {
                                    rain12hour = Decimal.Parse(rain12hourString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //日降水量
                            if (oneGram.Contains("PD"))
                            {
                                int index = oneGram.IndexOf("PD");
                                string dayrainStr = oneGram.Substring(index).Trim();
                                string[] dayrainStrList = dayrainStr.Split(' ');
                                string dayrainString = dayrainStrList[1];
                                try
                                {
                                    dayrain = Decimal.Parse(dayrainString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //当前降水量
                            if (oneGram.Contains("PJ"))
                            {
                                int index = oneGram.IndexOf("PJ");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string diffRainString = endStrList[1];
                                try
                                {
                                    currentRain = Decimal.Parse(diffRainString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //1分钟时段降水量
                            if (oneGram.Contains("PN01"))
                            {

                            }
                            //5分钟时段降水量
                            if (oneGram.Contains("PN05"))
                            {

                            }
                            //10分钟时段降水量
                            if (oneGram.Contains("PN10"))
                            {

                            }
                            //30分钟时段降水量
                            if (oneGram.Contains("PN30"))
                            {

                            }
                            //暴雨量
                            if (oneGram.Contains("PR"))
                            {

                            }
                            if (oneGram.Contains("PT"))
                            {
                                int index = oneGram.IndexOf("PT");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string totalRainString = endStrList[1];
                                try
                                {
                                    totalRain = Decimal.Parse(totalRainString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //瞬时河道水位、潮位
                            if (oneGram.Contains("Z "))
                            {
                                int index = oneGram.IndexOf("Z ");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string waterStageString = endStrList[1];
                                try
                                {
                                    waterStage = Decimal.Parse(waterStageString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //电压
                            if (oneGram.Contains("VT"))
                            {
                                int index = oneGram.IndexOf("VT");
                                string endStr = oneGram.Substring(index).Trim();
                                string[] endStrList = endStr.Split(' ');
                                string voltageString = endStrList[1];
                                try
                                {
                                    voltage = Decimal.Parse(voltageString);
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                                }
                            }
                            //定时报和均匀时段报处理
                            // 雨量组 和 水位组 均有数据 且数量相等。
                            if (rainList != null && rainList.Count > 0 && waterList != null && rainList.Count == waterList.Count)
                            {
                                CReportData oneData = new CReportData();
                                for (int k = 0; k < rainList.Count; k++)
                                {
                                    DateTime tmpDate = dataTime.AddMinutes(5 * k);
                                    oneData.Time = tmpDate;
                                    oneData.PeriodRain = rainList[k];
                                    oneData.Water = waterList[k];
                                    datas.Add(DeepClone(oneData));

                                }
                            }
                            // 雨量组数据 和 水位组数据不匹配
                            else
                            {
                                //
                                if (rainList != null && rainList.Count > 0)
                                {
                                    CReportData oneData = new CReportData();
                                    for (int k = 0; k < rainList.Count; k++)
                                    {
                                        DateTime tmpDate = dataTime.AddMinutes(5 * k);
                                        oneData.Time = tmpDate;
                                        oneData.PeriodRain = rainList[k];
                                        datas.Add(DeepClone(oneData));
                                    }
                                }

                                if (waterList != null && waterList.Count > 0)
                                {
                                    CReportData oneData = new CReportData();
                                    for (int k = 0; k < waterList.Count; k++)
                                    {
                                        DateTime tmpDate = dataTime.AddMinutes(5 * k);
                                        oneData.Time = tmpDate;
                                        oneData.Water = waterList[k];
                                        datas.Add(DeepClone(oneData));
                                    }

                                }
                            }
                            //普通报文处理
                            if (currentRain != null || totalRain != null || voltage != null || waterStage != null)
                            {
                                CReportData oneData = new CReportData();
                                oneData.Time = dataTime;
                                oneData.Rain = totalRain;
                                oneData.CurrentRain = currentRain;
                                oneData.Water = waterStage;
                                oneData.Voltge = voltage;
                                datas.Add(DeepClone(oneData));
                            }
                        }
                    }
                }
                #endregion
                foreach (var d in datas)
                {
                    if (!d.Rain.HasValue)
                    {
                        d.Rain = -1;
                    }
                    if (!d.Water.HasValue)
                    {
                        d.Water = -20000;
                    }if(d.Voltge <= 0)
                    {
                        d.Voltge = -20;
                    }
                }

                report = new CReportStruct
                {
                    StationType = type,
                    Datas = datas
                };
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("数据：" + msg);
                System.Diagnostics.Debug.WriteLine("规约协议正文解析错误" + e.Message);
                return false;
            }
        }

        public bool Parse_1(string msg, out CReportStruct report)
        {
            throw new NotImplementedException();
        }

        public bool Parse_2(string msg, out CReportStruct report)
        {
            throw new NotImplementedException();
        }

        #region 帮助方法
        private  T DeepClone<T>(T obj)
        {
            T ret = default(T);
            if (obj != null)
            {
                XmlSerializer cloner = new XmlSerializer(typeof(T));
                MemoryStream stream = new MemoryStream();
                cloner.Serialize(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                ret = (T)cloner.Deserialize(stream);
            }
            return ret;
        }
        #endregion

    }
}

