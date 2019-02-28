using System;
using System.Collections.Generic;
using Hydrology.Entity;

namespace Protocol.Data.Interface
{
    /// <summary>
    /// 远地下行指令接口
    /// </summary>
    public interface IDown
    {
        /// <summary>
        /// 查询下行指令
        /// </summary>
        String BuildQuery(string sid, IList<EDownParam> cmds, EChannelType ctype);

        String BuildQuery(string sid, IList<EDownParamGY> cmds, EChannelType ctype);

        /// <summary>
        /// 设置命令
        /// </summary>
        String BuildSet(string sid, IList<EDownParam> cmds, CDownConf down, EChannelType ctype);

        /// <summary>
        /// 解析查询后的数据
        /// </summary>
        bool Parse(string resp, out CDownConf downConf);
    }
}
