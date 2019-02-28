using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Protocol.Data.Interface;

namespace Protocol.Data.Lib
{
    /// <summary>
    /// U盘数据 批量传输不允许出现跨天传输的情况
    /// </summary>
    public class UBatchTrans : IUBatch
    {
        //示例: $00010K01yymmddhh#13
        public String BuildQuery(string sid, string type, DateTime time, bool isHour)
        {
            //  拼接发送指令的字符串
            StringBuilder sb = new StringBuilder();
            sb.Append('$');
            sb.Append(String.Format("{0:D4}", Int32.Parse(sid)));
            sb.Append("0K");
            sb.Append(type);

            if (isHour)
            {
                sb.Append(time.ToString("yyMMddHH"));
            }
            else
            {
                sb.Append(time.ToString("yyMMdd"));
                sb.Append("88");
            }
            sb.Append('\r');
            return sb.ToString();
        }

        /// <summary>
        /// 遥测站回应
        /// $00011K01yymmddhhmm000Dhhmm000D#13
        /// </summary>
        public CBatchStruct Parse(String data)
        {
            //  解析站点ID ， 4位     0001
            string sid = data.Substring(1, 4);
            //  解析命令指令 ，2位     1K 
            string cmd = data.Substring(5, 2);
            Debug.Assert(cmd.Equals("1K"), "与说明文档内容不符");
            //  解析站点类型， 2位      01 
            string stationType = data.Substring(7, 2);

            int year = Int32.Parse("20" + data.Substring(9, 2));   //  年       yy
            int month = Int32.Parse(data.Substring(11, 2)); //  月       mm
            int day = Int32.Parse(data.Substring(13, 2));   //  日       dd

            var datas = new List<CTimeAndData>();

            //  截取小时，分钟和数据段，以\r结尾
            //  hhmm000Dhhmm000D#13
            string dataSegs = data.Substring(15);
            do
            {
                int hour = Int32.Parse(dataSegs.Substring(0, 2));   //  小时      hh
                int minute = Int32.Parse(dataSegs.Substring(2, 2)); //  分钟   mm  
                /////   怎样转?
                String value = dataSegs.Substring(4, 4);            //  数据   000D,水位值要16转10，后2位没用

                datas.Add(new CTimeAndData()
                {
                    Time = new DateTime(year, month, day, hour, minute, 0),
                    Data = value
                });

                //  取剩下的小时，分钟和数据段，以\r结尾
                dataSegs = dataSegs.Substring(10);
            }
            while (!dataSegs.Equals("\r"));

            return new CBatchStruct()
            {
                StationID = sid,
                Cmd = cmd,
                StationType = stationType,
                Datas = datas
            };
        }
    }

}
