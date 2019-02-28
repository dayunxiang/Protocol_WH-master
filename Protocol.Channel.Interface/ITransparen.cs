using Hydrology.Entity;
using Protocol.Data.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Channel.Interface
{
    public interface ITransparen : IChannel
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

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns>
        /// True:    服务关闭成功
        /// False：  服务关闭失败
        /// </returns>
        bool DSStopService();

        /// <summary>
        /// 获取监听端口
        /// </summary>
        /// <returns>
        /// ushort: 端口类型
        /// </returns>
        ushort GetListenPort();

        /// <summary>
        /// 透明通讯初始话
        /// </summary>
        void Init();

        bool GetStarted();

        int GetCurrentPort();

        event EventHandler<ReceivedTimeOutEventArgs> GPRSTimeOut;   //  接收数据超时
        event EventHandler<ModemDataEventArgs> ModemDataReceived;   //  登录用户列表更新事件
        event EventHandler ModemInfoDataReceived;
        //  端口监测数据更新事件
        void ListenClientRequest(object state);
        bool SendData(uint id, string msg);
        List<ModemInfoStruct> getDTUList();
        string getSessionIdbyStationid(string stationId);
        bool SendData(string stationId, string datagram);
        Hashtable ClientSessionTable { get; }
        void InitInterface(IUp up, IDown down, IUBatch udisk, IFlashBatch flash, ISoil soil);
    }
}
