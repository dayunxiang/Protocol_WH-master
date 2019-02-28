using System;
using System.Collections.Generic;
using Hydrology.Entity;
using Protocol.Data.Interface;
using System.Text.RegularExpressions;

namespace Protocol.Data.Lib
{
    public class UpParser : IUp
    {
        public bool Parse(String msg, out CReportStruct report)
        {
            //6013 $60131G2201161111040003046112271367
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
        /// 
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


                Decimal Voltge, rain, water;

                //  解析电压  2(整数位) + 2(小数位)  单位V
                try
                {
                    Voltge = Decimal.Parse(data.Substring(20, 4)) * (Decimal)0.01;
                }catch(Exception e1)
                {
                    Voltge = -1;
                }

                // cln:20141110修改，此处根据站点类型类判断是否需要解析相应的数据，避免因为非必要字段导致的异常信息
                //  解析水位  4(整数位) + 2(小数位)  单位m
                //Decimal water = Decimal.Parse(data.Substring(10, 6)) * (Decimal)0.01;
                //  解析雨量                         单位mm，未乘以精度
                //Decimal rain = Decimal.Parse(data.Substring(16, 4));

                //  初始化雨量，水位，电压值
                //  雨量  包含雨量Rain
                //  水文  包含雨量Rain，水位Water
                //  水位  包含水位Water
                switch (stationType)
                {
                    case EStationType.ERainFall:
                        {
                            //  雨量
                            //  解析雨量                         单位mm，未乘以精度
                            try
                            {
                                rain = Decimal.Parse(data.Substring(16, 4));
                            }catch (Exception e)
                            {
                                rain = -1;
                            }
                            report.Rain = rain;
                        } break;
                    case EStationType.EHydrology:
                        {
                            //  水文
                            //  解析雨量                         单位mm，未乘以精度
                            try
                            {
                                rain = Decimal.Parse(data.Substring(16, 4));
                            }
                            catch (Exception e)
                            {
                                rain = -1;
                            }
                            //  解析水位  4(整数位) + 2(小数位)  单位m
                            try
                            {
                                water = Decimal.Parse(data.Substring(10, 6)) * (Decimal)0.01;
                            }catch(Exception e)
                            {
                                water = -200;
                            }
                            report.Rain = rain;
                            report.Water = water;
                        }
                        break;
                    case EStationType.ERiverWater:
                        {
                            //  水位
                            //  解析水位  4(整数位) + 2(小数位)  单位m
                            try
                            {
                                water = Decimal.Parse(data.Substring(10, 6)) * (Decimal)0.01;
                            }
                            catch (Exception e)
                            {
                                water = -200;
                            }
                            report.Water = water;
                            break;
                        }
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

        public bool Parse_1(String msg, out CReportStruct report)
        {
            //
            //6013 $60131G21 11 0200 012345 04
            report = null;
            try
            {
                string data = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(msg, out data))
                    return false;

                //  解析站点ID
                String stationID = data.Substring(0, 4);
                //  解析通信类别
                String type = data.Substring(6, 2);
                if (type != "21")
                {
                    return false;
                }
                //  解析报文类别
                EMessageType reportType = ProtocolMaps.MessageTypeMap.FindKey(data.Substring(8, 2));
                //  解析接收时间
                DateTime recvTime = DateTime.Now;
                string hour = data.Substring(10, 2);
                string minute = data.Substring(12, 2);
                DateTime collectTine = new DateTime(recvTime.Year, recvTime.Month, recvTime.Day, int.Parse(hour), int.Parse(minute), 0);
                string water = data.Substring(14, 6);
                string WaterPotential = data.Substring(20, 2);
                //  获取数据段，不包含站号、类别、报类、站类信息
                report = new CReportStruct();
                report.RecvTime = recvTime;
                report.Stationid = stationID;
                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("数据:" + msg);
                System.Diagnostics.Debug.WriteLine("上行指令解析数据不完整！" + exp.Message);
            }
            return false;
        }

        public bool Parse_2(String msg, out CReportStruct report)
        {
            //
            //6013 $60131G23 030200 012345 4 1 123
            report = null;
            try
            {
                string data = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(msg, out data))
                    return false;

                //  解析站点ID
                String stationID = data.Substring(0, 4);
                //  解析通信类别
                String type = data.Substring(6, 2);
                if (type != "23")
                {
                    return false;
                }
                //  解析报文类别
                //EMessageType reportType = ProtocolMaps.MessageTypeMap.FindKey(data.Substring(8, 2));
                //  解析接收时间
                DateTime recvTime = DateTime.Now;
                string day = data.Substring(8, 2);
                string hour = data.Substring(10, 2);
                string minute = data.Substring(12, 2);
                DateTime collectTine = new DateTime(recvTime.Year, recvTime.Month,int.Parse(day), int.Parse(hour), int.Parse(minute), 0);
                string water = data.Substring(14, 6);
                string WaterPotential = data.Substring(20, 1);
                string acc = data.Substring(21, 1);
                string num = data.Substring(22, 3);
                string method = data.Substring(25, 1);
                //  获取数据段，不包含站号、类别、报类、站类信息
                report = new CReportStruct();
                report.RecvTime = recvTime;
                report.Stationid = stationID;
                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("数据:" + msg);
                System.Diagnostics.Debug.WriteLine("上行指令解析数据不完整！" + exp.Message);
            }
            return false;
        }
    }
}
