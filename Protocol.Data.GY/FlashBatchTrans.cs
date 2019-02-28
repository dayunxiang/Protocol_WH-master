using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydrology.Entity;

namespace Protocol.Data.GY
{
    class FlashBatchTrans : IFlashBatch
    {
        public string BuildQuery(string sid, EStationType stationType, ETrans trans, DateTime beginTime, DateTime endTime, EChannelType ctype)
        {
            throw new NotImplementedException();
        }

        public bool Parse(string data, out CBatchStruct batch)
        {
            throw new NotImplementedException();
        }
    }
}
