using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydrology.Entity;

namespace Protocol.Channel.Interface
{
    public interface IWebGsm : IChannel
    {
        bool Init(string ip, int port, string account, string password);

        bool Start();

    }
}
