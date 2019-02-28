using System;
using Hydrology.Entity;

namespace Protocol.Data.Interface
{
    public interface IFlashBatch
    {
        /// <summary>
        /// Flash查询指令
        /// </summary>
        String BuildQuery(string sid, EStationType stationType, ETrans trans, DateTime beginTime, DateTime endTime, EChannelType ctype);

        /// <summary>
        /// 解析查询后的数据
        /// </summary>
        bool Parse(String data, out CBatchStruct batch);
    }
}
