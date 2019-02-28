using System;
using System.Collections.Generic;
using System.Text;
using Hydrology.Entity;
using Protocol.Data.Interface;
using System.Diagnostics;

namespace Protocol.Data.Lib
{
    public class FlashBatchTrans : IFlashBatch
    {
        /// <summary>
        /// 按小时传Flash查询指令
        /// $00010K0103yymmddhhyymmddhh #13
        /// 01：01为水位 02为雨量
        /// 03为按小时传 02为按天传
        /// </summary>
        public String BuildQuery(string sid, EStationType stationType, ETrans trans, DateTime beginTime, DateTime endTime, EChannelType ctype)
        {
            
            //  构建发送指令的字符串
            StringBuilder sb = new StringBuilder();
            sb.Append(ProtocolMaps.ChannelProtocolStartCharMap.FindValue(ctype));
            sb.Append(String.Format("{0:D4}", Int32.Parse(sid.Trim())));
            sb.Append("0K");

            //   type : 01：01为水位 02为雨量      
            //      type : 01：01为雨量 02为水位      
            sb.Append(ProtocolHelpers.StationType2ProtoStr_1(stationType));

            //  dayOrHour : 03为按小时传 02为按天传
            //              按小时传  时间格式：   yyMMddHH
            //              按天传   时间格式：   yyMMdd
            sb.Append(ProtocolMaps.TransMap.FindValue(trans));
            switch (trans)
            {
                case ETrans.ByHour:
                    sb.Append(beginTime.ToString("yyMMddHH"));
                    sb.Append(endTime.ToString("yyMMddHH"));
                    break;
                case ETrans.ByDay:
                    sb.Append(beginTime.ToString("yyMMdd"));
                    sb.Append(endTime.ToString("yyMMdd"));
                    break;
                default:
                    throw new Exception("传输格式错误");
            }

            sb.Append('\r');
            return sb.ToString();
        }

