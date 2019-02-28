using System;
using Hydrology.Entity;

namespace Protocol.Data.Interface
{
    /// <summary>
    /// U盘传输接口
    /// </summary>
    public interface IUBatch
    {
        /// <summary>
        /// U盘传输查询指令
        /// </summary>
        String BuildQuery(string sid, EStationType stationType, ETrans trans, DateTime beginTime, EChannelType ctype);

        /// <summary>
        /// 解析查询后的数据
        /// </summary>
        bool Parse(String data, out CBatchStruct batch);
    }
}
