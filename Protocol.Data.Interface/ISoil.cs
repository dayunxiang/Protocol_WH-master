using System;
using Hydrology.Entity;

namespace Protocol.Data.Interface
{
    /// <summary>
    /// 墒情协议接口
    /// </summary>
    public interface ISoil
    {
        String BuildQuery();

        bool Parse(string resp, out CEntitySoilData soil,out CReportStruct report);
    }
}
