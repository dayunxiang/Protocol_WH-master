using System;
using Hydrology.Entity;

namespace Protocol.Data.Interface
{
    /// <summary>
    /// 主动上行指令接口
    /// </summary>
    public interface IUp
    {
        /// <summary>
        /// 解析主动上行
        /// </summary>
        /// <param name="msg">数据</param>
        /// <param name="report">数据解析结果</param>
        /// <returns>
        /// True:   解析数据成功
        /// False:  解析数据失败
        /// </returns>
        bool Parse(String msg, out CReportStruct report);
        bool Parse_1(String msg, out CReportStruct report);
        bool Parse_2(String msg, out CReportStruct report);
    }
}
