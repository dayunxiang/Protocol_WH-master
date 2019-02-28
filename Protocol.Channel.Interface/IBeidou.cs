using System.IO.Ports;
using Hydrology.Entity;
using System;
using Protocol.Data.Interface;
using System.Collections.Generic;

namespace Protocol.Channel.Interface
{
    public interface IBeidouNormal : IChannel
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
        /// 发送文本命令
        /// </summary>
        /// <param name="msg"></param>
        //void SendText(string msg);

        /// <summary>
        /// 发送查询卫星状态信息命令
        /// </summary>
        void SendQSTA();
        /// <summary>
        /// 设置终端状态
        /// 包括调整服务频度，串口波特率，响应波束信息
        /// </summary>
        /// <param name="param"></param>
        void SendSTST(CSTSTStruct param);
        /// <summary>
        /// 自发自收
        /// </summary>
        /// <param name="param"></param>
        String SendTTCA(CTTCAStruct param);

        //Write by LH
      //  void SendTAPP(CTAPPStruct param);
        void SendTAPP();
        String SendBackTTCA();
        //void SendCOSS(CCOSSStruct param);
        //void SendCACA(CCACAStruct param);

        event EventHandler<TSTAEventArgs> TSTACompleted;
        event EventHandler<COUTEventArgs> COUTCompleted;
        event EventHandler<ReceiveErrorEventArgs> BeidouErrorReceived;
    }
}
