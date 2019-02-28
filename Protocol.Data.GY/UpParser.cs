using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydrology.Entity;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.IO;

//namespace Protocol.Data.GY
//{
//    class UpParser : IUp
//    {
//        public bool Parse(string msg, out CReportStruct report)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Parse_1(string msg, out CReportStruct report)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Parse_2(string msg, out CReportStruct report)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Parse_beidou(string sid, EMessageType type, string msg, out CReportStruct upReport)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}


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
                // 数据格式
                ////7E7E 已经在通讯协议刨去了
                //00
                //0060405210
                //04D2
                //31
                //0045
                //02
                //534E
                //180410000000
                //F1
                //28
                //0060405210
                //48
                //F0
                //28
                //1804092300
                //04
                //18
                //000005
                //26
                //19
                //041130 041130 041130 041130 041130 041130 041130 041130 041130 041130 041130 041130 041130 
                //03
                //0A13
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
                report.Type = "1G";
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
                string stationTypeString = data.Substring(0, 1);
                if (stationTypeString == "H")
                {
                    type = EStationType.EH;
                }
                if (stationTypeString == "P")
                {
                    type = EStationType.ERainFall;
                }
                //删除站点类型
                data = data.Substring(1);

                // report中的数据初始化
                List<CReportData> datas = new List<CReportData>();
                TimeSpan span = new TimeSpan(0);
                
                DateTime dataTime = sendTime;
                //int dataAccuracyLength;
                //int dataLength;
                //Decimal dataAccuracy;
                //Decimal dayRain;

                Nullable<Decimal> diffRain = null;
                Nullable<Decimal> totalRain = null;
                Nullable<Decimal> waterStage = null;
                Nullable<Decimal> voltage = null;
                //string dataDefine, dataDefine1, dataDefine2;
                //int flag = 0;
                if(data.Length >= 10)
                {
                    string[] dataArr =  Regex.Split(data, "TT", RegexOptions.IgnoreCase);
                    for(int i = 0; i < dataArr.Length; i++)
                    {
                        string oneGram = dataArr[i].Trim();
                        List<decimal> rainList = new List<decimal>();
                        List<decimal> waterList = new List<decimal>();
                        if (oneGram.Length < 10)
                        {
                            continue;
                        }
                        dataTime = new DateTime(
                                year: int.Parse("20" + oneGram.Substring(0, 2)),
                                month: int.Parse(oneGram.Substring(2, 2)),
                                day: int.Parse(oneGram.Substring(4, 2)),
                                hour: int.Parse(oneGram.Substring(6, 2)),
                                minute: int.Parse(oneGram.Substring(8, 2)),
                                second: 0
                            );
                        //观测时间引导符
                        if (oneGram.Contains("TT"))
                        {

                        }
                        if (oneGram.Contains("RGZS"))
                        {

                        }
                        if (oneGram.Contains("PIC"))
                        {

                        }
                        if (oneGram.Contains("DRP"))
                        {
                            int index = oneGram.IndexOf("DRP");
                            string rainListStr = oneGram.Substring(index+4, 24).Trim();
                            for(int j = 0; j < 12; j++)
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
                            string waterListStr = oneGram.Substring(index+5, 48).Trim();
                            for(int j =0;j < 12; j++)
                            {
                                waterList.Add((System.Int32.Parse(waterListStr.Substring(j * 4, 4), System.Globalization.NumberStyles.HexNumber)) * (decimal)0.01);
                            }
                        }
                        if (oneGram.Contains("DATA"))
                        {

                        }
                        if (oneGram.Contains("AC"))
                        {

                        }
                        if (oneGram.Contains("AI"))
                        {

                        }
                        if (oneGram.Contains("C"))
                        {

                        }
                        if (oneGram.Contains("DRxnn"))
                        {

                        }
                        if (oneGram.Contains("DT"))
                        {

                        }
                        if (oneGram.Contains("ED"))
                        {

                        }
                        if (oneGram.Contains("EJ"))
                        {

                        }
                        if (oneGram.Contains("FL"))
                        {

                        }
                        if (oneGram.Contains("GH"))
                        {

                        }
                        if (oneGram.Contains("GN"))
                        {

                        }
                        if (oneGram.Contains("GS"))
                        {

                        }
                        if (oneGram.Contains("GT"))
                        {

                        }
                        if (oneGram.Contains("GTP"))
                        {

                        }
                        if (oneGram.Contains("H"))
                        {

                        }
                        if (oneGram.Contains("HW"))
                        {

                        }
                        if (oneGram.Contains("M10"))
                        {

                        }
                        if (oneGram.Contains("M20"))
                        {

                        }
                        if(oneGram.Contains("M30"))
                        {

                        }
                        if(oneGram.Contains("M40"))
                        {

                        }
                        if (oneGram.Contains("M50"))
                        {

                        }
                        if (oneGram.Contains("M60"))
                        {

                        }
                        if (oneGram.Contains("M80"))
                        {

                        }
                        if (oneGram.Contains("M100"))
                        {

                        }
                        if (oneGram.Contains("MST"))
                        {

                        }
                        if (oneGram.Contains("P1"))
                        {

                        }
                        if (oneGram.Contains("P2"))
                        {

                        }
                        if (oneGram.Contains("P3"))
                        {

                        }
                        if (oneGram.Contains("P6"))
                        {

                        }
                        if (oneGram.Contains("P12"))
                        {

                        }
                        //日降水量
                        if (oneGram.Contains("PD"))
                        {

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
                                diffRain = Decimal.Parse(diffRainString);
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
                        if(rainList != null && rainList.Count > 0 && waterList != null && rainList.Count == waterList.Count)
                        {
                            CReportData oneData = new CReportData();
                            for(int k = 0; k < rainList.Count; k++)
                            {
                                DateTime tmpDate = dataTime.AddMinutes(5 * k);
                                oneData.Time = tmpDate;
                                oneData.DiffRain = rainList[k];
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
                                        oneData.DiffRain = rainList[k];
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
                        if (diffRain != null || totalRain != null ||voltage != null)
                        {
                            CReportData oneData = new CReportData();
                            oneData.Time = dataTime;
                            oneData.Rain = totalRain;
                            oneData.DiffRain = diffRain;
                            oneData.Water = waterStage;
                            oneData.Voltge = voltage;
                            datas.Add(DeepClone(oneData));
                        }
                    }
                }
                #region 废码代码
                //while (data.Length >= 2)
                //{
                //    // 截取要素标识符
                //    string sign = data.Substring(0, 2);
                //    flag = flag + 1;
                //    if(flag > 100)
                //    {
                //        break;
                //    }
                //    // 根据要素标识符取数据
                //    switch (sign)
                //    {
                //        case "ST":
                //            // 丢弃标识符
                //            data = data.Substring(2);
                //            // 丢弃一个字节
                //            data = data.Substring(1);
                //            // 丢弃一个测站地址
                //            data = data.Substring(10);
                //            // 丢弃一个字节
                //            data = data.Substring(1);

                //            // 遥测站分类码，不确定是不是一定在这个后面
                //            string stationTypeString = data.Substring(0,1);
                //            if(stationTypeString == "H")
                //            {
                //                type = EStationType.EH;
                //            }
                //            if(stationTypeString == "P")
                //            {
                //                type = EStationType.ERainFall;
                //            }
                //            data = data.Substring(1);
                //            //type = stationTypeString == "50" ? EStationType.ERainFall : EStationType.EHydrology;
                //            data = data.Substring(1);
                //            break;
                //        case "TT":
                //            // 丢弃标识符
                //            data = data.Substring(2);
                //            //丢弃一个字节
                //            data = data.Substring(1);
                //            dataTime = new DateTime(
                //                year: int.Parse("20" + data.Substring(0, 2)),
                //                month: int.Parse(data.Substring(2, 2)),
                //                day: int.Parse(data.Substring(4, 2)),
                //                hour: int.Parse(data.Substring(6, 2)),
                //                minute: int.Parse(data.Substring(8, 2)),
                //                second: 0
                //            );
                //            data = data.Substring(10);
                //            //丢弃一个字节
                //            data = data.Substring(1);
                //            break;
                //        case "PD":
                //            // 丢弃标识符
                //            data = data.Substring(2);
                //            // 丢弃数据定义19 3个字节、精度1
                //            dataDefine1 = Convert.ToString(int.Parse(data.Substring(0, 1)), 2);
                //            dataDefine2 = Convert.ToString(int.Parse(data.Substring(1, 1)), 2);
                //            dataDefine = dataDefine1.PadLeft(4, '0') + dataDefine2.PadLeft(4, '0');
                //            dataLength = Convert.ToInt32(dataDefine.Substring(0, 5), 2) * 2;
                //            dataAccuracyLength = Convert.ToInt32(dataDefine.Substring(5, 3), 2);
                //            dataAccuracy = 1;
                //            while (dataAccuracyLength > 0)
                //            {
                //                dataAccuracy *= (decimal)0.1;
                //                dataAccuracyLength--;
                //            }
                //            data = data.Substring(2);

                //            string dayRainString = data.Substring(0, dataLength);
                //            data = data.Substring(dataLength);
                //            try
                //            {
                //                dayRain = Decimal.Parse(dayRainString) * dataAccuracy;
                //            }
                //            catch (Exception e)
                //            {
                //                System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //            }
                //            break;
                //        case "PJ":
                //            // 丢弃标识符
                //            data = data.Substring(2);
                //            // 丢掉一个字节
                //            data = data.Substring(1);
                //            string[] rainArr = data.Split('.');
                //            int rainLen = rainArr[0].Length;

                //            string rainStr = data.Substring(0,rainLen + 2);
                //            data = data.Substring(rainLen + 2);
                //            data = data.Substring(1);
                //            // 丢弃数据定义19 3个字节、精度1



                //            string diffRainString = rainStr;

                //            try
                //            {
                //                diffRain = Decimal.Parse(diffRainString);
                //            }
                //            catch (Exception e)
                //            {
                //                System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //            }
                //            break;
                //        case "PT":
                //            // 丢弃标识符
                //            data = data.Substring(2);
                //            // 丢掉一个字节
                //            data = data.Substring(1);

                //            string[] dataArr = data.Split('.');
                //            int tmp = dataArr[0].Length;
                //            string totalRain = data.Substring(0,tmp + 2);

                //            data = data.Substring(tmp + 2);
                //            data = data.Substring(1);

                //            // 根据长度精度解析数据
                //            if (reportType == "31")
                //            {
                //                for (int i = 0; i < 13; i++)
                //                {
                //                    try
                //                    {
                //                        // 数据截取
                //                        string rainString = totalRain;

                //                        if (i != 0)
                //                        {
                //                            dataTime = dataTime + span;
                //                        }
                //                        Decimal rain = 0;
                //                        rain = Decimal.Parse(rainString);

                //                        // 数据封包
                //                        bool isExists = false;
                //                        if (datas.Count != 0)
                //                        {
                //                            foreach (var d in datas)
                //                            {
                //                                if (d.Time == dataTime)
                //                                {
                //                                    isExists = true;
                //                                    d.Rain = rain;
                //                                }
                //                            }
                //                        }
                //                        if (isExists == false)
                //                        {
                //                            CReportData reportData = new CReportData
                //                            {
                //                                Rain = rain,
                //                                Time = dataTime
                //                            };
                //                            datas.Add(reportData);
                //                        }

                //                    }
                //                    catch (Exception e)
                //                    {
                //                        System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //                    }
                //                }
                //            }
                //            else if (reportType == "32" || reportType == "33" || reportType == "30")
                //            {
                //                try
                //                {
                //                    // 数据截取
                //                    string rainString = totalRain;

                //                    Decimal rain = 0;
                //                    rain = Decimal.Parse(rainString);

                //                    // 数据封包
                //                    bool isExists = false;
                //                    if (datas.Count != 0)
                //                    {
                //                        foreach (var d in datas)
                //                        {
                //                            if (d.Time == dataTime)
                //                            {
                //                                isExists = true;
                //                                d.Rain = rain;
                //                            }
                //                        }
                //                    }
                //                    if (isExists == false)
                //                    {
                //                        CReportData reportData = new CReportData
                //                        {
                //                            Rain = rain,
                //                            Time = dataTime
                //                        };
                //                        datas.Add(reportData);
                //                    }

                //                }
                //                catch (Exception e)
                //                {
                //                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //                }
                //            }
                //            break;
                //        case "VT":
                //            // 丢弃标识符
                //            data = data.Substring(2);
                //            //丢掉一个位置
                //            data = data.Substring(1);
                //            // 丢弃数据定义?? ?个字节、精度？
                //            string[] voltageArr = data.Split('.');
                //            int voltageLen = voltageArr[0].Length;
                //            string voltageStr = data.Substring(0,voltageLen + 3);
                //            data = data.Substring(voltageLen + 3);
                //            data = data.Substring(1);

                //            // 根据长度精度解析数据
                //            if (reportType == "31")
                //            {
                //                for (int i = 0; i < 13; i++)
                //                {
                //                    try
                //                    {
                //                        // 数据截取
                //                        string voltageString = voltageStr;
                //                        if (i != 0)
                //                        {
                //                            dataTime = dataTime + span;
                //                        }
                //                        Decimal voltage = 0;
                //                        voltage = Decimal.Parse(voltageString);

                //                        // 数据封包
                //                        bool isExists = false;
                //                        if (datas.Count != 0)
                //                        {
                //                            foreach (var d in datas)
                //                            {
                //                                if (d.Time == dataTime)
                //                                {
                //                                    isExists = true;
                //                                    d.Voltge = voltage;
                //                                }
                //                            }
                //                        }
                //                        if (isExists == false)
                //                        {
                //                            CReportData reportData = new CReportData
                //                            {
                //                                Voltge = voltage,
                //                                Time = dataTime
                //                            };
                //                            datas.Add(reportData);
                //                        }
                //                    }
                //                    catch (Exception e)
                //                    {
                //                        System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //                    }
                //                }
                //            }
                //            else if (reportType == "32" || reportType == "33" ||reportType == "30")
                //            {
                //                try
                //                {
                //                    // 数据截取
                //                    string voltageString = voltageStr;

                //                    Decimal voltage = 0;
                //                    voltage = Decimal.Parse(voltageString);

                //                    // 数据封包
                //                    bool isExists = false;
                //                    if (datas.Count != 0)
                //                    {
                //                        foreach (var d in datas)
                //                        {
                //                            if (d.Time == dataTime)
                //                            {
                //                                isExists = true;
                //                                d.Voltge = voltage;
                //                            }
                //                        }
                //                    }
                //                    if (isExists == false)
                //                    {
                //                        CReportData reportData = new CReportData
                //                        {
                //                            Voltge = voltage,
                //                            Time = dataTime
                //                        };
                //                        datas.Add(reportData);
                //                    }
                //                }
                //                catch (Exception e)
                //                {
                //                    System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //                }
                //            }
                //            break;
                //        default:
                //            if (data.StartsWith("Z"))
                //            {
                //                // 丢弃标识符
                //                data = data.Substring(1);
                //                //丢掉一个字节
                //                data = data.Substring(1);
                //                string[] dataArr2 = data.Split('.');
                //                int tmp2 = dataArr2[0].Length;
                //                // 丢弃数据定义23 4个字节、精度3
                //                string waterStr = data.Substring(0,tmp2 + 4);
                //                data = data.Substring(tmp2 + 4);
                //                data = data.Substring(1);

                //                // 根据长度精度解析数据
                //                if (reportType == "31")
                //                {
                //                    for (int i = 0; i < 13; i++)
                //                    {
                //                        try
                //                        {
                //                            // 数据截取
                //                            string waterString = waterStr;
                //                            if (i != 0)
                //                            {
                //                                dataTime = dataTime + span;
                //                            }
                //                            Decimal water = 0;
                //                            water = Decimal.Parse(waterString);

                //                            // 数据封包
                //                            bool isExists = false;
                //                            if (datas.Count != 0)
                //                            {
                //                                foreach (var d in datas)
                //                                {
                //                                    if (d.Time == dataTime)
                //                                    {
                //                                        isExists = true;
                //                                        d.Water = water;
                //                                    }
                //                                }
                //                            }
                //                            if (isExists == false)
                //                            {
                //                                CReportData reportData = new CReportData
                //                                {
                //                                    Water = water,
                //                                    Time = dataTime
                //                                };
                //                                datas.Add(reportData);
                //                            }
                //                        }
                //                        catch (Exception e)
                //                        {
                //                            System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //                        }
                //                    }
                //                }
                //                else if (reportType == "32" || reportType == "33" || reportType == "30")
                //                {
                //                    try
                //                    {
                //                        // 数据截取
                //                        string waterString = waterStr;
                //                        Decimal water = 0;
                //                        water = Decimal.Parse(waterString);

                //                        // 数据封包
                //                        bool isExists = false;
                //                        if (datas.Count != 0)
                //                        {
                //                            foreach (var d in datas)
                //                            {
                //                                if (d.Time == dataTime)
                //                                {
                //                                    isExists = true;
                //                                    d.Water = water;
                //                                }
                //                            }
                //                        }
                //                        if (isExists == false)
                //                        {
                //                            CReportData reportData = new CReportData
                //                            {
                //                                Water = water,
                //                                Time = dataTime
                //                            };
                //                            datas.Add(reportData);
                //                        }
                //                    }
                //                    catch (Exception e)
                //                    {
                //                        System.Diagnostics.Debug.WriteLine("规约协议数据截取错误" + e.ToString());
                //                    }
                //                }
                //            }
                //            else if (data.StartsWith("DRxnn"))
                //            {
                //                // 丢弃标识符
                //                data = data.Substring(2);
                //                // 丢弃数据定义18 3个字节、精度0
                //                data = data.Substring(2);
                //                // 时间步长
                //                string timeSpanString = data.Substring(0, 6);
                //                data = data.Substring(6);
                //                TimeSpan timeSpan = new TimeSpan(Int32.Parse(timeSpanString.Substring(0, 2)), Int32.Parse(timeSpanString.Substring(2, 2)), Int32.Parse(timeSpanString.Substring(4, 2)), 0);
                //                span = span + timeSpan;
                //            }

                //            break;
                //    }
                //}
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

