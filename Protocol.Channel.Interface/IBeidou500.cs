using System;
using System.IO.Ports;
using Hydrology.Entity;

namespace Protocol.Channel.Interface
{
    public interface IBeidou500 : IChannel
    {
        SerialPort Port { get; set; }

        /// <summary>
        /// 初始化北斗卫星监视串口
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        void Init(string portName, int baudRate);

        /// <summary>
        /// 打开串口
        /// </summary>
        bool Open();

        /// <summary>
        /// 查询北斗卫星所有状态信息
        /// 包括：用户信息(HQYH)、时间输出(CXSJ)、状态检测(ZTJC)
        /// </summary>
        void Query();

        event EventHandler<Beidou500BJXXEventArgs> Beidou500BJXXReceived;
        event EventHandler<Beidou500ZTXXEventArgs> Beidou500ZTXXReceived;
        event EventHandler<Beidou500SJXXEventArgs> Beidou500SJXXReceived;
        /// <summary>
        /// 自发自收
        /// </summary>
        /// <param name="param"></param>
        String Send500TTCA(CBeiDouTTCA param);
        void Send500CXSJ();
    }
}
