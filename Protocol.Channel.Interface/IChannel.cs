using System;
using System.Collections.Generic;
using Hydrology.Entity;
using Protocol.Data.Interface;

namespace Protocol.Channel.Interface
{
    /// <summary>
    /// 信道协议接口
    /// </summary>
    public interface IChannel
    {
        IUp Up { get; set; }                    //  上行指令接口
        IDown Down { get; set; }                //  下行指令接口
        IUBatch UBatch { get; set; }            //  U盘批量传输接口
        IFlashBatch FlashBatch { get; set; }    //  Flash批量传输接口
        ISoil Soil { get; set; }                //  土壤墒情接口
        /// <summary>
        /// 端口之间是否畅通
        /// </summary>
        Boolean IsCommonWorkNormal { get; set; }

        event EventHandler<UpEventArgs> UpDataReceived;             //  上行指令触发的事件
        event EventHandler<DownEventArgs> DownDataReceived;         //  下行指令触发的事件
        event EventHandler<BatchEventArgs> BatchDataReceived;       //  批量传输指令触发的事件
        event EventHandler<ReceiveErrorEventArgs> ErrorReceived;    //  接收错误数据事件
        event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;                //  发送和接收数据时触发的事件，用于记录日志
        event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;  //  通讯口状态改变事件(包括串口和端口)
        event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;       //  墒情数据接收事件
        void InitInterface(IUp up, IDown down, IUBatch udisk, IFlashBatch flash, ISoil soil);       //  初始化数据协议接口
        void InitStations(List<CEntityStation> stations);                               //  初始化系统站点信息

        void Close();   //  关闭方法
    }


}
