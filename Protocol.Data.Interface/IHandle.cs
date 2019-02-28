using Entity.Protocol.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Data.Interface
{
    public interface IHandle
    {
        Dictionary<string, Object> getHandledData(CRouter router, string hexOrASCII);
    }
}
