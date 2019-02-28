/************************************************************************************
* Copyright (c) 2018 All Rights Reserved.
*命名空间：Protocol.Channel.Transparen
*文件名： TSession
*创建人： gaoming
*创建时间：2018-12-10 14:24:55
*描述  Socket会话类
*=====================================================================
*修改标记
*修改时间：2018-12-10 14:24:55
*修改人：XXX
*描述：
************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Protocol.Channel.Transparen
{
    public class TSession
    {
        // 接收数据缓冲区大小 （正常数据不超过过1024）
        private const int DefaultBufferSize = 2 * 1024;

        // 最大无数据接收时间（默认为 600 秒）
        private const int MaxNoDataReceivedTime = 600;

        private int _id;                         // 会话 ID
        private Socket _clientSocket;            // 客户端 Socket
        private string _ip = string.Empty;       // 客户端 IP 地址

        private TSessionState _state;             // 会话状态 
        private TDisconnectType _disconnectType;  // 客户端的退出类型

        private DateTime _loginTime;             // 绘画开始时间
        private DateTime _lastDataReceivedTime;  // 最近接收数据的时间

        public byte[] ReceiveBuffer;   // 数据接收缓冲区
        public byte[] DatagramBuffer;  // 数据包文缓冲区，防止接收空间不够

       
        public int ID  // 会话 ID，即 Socket 的句柄属性
        {
            get { return _id; }
            set { _id = value; }
        }

        public string IP  // 会话客户端 IP
        {
            get { return _ip; }
        }

        public Socket ClientSocket  // 获得与客户端会话关联的Socket对象
        {
            get { return _clientSocket; }
        }

        public TDisconnectType DisconnectType  // 存取客户端的退出方式
        {
            get { return _disconnectType; }
            set { _disconnectType = value; }
        }

        public int ReceiveBufferLength  // 接收缓冲区长度
        {
            get { return ReceiveBuffer.Length; }
        }

        public int DatagramBufferLength  // 会话的数据包长度
        {
            get
            {
                if (DatagramBuffer == null)
                {
                    return 0;
                }
                else
                {
                    return DatagramBuffer.Length;
                }
            }
        }

        public TSessionState State
        {
            get { return _state; }
            set { _state = value; }
        }

        public DateTime LastDataReceivedTime
        {
            set { _lastDataReceivedTime = value; }
            get { return _lastDataReceivedTime; }
        }

        public DateTime LoginTime
        {
            set { _loginTime = value; }
            get { return _loginTime; }
        }


        public TSession(Socket _cliSocket)  // _cliSocket会话使用的Socket连接
        {
            this._clientSocket = _cliSocket;
            this._id = (int)this._clientSocket.Handle;

            IPEndPoint iep = (IPEndPoint)_cliSocket.RemoteEndPoint;
            _ip = iep.Address.ToString();

            ReceiveBuffer = new byte[DefaultBufferSize];  // 数据接收缓冲区
            DatagramBuffer = null;  // 数据包存储区

            _lastDataReceivedTime = DateTime.Now;  // 会话开始时间
            _state = TSessionState.Normal;
        }

        public void Clear()  //  清空缓冲区
        {
            ReceiveBuffer = null;
            DatagramBuffer = null;
        }

        public void ClearDatagramBuffer()  // 清除包文缓冲区
        {
            if (DatagramBuffer != null) DatagramBuffer = null;
        }


        /// <summary>
        /// 连接断开类型枚举 DisconectType
        /// </summary>
        public enum TDisconnectType
        {
            Normal,     // 正常断开
            Timeout,    // 超时断开
            Exception   // 异常断开
        }

        /// <summary>
        /// 通信会话状态枚举 SessionState
        /// </summary>
        public enum TSessionState
        {
            Normal,   // 正常
            NoReply,  // 无应答, 即将关闭
            Closing,  // 正在关闭
            Closed    // 已经关闭
        }

        /// <summary>
        /// 数据包类（框架）
        /// </summary>
        public class TDatagram
        {
            //  字段成员

            public TDatagram() { }

            /// <summary>
            /// 清除包缓冲区
            /// </summary>
            public void Clear() { }

            /// <summary>
            /// 判断数据包类型，包括判错
            /// </summary>
            public bool CheckDatagramKind()
            {
                throw new System.NotImplementedException();
            }

            /// <summary>
            /// 解析数据包
            /// </summary>
            public void ResolveDatagram() { }
        }
}
}