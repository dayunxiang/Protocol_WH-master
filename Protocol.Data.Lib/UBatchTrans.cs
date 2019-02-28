using System;
using System.Collections.Generic;
using System.Text;
using Hydrology.Entity;
using Protocol.Data.Interface;

namespace Protocol.Data.Lib
{
    /// <summary>
    /// U盘数据 
    /// 批量传输不允许出现跨天传输的情况
    /// 按天传输时标:yyMMdd88
    /// 小时传输时标:yyMMdd88
    /// </summary>
    public class UBatchTrans : IUBatch
    {
        //示例: $00010K01yymmddhh#13
        public String BuildQuery(string sid, EStationType stationType, ETrans trans, DateTime beginTime, EChannelType ctype)
        {
            //  拼接发送指令的字符串
            StringBuilder sb = new StringBuilder();
            sb.Append(ProtocolMaps.ChannelProtocolStartCharMap.FindValue(ctype));
            sb.Append(String.Format("{0:D4}", Int32.Parse(sid.Trim())));
            sb.Append("0K");
            //  type : 01：01为水位 02为雨量            
            sb.Append(ProtocolHelpers.StationType2ProtoStr(stationType));

            switch (trans)
            {
                case ETrans.ByHour:
                    sb.Append(beginTime.ToString("yyMMddHH"));
                    break;
                case ETrans.ByDay:
                    sb.Append(beginTime.ToString("yyMMdd"));
                    sb.Append("88");
                    break;
                default:
                    throw new Exception("传输格式错误");
            }
            sb.Append('\r');
            return sb.ToString();
        }

        /// <summary>
        /// 遥测站回应
        /// $00011K01yymmddhhmm000Dhhmm000D#13
        /// </summary>
        public bool Parse(String msg, out CBatchStruct batch)
        {
            batch = new CBatchStruct();
            try
            {
                string data = string.Empty;
                if (!ProtocolHelpers.DeleteSpecialChar(msg, out data))
                    return false;

                //  解析站点ID ， 4位     0001
                batch.StationID = data.Substring(0, 4);

                //  解析命令指令 ，2位     1K 
                batch.Cmd = data.Substring(4, 2);

                //  解析站点类型， 2位      01 
                batch.StationType = ProtocolHelpers.ProtoStr2StationType(data.Substring(6, 2));

                int year = Int32.Parse("20" + data.Substring(8, 2));    //  年       yy
                int month = Int32.Parse(data.Substring(10, 2));         //  月       mm
                int day = Int32.Parse(data.Substring(12, 2));           //  日       dd

                //  截取小时，分钟和数据段
                //  hhmm000Dhhmm000D
                var datas = new List<CTimeAndData>();
                int segLength = 10;
                string dataSegs = data.Substring(14);
                while (dataSegs.Length >= segLength)
                {
                    int hour = Int32.Parse(dataSegs.Substring(0, 2));   //  小时   hh
                    int minute = Int32.Parse(dataSegs.Substring(2, 2)); //  分钟   mm  

                    String hexStr = dataSegs.Substring(4, 4);            //  数据   000D,水位值要16转10，后2位没用
                                                                         // int value = Int32.Parse(hexStr, System.Globalization.NumberStyles.HexNumber);
                    string value = "22";
                    datas.Add(new CTimeAndData()
                    {
                        Time = new DateTime(year, month, day, hour, minute, 0),
                        Data = value
                    });

                    //  取剩下的小时，分钟和数据段
                    dataSegs = dataSegs.Substring(segLength);
                }
                batch.Datas = datas;
                return true;
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("数据:" + msg);
                System.Diagnostics.Debug.WriteLine("U盘批量传输解析数据不完整！" + exp.Message);
            }
            return false;
        }
    }
}
