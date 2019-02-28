using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Protocol.Data.Interface;
using Hydrology.Entity;

namespace Protocol.Data.Lib
{
    public class SoilParser : ISoil
    {
        public String BuildQuery()
        {
            return string.Empty;
        }

        /// <summary>
        /// #S1209110500705141749 000.87001.06000.98000.76000.82
        /// #S1209110500705141755 000.67000.96000.90
        /// </summary>
        /// <param name="rawStr"></param>
        /// <param name="soil"></param>
        /// <returns></returns>
        /// 
        public bool Parse(string resp, out CEntitySoilData soil, out CReportStruct report)
        {
            soil = null;
            report = null;
            report = new CReportStruct();
            try
            {
                string rawStr = resp.Trim();

                if (rawStr.StartsWith("#S1"))
                {
                    soil = new CEntitySoilData();
                    //  终端机号
                    soil.StrDeviceNumber = rawStr.Substring(3, 8);

                    //  数据采集时间
                    int year = Int32.Parse("20" + rawStr.Substring(11, 2));
                    int month = Int32.Parse(rawStr.Substring(13, 2));
                    int day = Int32.Parse(rawStr.Substring(15, 2));
                    int hour = Int32.Parse(rawStr.Substring(17, 2));
                    int minute = Int32.Parse(rawStr.Substring(19, 2));
                    soil.DataTime = new DateTime(year, month, day, hour, minute, 0);

                    // #S1209110500705141749 000.87001.06000.98000.76000.82
                    if (rawStr.Length.Equals(22 + 30))
                    {
                        soil.Voltage10 = float.Parse(rawStr.Substring(22, 6));
                        soil.Voltage20 = float.Parse(rawStr.Substring(28, 6));
                        soil.Voltage30 = float.Parse(rawStr.Substring(34, 6));
                        soil.Voltage40 = float.Parse(rawStr.Substring(40, 6));
                        soil.Voltage60 = float.Parse(rawStr.Substring(46, 6));
                    }
                    // #S1209110500705141755 000.67000.96000.90
                    if (rawStr.Length.Equals(22 + 18))
                    {
                        soil.Voltage10 = float.Parse(rawStr.Substring(22, 6));
                        soil.Voltage20 = float.Parse(rawStr.Substring(28, 6));
                        soil.Voltage40 = float.Parse(rawStr.Substring(34, 6));
                    }
                    return true;
                }
                else if (rawStr.StartsWith("$") && "1G" == rawStr.Substring(5, 2))
                {
                    soil = new CEntitySoilData();
                    string StationID = rawStr.Substring(1, 4);
                    soil.StationID = StationID;
                    //  $70521G25142523453124
                    string cmd = rawStr.Substring(7, 2);
                    if ("25" == cmd)
                    {
                        var now = DateTime.Now;
                        soil.DataTime = new DateTime(now.Year,now.Month,now.Day,now.Hour,now.Minute,0);
                        //  $70521G25142523453124
                        if (21 == rawStr.Length)
                        {
                            soil.Voltage10 = float.Parse(rawStr.Substring(9, 4)) / 100;
                            soil.Voltage20 = float.Parse(rawStr.Substring(13, 4)) / 100;
                            soil.Voltage40 = float.Parse(rawStr.Substring(17, 4)) / 100;
                        }
                        else if (29 == rawStr.Length)//  $70521G2514252345312412345678
                        {
                            soil.Voltage10 = float.Parse(rawStr.Substring(9, 4)) / 100;
                            soil.Voltage20 = float.Parse(rawStr.Substring(13, 4)) / 100;
                            soil.Voltage30 = float.Parse(rawStr.Substring(17, 4)) / 100;
                            soil.Voltage40 = float.Parse(rawStr.Substring(21, 4)) / 100;
                            soil.Voltage60 = float.Parse(rawStr.Substring(25, 4)) / 100;
                        }
                        else
                        {
                            return false;
                        }
                        return true;
                    }
                    else if ("22" == cmd  || "21" == cmd)
                    {
                        //  $70521G2217yymmddhhmm12345600011256000100020003
                        EStationType stationType = EStationType.EHydrology;
                        decimal water = decimal.Parse(rawStr.Substring(21, 6)) / 100;
                        decimal rain = decimal.Parse(rawStr.Substring(27, 4));
                        decimal volegate = decimal.Parse(rawStr.Substring(31, 4)) / 100;
                        CReportData reportData = new CReportData();
                        switch (rawStr.Substring(9, 2))
                        {
                            case "04":
                                stationType = EStationType.ESoil;
                                if ("21" == cmd)
                                {
                                    report.ReportType = EMessageType.EAdditional;
                                }
                                if ("22" == cmd)
                                {
                                    report.ReportType = EMessageType.ETimed;
                                }
                                break;
                            case "05":
                                stationType = EStationType.ESoilRain;
                                reportData.Rain = rain;
                                reportData.Voltge = volegate;
                                break;
                            case "06":
                            case "16":
                                stationType = EStationType.ESoilWater;
                                reportData.Water = water;
                                reportData.Voltge = volegate;
                                break;
                            case "07":
                            case "17":
                                reportData.Rain = rain;
                                reportData.Water = water;
                                reportData.Voltge = volegate;
                                stationType = EStationType.ESoilHydrology;
                                break;
                            default: throw new Exception(); break;
                        }

                        int year = Int32.Parse("20" + rawStr.Substring(11, 2));
                        int month = Int32.Parse(rawStr.Substring(13, 2));
                        int day = Int32.Parse(rawStr.Substring(15, 2));
                        int hour = Int32.Parse(rawStr.Substring(17, 2));
                        int minute = Int32.Parse(rawStr.Substring(19, 2));
                        DateTime collectTime = new DateTime(year, month, day, hour, minute, 0);

                        soil.DataTime = collectTime;

                        //gm  20161030  下1行
                        soil.DVoltage = volegate;

                        if (47 == rawStr.Length)
                        {
                            //  $7052 1G2217 yymmddhhmm 123456 0001 1256 0001 0002 0003 
                            soil.Voltage10 = float.Parse(rawStr.Substring(35, 4)) / 100;
                            soil.Voltage20 = float.Parse(rawStr.Substring(39, 4)) / 100;
                            soil.Voltage40 = float.Parse(rawStr.Substring(43, 4)) / 100;

                          //  report = new CReportStruct();
                            report.Datas = new List<CReportData>();
                            report.Stationid = StationID;
                            report.StationType = stationType;
                            report.RecvTime = DateTime.Now;
                            reportData.Time = DateTime.Now;
                            report.Datas.Add(reportData);

                        }
                        else if (47 + 8 == rawStr.Length)
                        {
                            //  $70521G2217yymmddhhmm123456 0001 1256 0001 0002 0003 0004 0005
                            soil.Voltage10 = float.Parse(rawStr.Substring(35, 4)) / 100;
                            soil.Voltage20 = float.Parse(rawStr.Substring(39, 4)) / 100;
                            soil.Voltage30 = float.Parse(rawStr.Substring(43, 4)) / 100;
                            soil.Voltage40 = float.Parse(rawStr.Substring(47, 4)) / 100;
                            soil.Voltage60 = float.Parse(rawStr.Substring(51, 4)) / 100;

                            report = new CReportStruct();
                            report.Stationid = StationID;
                            report.StationType = stationType;
                            report.RecvTime = DateTime.Now;
                            reportData.Time = DateTime.Now;
                            report.Datas = new List<CReportData>();
                            report.Datas.Add(reportData);
                        }
                        else
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception exp) { }
            return false;
        }
    }
}
