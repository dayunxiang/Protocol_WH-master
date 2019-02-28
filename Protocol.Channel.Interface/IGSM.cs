using System;
using System.IO.Ports;
using Hydrology.Entity;

namespace Protocol.Channel.Interface
{
    public interface IGsm : IChannel
    {
        string LastError { get; set; }
        SerialPort ListenPort { get; set; }

        bool OpenPort();
        void ClosePort();

        void InitPort(string comPort, int baudRate);
        bool InitGsm();

        //string SendMsg(string atCom);
        void SendMsg(string phone, string msg);

        event EventHandler<ReceivedTimeOutEventArgs> GSMTimeOut;
    }
}
