using System;
using System.Diagnostics;
using Protocol.Data.Interface;

namespace Protocol.Data.Lib
{
    public class UpHelper
    {
        /// <summary>
        /// 解析站点数据内容
        /// 示例数据:090227080012345600011260
        /// 0902270800      10位数据时报
        /// 123456          6位水位
        /// 0001            4位雨量
        /// 1260            4位电压
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="stype">站类</param>
        /// <returns></returns>
        public static CReportData GetData(string data, string stype)
        {
            try
            {
                //  判断数据段长度是否为24
                Debug.Assert(data.Length == 24, "数据时报+水位+雨量+电压 的总长度为14");

                Debug.WriteLine(data);

                var result = new CReportData()
                {
                    Time = new DateTime(
                        year: Int32.Parse("20" + data.Substring(0, 2)), //  年
                        month: Int32.Parse(data.Substring(2, 2)),       //  月
                        day: Int32.Parse(data.Substring(4, 2)),         //  日
                        hour: Int32.Parse(data.Substring(6, 2)),        //  时
                        minute: Int32.Parse(data.Substring(8, 2)),      //  分
                        second: 0),                                     //  秒

                    Voltge = Int32.Parse(data.Substring(20, 4))         //  电压
                };

                int water = Int32.Parse(data.Substring(10, 6));         //  水位
                int rain = Int32.Parse(data.Substring(16, 4));          //  雨量


                //  雨量  站类为01        包含雨量Rain
                //  水文  站类为12，13    包含雨量Rain，水位Water
                //  水位  站类为02，03    包含水位Water
                switch (stype)
                {
                    case "01":  //  雨量
                        result.Rain = rain;
                        break;
                    case "12":  //  并行水文
                    case "13":  //  串行水文
                        result.Rain = rain;
                        result.Water = water;
                        break;
                    case "02":  //  并行水位
                    case "03":  //  串行水位
                        result.Water = water;
                        break;
                }

                return result;
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }
    }

}
