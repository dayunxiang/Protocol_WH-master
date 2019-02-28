using System;
using System.Collections.Generic;
using Hydrology.Entity;


namespace Protocol.Channel.Interface
{
    public interface IHDGprs : IChannel
    {
        void Init();
        int DSStartService(ushort port, int protocol, int mode, string mess, IntPtr ptr);
        int DSStopService(string mess);
        ushort GetListenPort();
        //int sendHex(uint userid,Byte[] data, int len, string mess);
        int sendHex(string userid, byte[] data, uint len, string mess);

        bool SendData(string id, string msg);
        bool FindByID(string userID, out byte[] dtuID);
        void SendDataTwice(string id, string msg);
        void SendDataTwiceForBatchTrans(string id, string msg);

        //List<HDModemInfoStruct> DTUList { get; set; }

        event EventHandler<ReceivedTimeOutEventArgs> GPRSTimeOut;
        event EventHandler<ModemDataEventArgs> ModemDataReceived;   //  登录用户列表更新事件
        event EventHandler HDModemInfoDataReceived;
        //event EventHandler<BatchSDEventArgs> BatchSDDataReceived;
        //bool DSStartService(ushort port);
    }
}