        /// <summary>
        /// 按小时传遥测站回应
        ///     $00011K0103yymmddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456#13
        /// 测试数据
        ///     $70521K01031404022000001032022006001032022012001032022018001032022024001032022030001032022036001032022042001032022048001032022054001032022100001032022112001032022118001032022124001032022130001032022136001032\r
        /// 
        /// 按天传遥测站回应
        ///     $00011K0102yymmddhhmm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456#13
        /// 测试数据
        ///     $70521K010214040200000010320200060010320200120010320200180010320200240010320200300010320200360010320200420010320200480010320200540010320201000010320201060010320201120010320201180010320201240010320201300010320201360010320201420010320201480010320201540010320202000010320202060010320202120010320202180010320202240010320202300010320202360010320202420010320202480010320202540010320203000010320203060010320203120010320203180010320203240010320203300010320203360010320203420010320203480010320203540010320204000010320204060010320204120010320204180010320204240010320204300010320204360010320204420010320204480010320204540010320205000010320205060010320205120010320205180010320205240010320205300010320205360010320205420010320205480010320205540010320206000010320206060010320206120010320206180010320206240010320206300010320206360010320206420010320206480010320206540010320207000010320207060010320207120010320207180010320207240010320207300010320207360010320207420010320207480010320207540010320208000010320208060010320208120010320208180010320208240010320208300010320208360010320208420010320208480010320208540010320209000010320209060010320209120010320209180010320209240010320209300010320209360010320209420010320209480010320209540010320210000010320210060010320210120010320210180010320210240010320210300010320210360010320210420010320210480010320210540010320211000010320211060010320211120010320211180010320211240010320211300010320211360010320211420010320211480010320211540\r
        /// </summary>
        public bool Parse(String msg, out CBatchStruct batch)
        {
            batch = new CBatchStruct();
            try
            {
                string data = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(msg, out data))
                    return false;
                Debug.WriteLine(msg);
                //  解析站点ID ， 4位     0001
                batch.StationID = data.Substring(0, 4);
                //  解析命令指令 ，2位     1K 
                batch.Cmd = data.Substring(4, 2);
                //  解析站点类型， 2位     01 
                batch.StationType = ProtocolHelpers.ProtoStr2StationType(data.Substring(6, 2));

                //  解析传输类型， 2位      03
                batch.TransType = ProtocolMaps.TransMap.FindKey(data.Substring(8, 2));

                var datas = new List<CTimeAndData>();
                //  按小时传
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                int segLength = 0;
                string dataSegs = string.Empty;
                switch (batch.TransType)
                {
                    case ETrans.ByHour:
                        year = Int32.Parse("20" + data.Substring(10, 2));  //  年       yy
                        month = Int32.Parse(data.Substring(12, 2)); //  月       mm

                        //  截取日、小时、分钟和数据段 ，以\r结尾  
                        //  ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456#13
                        dataSegs = data.Substring(14);
                        segLength = 12;
                        while (dataSegs.Length >= segLength)
                        {
                            //  读取前8位数据
                            day = Int32.Parse(dataSegs.Substring(0, 2));   //  日       dd
                            hour = Int32.Parse(dataSegs.Substring(2, 2));  //  小时      hh
                            minute = Int32.Parse(dataSegs.Substring(4, 2));//  分钟   mm 
                            string value = dataSegs.Substring(6, 6);
                            datas.Add(new CTimeAndData()
                            {
                                Time = new DateTime(year, month, day, hour, minute, 0),
                                Data = value
                            });
                            //  取剩下的分钟和数据段
                            dataSegs = dataSegs.Substring(segLength);
                        }
                        break;
                    case ETrans.ByDay://  按天传
                        year = Int32.Parse("20" + data.Substring(10, 2));   //  年     yy
                        month = Int32.Parse(data.Substring(12, 2));         //  月     mm
                        DateTime dt = new DateTime();

                        //day = Int32.Parse(data.Substring(14, 2));        //  日     dd
                        //hour = Int32.Parse(data.Substring(16, 2));       //  小时   hh
                        //DateTime dt = new DateTime(year, month, day, hour, 0, second);
                        ////  截取分钟和数据段
                        ////  mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456#13
                        dataSegs = data.Substring(14);
                        segLength = 12;
                       // int previousMinute = 0;
                        //bool isFirst = true;
                        while (dataSegs.Length >= segLength)
                        {
                            day = Int32.Parse(dataSegs.Substring(0, 2));        //  日     dd
                            hour = Int32.Parse(dataSegs.Substring(2, 2));       //  小时   hh
                            minute = Int32.Parse(dataSegs.Substring(4, 2)); //  分钟   mm  
                            second = 0;
                            //  第一段数据
                            //if (isFirst)
                            //{
                            //    dt = new DateTime(year, month, day, hour, minute, second);
                            //}

                            //  不是第一段数据
                            dt = new DateTime(year, month, day, hour, minute, second);
                            //if (!isFirst)
                            //{
                            //    if (previousMinute >= minute)
                            //    {
                            //        dt = dt.AddHours(1);
                            //        //double addhour = 1;
                            //        //dt.AddHours(addhour);
                            //    }
                            //    dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, minute, dt.Second);
                            //}

                            //Int32 value = Int32.Parse(dataSegs.Substring(2, 6));//  数据   123456
                            string value = dataSegs.Substring(6, 6);
                            datas.Add(new CTimeAndData()
                            {
                                Time = dt,
                                Data = value
                            });

                            Debug.WriteLine(string.Format("{0:D2}  {1:D6}  ------ {2}   {3:D6}", minute, value, dt.ToString("yyyyMMdd HH:mm:ss"), value));

                            //  取剩下的日，小时，分钟和数据段
                            dataSegs = dataSegs.Substring(segLength);
                            //previousMinute = minute;
                            //isFirst = false;
                        }
                        break;
                }
                batch.Datas = datas;
                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("数据:" + msg);
                System.Diagnostics.Debug.WriteLine("Flash批量传输解析数据不完整！" + exp.Message);
            }
            return false;
        }
    }
}
