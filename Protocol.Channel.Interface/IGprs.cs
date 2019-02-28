using System;
using System.Collections.Generic;
using Hydrology.Entity;

namespace Protocol.Channel.Interface
{
    /// <summary>
    /// GPRS 接口
    /// </summary>
    public interface IGprs : IChannel
    {
        /// <summary>
        /// 开启服务
        /// </summary>
        /// <param name="port">端口</param>
        /// <returns>
        /// True:   服务打开成功
        /// False:  服务关闭失败
        /// </returns>
        bool DSStartService(ushort port);

        bool addPort(ushort port);
        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns>
        /// True:    服务关闭成功
        /// False：  服务关闭失败
        /// </returns>
        bool DSStopService();

        /// <summary>
        /// 获取服务启动状态
        /// </summary>
        /// <returns>
        /// True:   端口开启
        /// False:  端口关闭
        /// </returns>
        bool GetStarted();

        /// <summary>
        /// 获取最新错误信息
        /// </summary>
        /// <returns>
        /// String: 最新错误信息
        /// </returns>
        string GetLastError();


        /// <summary>
        /// 获取监听端口
        /// </summary>
        /// <returns>
        /// ushort: 端口类型
        /// </returns>
        ushort GetListenPort();


        ushort GetCurrentPort();

        /// <summary>
        /// 发送数据 16进制
        /// </summary>
        /// <param name="id"></param>
        /// <param name="bts"></param>
        /// <returns></returns>
        void SendDataTwice(uint id, string msg);

        void SendDataTwiceForBatchTrans(uint id, string msg);
        /// <summary>
        /// 初始化
        /// </summary>
        void Init();

        /// <summary>
        /// 根据用户ID，查询模块ID
        /// </summary>
        /// <param name="userID">GPRS号码，即用户ID：如60007052</param>
        /// <param name="dtuID">Modem模块的ID</param>
        /// <returns></returns>
        bool FindByID(string userID, out uint dtuID);


        bool SendHex(uint id, byte[] bts);
        bool SendHexTwice(uint id, byte[] bts);
        List<ModemInfoStruct> DTUList { get; set; }                 //  Modem模块登录列表

        event EventHandler<ReceivedTimeOutEventArgs> GPRSTimeOut;   //  接收数据超时
        event EventHandler<ModemDataEventArgs> ModemDataReceived;   //  登录用户列表更新事件
        event EventHandler ModemInfoDataReceived;       
        //  端口监测数据更新事件

        bool SendData(uint id, string msg);
    }
}
