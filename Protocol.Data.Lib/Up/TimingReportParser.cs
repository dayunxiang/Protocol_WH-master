using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Protocol.Data.Interface;

namespace Protocol.Data.Lib
{
    /// <summary>
    /// 定时报解析类
    /// </summary>
    public class TimingReportParser : IUp
    {
        public CReportStruct Parse(String data)
        {
            //  判断是否是合法输入
            Debug.Assert(data.StartsWith(CSpecialChars.StartCh.ToString()), "数据以非'$'字符开始");
            Debug.Assert(data.EndsWith(CSpecialChars.EndCh.ToString()), "数据以非'\r'字符结束");

            Debug.WriteLine(data);
            var result = new CReportStruct()
            {
                Sid = data.Substring(1, 4),     //  解析站号
                Type = data.Substring(5, 2),    //  解析类别
                RType = data.Substring(7, 2),   //  解析报类
                SType = data.Substring(9, 2),   //  解析站类
                RecvTime = DateTime.Now,        //  接收时间
                Datas = new List<CReportData>() //  实例化Datas属性
            };

            //  获取数据段，不包含站号、类别、报类、站类信息
            var allData = data.Substring(11);
            allData = allData.Substring(0, allData.Length - 1);

            if (allData.Contains(CSpecialChars.BlankCh))
            {
                foreach (var item in allData.Split(CSpecialChars.BlankCh))
                {
                    result.Datas.Add(UpHelper.GetData(item, result.SType));
                }
            }
            else
            {
                result.Datas.Add(UpHelper.GetData(allData, result.SType));
            }

            Debug.WriteLine("-------------------------");
            return result;
        }
    }
}
