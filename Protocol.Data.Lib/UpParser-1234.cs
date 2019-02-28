using System;
using System.Collections.Generic;
using Hydrology.Entity;
using Protocol.Data.Interface;

namespace Protocol.Data.Lib
{
    public class UpParser : IUp
    {
        public bool Parse(String msg, out CReportStruct report)
        {
            report = null;
            try
            {
                string data = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(msg, out data))
                    return false;

                //  解析站点ID
                String stationID = data.Substring(0, 4);
                //  解析通信类别
                String type = data.Substring(4, 2);
                //  解析报文类别
                EMessageType reportType = ProtocolMaps.MessageTypeMap.FindKey(data.Substring(6, 2));

                //  解析站点类别
                //EStationType stationType = EStationType.EHydrology;
                //String stationTypeStr = data.Substring(8, 2);
                //switch (stationTypeStr)
                //{
                //    case "01":  //  雨量
                //        stationType = EStationType.ERainFall;
                //        break;
                //    case "02":  //  水位
                //    case "12":
                //        stationType = EStationType.ERiverWater;
                //        break;
                //    case "03":  //  水文
                //    case "13":
                //        stationType = EStationType.EHydrology;
                //        break;
                //}
                EStationType stationType = ProtocolHelpers.ProtoStr2StationType(data.Substring(8, 2));

                //  解析接收时间
                DateTime recvTime = DateTime.Now;

                //  获取数据段，不包含站号、类别、报类、站类信息
                var lists = data.Substring(10).Split(CSpecialChars.BALNK_CHAR);
                var datas = GetData(lists, stationType);

                report = new CReportStruct()
                {
                    Stationid = stationID,
                    Type = type,
                    ReportType = reportType,
                    StationType = stationType,
                    RecvTime = recvTime,
                    Datas = datas
                };
                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("数据:" + msg);
                System.Diagnostics.Debug.WriteLine("上行指令解析数据不完整！" + exp.Message);
            }
            return false;
        }

        private List<CReportData> GetData(IList<string> dataSegs, EStationType stationType)
        {
            var result = new List<CReportData>();
            foreach (var item in dataSegs)
            {
                CReportData data = new CReportData();
                if (GetData(item, stationType, out data))
                    result.Add(data);
            }
            return result;
        }

        /// <summary>
        /// 解析站点数据内容
        /// 示例数据:090227080012345600011260
        /// 0902270800      10位数据时报
        /// 123456          6位水位
        /// 0001            4位雨量
        /// 1260            4位电压
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="stationType">站类</param>
        /// <param name="report"></param>
        /// <returns></returns>
        private bool GetData(string data, EStationType stationType, out CReportData report)
        {
            report = new CReportData();
            try
            {
                //  解析时间
                report.Time = new DateTime
                    (
                        year: Int32.Parse("20" + data.Substring(0, 2)), //  年
                        month: Int32.Parse(data.Substring(2, 2)),       //  月
                        day: Int32.Parse(data.Substring(4, 2)),         //  日
                        hour: Int32.Parse(data.Substring(6, 2)),        //  时
                        minute: Int32.Parse(data.Substring(8, 2)),      //  分
                        second: 0                                       //  秒
                    );
                //  解析电压  2(整数位) + 2(小数位)  单位V
                Decimal Voltge = Decimal.Parse(data.Substring(20, 4)) * (Decimal)0.01;
                //  解析水位  4(整数位) + 2(小数位)  单位m
                Decimal water = Decimal.Parse(data.Substring(10, 6)) * (Decimal)0.01;
                //  解析雨量                         单位mm，未乘以精度
                Decimal rain = Decimal.Parse(data.Substring(16, 4));

                //  初始化雨量，水位，电压值
                //  雨量  包含雨量Rain
                //  水文  包含雨量Rain，水位Water
                //  水位  包含水位Water
                switch (stationType)
                {
                    case EStationType.ERainFall:   //  雨量
                        report.Rain = rain; break;
                    case EStationType.EHydrology:  //  水文
                        report.Rain = rain;
                        report.Water = water;
                        break;
                    case EStationType.ERiverWater: //  水位
                        report.Water = water; break;
                    default: break;
                }
                report.Voltge = Voltge;

                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
            }
            return false;
        }


        public bool Parse(String msg, CReportArtificalFlow report)
        {
            throw new NotImplementedException();
        }

        public bool Parse(String msg, CReportArtificalWater report)
        {
            throw new NotImplementedException();
        }
    }
}
