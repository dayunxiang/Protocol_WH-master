using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Protocol.Data.Interface;

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
        public String BuildQuery(string sid, string type, bool isHour, DateTime time1, DateTime time2)
        {
            //  构建发送指令的字符串
            StringBuilder sb = new StringBuilder();
            sb.Append('$');
            sb.Append(String.Format("{0:D4}", Int32.Parse(sid)));
            sb.Append("0K");

            //  type : 01：01为水位 02为雨量            
            sb.Append(type);

            //  dayOrHour : 03为按小时传 02为按天传
            if (isHour)
            {
                //  按小时传
                //  time的格式为yymmddhh
                sb.Append("03");
                sb.Append(time1.ToString("yyMMddHH"));
                sb.Append(time2.ToString("yyMMddHH"));
            }
            else
            {
                //  按天传
                //  time的格式为yymmdd
                sb.Append("02");
                sb.Append(time1.ToString("yyMMdd"));
                sb.Append(time2.ToString("yyMMdd"));
            }
            sb.Append('\r');
            return sb.ToString();
        }

        /// <summary>
        /// 按小时传遥测站回应
        /// $00011K0103yymmddhhmm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456#13
        /// 按天传遥测站回应
        /// $00011K0102yymmddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456#13
        /// </summary>
        public CBatchStruct Parse(String data)
        {
            //  解析站点ID ， 4位     0001
            String sid = data.Substring(1, 4);
            //  解析命令指令 ，2位     1K 
            String cmd = data.Substring(5, 2);
            Debug.Assert(cmd.Equals("1K"), "与说明文档内容不符");
            //  解析站点类型， 2位      01 
            String stationType = data.Substring(7, 2);
            //  解析传输类型， 2位      03
            String transType = data.Substring(9, 2);

            var datas = new List<CTimeAndData>();

            if (transType.Equals("03")) //  按小时传
            {
                int year = Int32.Parse("20" + data.Substring(11, 2));  //  年       yy
                int month = Int32.Parse(data.Substring(13, 2)); //  月       mm
                int day = Int32.Parse(data.Substring(15, 2));   //  日       dd
                int hour = Int32.Parse(data.Substring(17, 2));  //  小时      hh

                //  截取分钟和数据段 ，以\r结尾  
                //  mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456mm123456
                string dataSegs = data.Substring(19);
                do
                {
                    //  读取前8位数据
                    int minute = Int32.Parse(dataSegs.Substring(0, 2));//  分钟   mm  
                    string value = dataSegs.Substring(2, 6);           //  数据   123456

                    datas.Add(new CTimeAndData()
                    {
                        Time = new DateTime(year, month, day, hour, minute, 0),
                        Data = value
                    });

                    //  取剩下的分钟和数据段，以\r结尾
                    dataSegs = dataSegs.Substring(0, 8);
                }
                while (!dataSegs.Equals("\r"));
            }
            else if (transType.Equals("02"))    //  按天传
            {

                int year = Int32.Parse("20" + data.Substring(11, 2)); //  年       yy
                int month = Int32.Parse(data.Substring(13, 2));//  月       mm

                //  截取日，小时，分钟和数据段，以\r结尾
                //  ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456ddhhmm123456
                string dataSegs = data.Substring(15);
                do
                {
                    //  读取前12位数据
                    int day = Int32.Parse(dataSegs.Substring(0, 2));    //  日       dd
                    int hour = Int32.Parse(dataSegs.Substring(2, 2));   //  小时      hh
                    int minute = Int32.Parse(dataSegs.Substring(4, 2)); //  分钟   mm  
                    string value = dataSegs.Substring(6, 6);            //  数据   123456

                    datas.Add(new CTimeAndData()
                    {
                        Time = new DateTime(year, month, day, hour, minute, 0),
                        Data = value
                    });

                    //  取剩下的日，小时，分钟和数据段，以\r结尾
                    dataSegs = dataSegs.Substring(0, 12);
                }
                while (!dataSegs.Equals("\r"));
            }

            return new CBatchStruct()
            {
                StationID = sid,
                StationType = stationType,
                TransType = transType,
                Cmd = cmd,
                Datas = datas
            };
        }

    }
}
