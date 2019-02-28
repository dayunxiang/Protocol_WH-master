using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static Protocol.Channel.Transparen.TSession;
using Hydrology.Entity;
using Entity.Protocol.Channel;
using Protocol.Channer.Router;
using Protocol.Channel.Interface;
using Protocol.Data.Interface;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using Hydrology.DBManager.Interface;
using Hydrology.DBManager.DB.SQLServer;

/************************************************************************************
* Copyright (c) 2018 All Rights Reserved.
*命名空间：Protocol.Channel.Transparen
*文件名： TransparenParser
*创建人： gaoming
*创建时间：2018-12-10 11:18:19
*描述:透明socket通信接收与发送
*=====================================================================
*修改标记
*修改时间：2018-12-10 11:18:19
*修改人：XXX
*描述：
************************************************************************************/
namespace Protocol.Channel.Transparen
{
    /// <summary>
    /// tru通过透明模式传输报文解析与下行命令发送
    /// 
    /// </summary>
    public class TransparenDll : ITransparen
    {
        #region 事件
        public event EventHandler ReceiverException;  // 接收器异常错误
        public event EventHandler ReceiverWork;       // 接收器工作事件（启动、暂停、关闭、改参）

        public event EventHandler ClientException;  // 客户端异常事件（读/写异常）
        public event EventHandler ClientRequest;    // 客户端请求事件（连接、关闭）

        public event EventHandler DatagramError;   // 错误数据包
        public event EventHandler DatagramHandle;  // 处理数据包
        #endregion

        #region 用于触发回主程序的数据的事件
        public event EventHandler<UpEventArgs> UpDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;
        public event EventHandler<ReceivedTimeOutEventArgs> GPRSTimeOut;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;

        public event EventHandler<ModemDataEventArgs> ModemDataReceived;
        public event EventHandler ModemInfoDataReceived;
        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        #endregion

        #region 字段
        private int _loopWaitTime = 25;                      // 默认循环等待时间（毫秒）
        private int _maxAllowDatagramQueueCount = 8 * 1024;  // 默认的最大队列数据
        private int _maxAllowClientCount = 512;              // 接收器程序允许的最大客户端连接数
        private int _maxAllowListenQueueLength = 5;          // 侦听队列的最大长度
        private int _maxSocketDataTimeout = 3600;             // 最大无数据接收时间（3600秒）

        private int _clientCount;           // 连接客户端会话计数
        private int _datagramCount;         // 总计收到客户端数据包（含错误包）
        private int _datagramQueueCount;    // 待处理包计数
        private int _errorDatagramCount;    // 总计收到客户端错误数据包
        private int _exceptionCount;        // 接收器端总计发生的异常错误

        private int _tcpSocketPort ;  // 接收器端口号
        private Socket _receiverSocket;     // 接收器 Socket 对象

        private bool _stopReceiver = true;          // 停止 DatagramReceiver 接收器
        private bool _stopConnectRequest = false;   // 停止客户端连接请求

        private Hashtable _sessionTable;          // 客户端会话哈希表
        private Queue<TDatagram> _datagramQueue;  // 待处理数据包队列

        private string _dbConnectionStr;       // 数据库连接字串（时段）
        private SqlConnection _sqlConnection;  // 数据库连接对象（时段）

        private int cores;  //系统cpu*核心数

        private EChannelType m_channelType;

        private EListeningProtType m_portType;

        private Channle2Data channel2Data;

        private Dictionary<string, object> resMap;

        private IStationProxy m_proxyStation;
        private ISoilStationProxy m_proxySoliStation;
        private List<CEntityStation> m_listStations;
        private List<CEntitySoilStation> m_listSoillStations;
        private List<TransparentHelper> m_listTransparents;


        #endregion

        #region 定时器
        private System.Timers.Timer onlineTimer;
        #endregion

        #region 属性
        public bool isStart;
        
        public bool IsRun  // 接收器运行状态
        {
            get { return !_stopReceiver; }
        }
        public List<ModemInfoStruct> DTUList;
        public bool StopConnectRequst
        {
            get { return _stopConnectRequest; }
            set { _stopConnectRequest = value; }
        }

        public int TcpSocketPort  // Socket 端口
        {
            get { return _tcpSocketPort; }
            set { _tcpSocketPort = value; }
        }

        public List<TransparentHelper> pub_listTransparents
        {
            get
            {
                return m_listTransparents;
            }
            set
            {
                m_listTransparents = value;
            }
        }

        public int DatagramCount  // 总计接收数据包
        {
            get { return _datagramCount; }
        }

        public int ErrorDatagramCount  // 总计错误数据包
        {
            get { return _errorDatagramCount; }
        }

        public int ExceptionCount  // 接收器异常错误数
        {
            get { return _exceptionCount; }
        }

        public int ClientCount  // 当前的客户端连接数
        {
            get { return _clientCount; }
        }

        public int DatagramQueueCount  // 待处理的数据包总数
        {
            get { return _datagramQueueCount; }
        }

        public int MaxAllowClientCount  // 允许连接的最大数
        {
            get { return _maxAllowClientCount; }
            set { _maxAllowClientCount = value; }
        }

        public int MaxListenQueueLength  // 侦听队列的长度
        {
            get { return _maxAllowListenQueueLength; }
            set { _maxAllowListenQueueLength = value; }
        }

        public int MaxAllowDatagramQueueCount  // 最大数据包队列数
        {
            set { _maxAllowDatagramQueueCount = value; }
        }

        public int MaxSocketDataTimeout  // 最大无数据传输时间（即超时时间）
        {
            get { return _maxSocketDataTimeout; }
            set { _maxSocketDataTimeout = value; }
        }

        public int LoopWaitTime  // 循环等待时间
        {
            get { return _loopWaitTime; }
            set { _loopWaitTime = value; }
        }

        public string DBConnectionStr
        {
            set { _dbConnectionStr = value; }
        }

        public Hashtable ClientSessionTable  // 当前在线会话列表副本
        {
            get
            {
                Hashtable sessionOnline = new Hashtable();
                lock (_sessionTable)
                {
                    foreach (TSession session in _sessionTable.Values)
                    {
                        sessionOnline.Add(session.ID, session);
                    }
                }
                return sessionOnline;
            }
        }

        public IUp Up { get; set; }
        public IDown Down { get; set; }
        public IUBatch UBatch { get; set; }
        public IFlashBatch FlashBatch { get; set; }
        public ISoil Soil { get; set; }

        public bool IsCommonWorkNormal
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion


        public TransparenDll() {
            onlineTimer= new System.Timers.Timer(5000);
            m_channelType = EChannelType.None;
            cores = System.Environment.ProcessorCount;
            channel2Data = new Channle2Data();
            resMap = new Dictionary<string, object>();
            m_proxyStation = new CSQLStation();
            m_proxySoliStation = new CSQLSoilStation();
            m_listStations = new List<CEntityStation>();
            m_listSoillStations = new List<CEntitySoilStation>();
            m_listTransparents = new List<TransparentHelper>();
        }
        #region 公共方法
        /// <summary>
        /// 关闭接收器，要求在断开全部连接后才能停止
        /// </summary>
        public bool DSStopService()  // 关闭接收器（要求在断开全部连接后才能停止）
        {
            onlineTimer.Stop();
            // 先设置该值, 否则在循环 AccecptClientConnect 时可能出错
            _stopReceiver = true;

            //if (_sqlConnection != null)
            //{
            //    this.CloseDatabase();
            //}

            if (_datagramQueue != null)  // 清空队列
            {
                lock (_datagramQueue)
                {
                    while (_datagramQueue.Count > 0)
                    {
                        TDatagram datagram = _datagramQueue.Dequeue();
                        datagram.Clear();
                    }
                }
            }

            if (_sessionTable != null)  // 关闭各个会话
            {
                lock (_sessionTable)
                {
                    foreach (TSession session in _sessionTable.Values)
                    {
                        try
                        {
                            session.Clear();
                            session.ClientSocket.Close();
                        }
                        catch { }
                    }
                }
            }

            if (_receiverSocket != null)  // 关闭接收器 Socket
            {
                lock (_receiverSocket)
                {
                    try
                    {
                        if (_sessionTable != null && _sessionTable.Count > 0)
                        {
                            // 可能引起 AcceptClientConnect 的 Poll 结束
                            _receiverSocket.Shutdown(SocketShutdown.Receive);
                        }
                        _receiverSocket.Close();
                    }
                    catch(Exception e)
                    {
                        relievePort(TcpSocketPort);
                        this.OnReceiverException();
                    }
                }
            }

            if (_sessionTable != null)  // 最后清空会话列表
            {
                lock (_sessionTable)
                {
                    _sessionTable.Clear();
                }
            }
            isStart = false;
            return true;
        }

        /// <summary>
        ///  启动接收器
        /// </summary>
        public bool DSStartService(ushort port)
        {
            try
            {
                

                _stopReceiver = true;

                this.DSStopService();

                onlineTimer.Start();

                //if (!this.ConnectDatabase()) return false;

                _clientCount = 0;
                _datagramQueueCount = 0;
                _datagramCount = 0;
                _errorDatagramCount = 0;
                _exceptionCount = 0;

                _sessionTable = new Hashtable(_maxAllowClientCount);
                _datagramQueue = new Queue<TDatagram>(_maxAllowDatagramQueueCount);

                _stopReceiver = false;  // 循环中均要该标志

                ThreadPool.SetMaxThreads(cores * 2, cores);

                if (!this.CreateReceiverSocket(port))  //建立服务器端 Socket 对象
                {
                    return false;
                }

                // 侦听客户端连接请求线程, 使用委托推断, 不建 CallBack 对象
                if (!ThreadPool.QueueUserWorkItem(ListenClientRequest))
                {
                    return false;
                }

                // 处理数据包队列线程
                if (!ThreadPool.QueueUserWorkItem(HandleDatagrams))
                {
                    return false;
                }

                // 检查客户会话状态, 长时间未通信则清除该对象
                if (!ThreadPool.QueueUserWorkItem(CheckClientState))
                {
                    return false;
                }
                isStart = true;
                TcpSocketPort = port;
                _stopConnectRequest = false;  // 启动接收器，则自动允许连接
            }
            catch
            {
                this.OnReceiverException();
                _stopReceiver = true;
            }

            return !_stopReceiver;
        }


        /// <summary>
        ///  关闭全部客户端会话，并做关闭标记
        /// </summary>
        public void CloseAllSession()
        {
            lock (_sessionTable)
            {
                foreach (TSession session in _sessionTable.Values)
                {
                    lock (session)
                    {
                        if (session.State == TSessionState.Normal)
                        {
                            session.DisconnectType = TDisconnectType.Normal;

                            // 做标记，在另外的进程中关闭
                            session.State = TSessionState.NoReply;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 直接关闭一个客户端会话
        /// </summary>
        /// <param name="sessionID"></param>
        public bool CloseOneSession(int sessionID)
        {
            bool closeSuccess = false;

            lock (_sessionTable)
            {
                if (_sessionTable.ContainsKey(sessionID))  // 包含该会话 ID
                {
                    closeSuccess = true;

                    TSession session = (TSession)_sessionTable[sessionID];

                    lock (session)
                    {
                        if (session.State == TSessionState.Normal)
                        {
                            session.DisconnectType = TDisconnectType.Normal;

                            // 做标记，在 CheckClientState 中关闭
                            session.State = TSessionState.NoReply;
                        }
                    }
                }
            }
            return closeSuccess;
        }

        /// <summary>
        ///  对 ID 号的 session 发送包信息
        /// </summary>
        /// <param name="sessionID"></param>
        public bool SendData(uint sessionID, string datagram)
        {
            bool sendSuccess = false;

            TSession session = null;
            try
            {
                foreach (int key in _sessionTable.Keys)
                {
                    uint a = (uint)key;
                    if ((uint)key == sessionID)
                    {
                        session = (TSession)_sessionTable[key];
                    }
                }
            }
            catch(Exception e)
            {

            }
           
            if (_sessionTable.ContainsKey(sessionID))
            {
                lock (_sessionTable)
                {
                    session = (TSession)_sessionTable[sessionID];
                }
            }
            if (_sessionTable.ContainsKey(sessionID.ToString()))
            {
                lock (_sessionTable)
                {
                    session = (TSession)_sessionTable[sessionID.ToString()];
                }
            }


            if (session != null && session.State == TSessionState.Normal)
            {
                lock (session)
                {
                    try
                    {
                        //byte[] data = Encoding.ASCII.GetBytes(datagram);  // 获得数据字节数组
                        //TODO
                        byte[] data = hexStringToByte(datagram);
                        session.ClientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, EndSendData, session);
                        sendSuccess = true;
                    }
                    catch
                    {
                        session.DisconnectType = TDisconnectType.Exception;

                        // 写 socket 发生错误，则准备关闭该会话，系统不认为是错误
                        session.State = TSessionState.NoReply;

                        this.OnClientException();
                    }
                }
                InvokeMessage(datagram, "发送");
            }

            return sendSuccess;
        }


        public bool SendData(string  stationId, string datagram)
        {
            bool sendSuccess = false;
            TSession session = null;
            uint sessionID = uint.Parse(getSessionIdbyStationid(stationId));
            try
            {
                foreach (int key in _sessionTable.Keys)
                {
                    uint a = (uint)key;
                    if ((uint)key == sessionID)
                    {
                        session = (TSession)_sessionTable[key];
                    }
                }
            }
            catch (Exception e)
            {

            }

            if (_sessionTable.ContainsKey(sessionID))
            {
                lock (_sessionTable)
                {
                    session = (TSession)_sessionTable[sessionID];
                }
            }
            if (_sessionTable.ContainsKey(sessionID.ToString()))
            {
                lock (_sessionTable)
                {
                    session = (TSession)_sessionTable[sessionID.ToString()];
                }
            }


            if (session != null && session.State == TSessionState.Normal)
            {
                lock (session)
                {
                    try
                    {
                        //byte[] data = Encoding.ASCII.GetBytes(datagram);  // 获得数据字节数组
                        //TODO
                        byte[] data = hexStringToByte(datagram);
                        session.ClientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, EndSendData, session);
                        sendSuccess = true;
                    }
                    catch
                    {
                        session.DisconnectType = TDisconnectType.Exception;

                        // 写 socket 发生错误，则准备关闭该会话，系统不认为是错误
                        session.State = TSessionState.NoReply;

                        this.OnClientException();
                    }
                }
                InvokeMessage(datagram, "发送");
            }

            return sendSuccess;
        }
        #endregion


        #region 保护方法(事件方法)

        protected void OnReceiverException()
        {
            if (ReceiverException != null)
            {
                ReceiverException(this, new EventArgs());
            }
        }

        protected void OnReceiverWork()
        {
            if (ReceiverWork != null)
            {
                ReceiverWork(this, new EventArgs());
            }
        }

        protected void OnClientException()
        {
            if (ClientException != null)
            {
                ClientException(this, new EventArgs());
            }
        }

        protected void OnClientRequest()
        {
            if (ClientRequest != null)
            {
                ClientRequest(this, new EventArgs());
            }
        }

        protected void OnDatagramError()
        {
            if (DatagramError != null)
            {
                DatagramError(this, new EventArgs());
            }
        }

        protected void OnDatagramHandle()
        {
            if (DatagramHandle != null)
            {
                DatagramHandle(this, new EventArgs());
            }
        }

        #endregion


        #region 私有方法
        /// <summary>
        /// 创建接收服务器的 Socket, 并侦听客户端连接请求
        /// </summary>
        private bool CreateReceiverSocket(ushort port)
        {
            try
            {
                _receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _receiverSocket.Bind(new IPEndPoint(IPAddress.Any, port));  // 绑定端口
                this._tcpSocketPort = port;
                _receiverSocket.Listen(_maxAllowListenQueueLength);  // 开始监听
                return true;
            }
            catch
            {
                this.OnReceiverException();
                return false;
            }
        }
        /// <summary>
        /// 清理数据库资源
        /// </summary>
        //private bool CloseDatabase()
        //{
        //    try
        //    {
        //        if (_sqlConnection != null)
        //        {
        //            _sqlConnection.Close();
        //        }
        //        return true;
        //    }
        //    catch
        //    {
        //        this.OnReceiverException();
        //        return false;
        //    }
        //}

        /// <summary>
        /// 连接数据库
        /// </summary>
        private bool ConnectDatabase()
        {
            bool connectSuccess = false;
            _sqlConnection = new SqlConnection();

            try
            {
                _sqlConnection.ConnectionString = _dbConnectionStr;
                _sqlConnection.Open();

                connectSuccess = true;
            }
            catch
            {
                this.OnReceiverException();
            }
            finally
            {
                //if (!connectSuccess)
                //{
                //    this.CloseDatabase();
                //}
            }
            return connectSuccess;
        }

        private void initStations()
        {
            m_listStations = m_proxyStation.QueryAll();
            m_listSoillStations = m_proxySoliStation.QueryAllSoilStation();
            //水雨情、墒情中gprs号码 gsm号码 北斗号码都是不重复的
            //1.初始化水情信息对应
            for (int i = 0; i < m_listStations.Count; i++)
            {
                TransparentHelper transparent = new TransparentHelper();
                transparent.stationId = m_listStations[i].StationID;
                transparent.gprsId = m_listStations[i].GPRS;
                transparent.gsmNum = m_listStations[i].GSM;
                transparent.beidouNum = m_listStations[i].BDSatellite;
                m_listTransparents.Add(transparent);
            }
            //2.初始化墒情信息对应
            for (int i = 0; i < m_listSoillStations.Count; i++)
            {
                TransparentHelper transparent = new TransparentHelper();
                transparent.gprsId = m_listSoillStations[i].GPRS;
                transparent.gsmNum = m_listSoillStations[i].GSM;
                transparent.beidouNum = m_listSoillStations[i].BDSatellite;
                m_listTransparents.Add(transparent);
            }
        }
        /// <summary>
        /// 判断重复IP地址
        /// </summary>
        private bool CheckSameClientIP(Socket clientSocket)  // 
        {
            IPEndPoint iep = (IPEndPoint)clientSocket.RemoteEndPoint;
            string ip = iep.Address.ToString();

            if (ip.Substring(0, 7) == "127.0.0")
            {
                return false;  // 本机器测试特别设定
            }

            lock (_sessionTable)
            {
                foreach (TSession session in _sessionTable.Values)
                {
                    if (session.IP == ip)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 循环侦听客户端请求，由于要用线程池，故带一个参数
        /// </summary>
        public void ListenClientRequest(object state)
        {
            Socket client = null;
            while (!_stopReceiver)
            {
                if (_stopConnectRequest)  //  停止客户端连接请求
                {
                    if (_receiverSocket != null)
                    {
                        try
                        {
                            _receiverSocket.Close();  // 强制关闭接收器
                        }
                        catch
                        {
                            this.OnReceiverException();
                        }
                        finally
                        {
                            // 必须为 null，否则 disposed 对象仍然存在，将引发下面的错误
                            _receiverSocket = null;
                        }
                    }
                    continue;
                }
                else
                {
                    if (_receiverSocket == null)
                    {
                        if (!this.CreateReceiverSocket((ushort)_tcpSocketPort))
                        {
                            continue;
                        }
                    }
                }

                try
                {
                    if (_receiverSocket.Poll(_loopWaitTime, SelectMode.SelectRead))
                    {
                        // 频繁关闭、启动时，这里容易产生错误（提示套接字只能有一个）
                        client = _receiverSocket.Accept();

                        if (client != null && client.Connected)
                        {
                            if (this._clientCount >= this._maxAllowClientCount)
                            {
                                this.OnReceiverException();

                                try
                                {
                                    client.Shutdown(SocketShutdown.Both);
                                    client.Close();
                                }
                                catch { }
                            }
                            else if (CheckSameClientIP(client))  // 已存在该 IP 地址
                            {
                                try
                                {
                                    client.Shutdown(SocketShutdown.Both);
                                    client.Close();
                                }
                                catch { }
                            }
                            else
                            {
                                TSession session = new TSession(client);
                                session.LoginTime = DateTime.Now;

                                lock (_sessionTable)
                                {
                                    int preSessionID = session.ID;
                                    while (true)
                                    {
                                        if (_sessionTable.ContainsKey(session.ID))  // 有可能重复该编号
                                        {
                                            session.ID = 100000 + preSessionID;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    _sessionTable.Add(session.ID, session);  // 登记该会话客户端
                                    Interlocked.Increment(ref _clientCount);
                                }

                                this.OnClientRequest();

                                try  // 客户端连续连接或连接后立即断开，易在该处产生错误，系统忽略之
                                {
                                    // 开始接受来自该客户端的数据
                                    session.ClientSocket.BeginReceive(session.ReceiveBuffer, 0,
                                        session.ReceiveBufferLength, SocketFlags.None, EndReceiveData, session);
                                }
                                catch
                                {
                                    session.DisconnectType = TDisconnectType.Exception;
                                    session.State = TSessionState.NoReply;
                                }
                            }
                        }
                        else if (client != null)  // 非空，但没有连接（connected is false）
                        {
                            try
                            {
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                            }
                            catch { }
                        }
                    }
                }
                catch
                {
                    this.OnReceiverException();

                    if (client != null)
                    {
                        try
                        {
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                        }
                        catch { }
                    }
                }
                // 该处可以适当暂停若干毫秒
            }
            // 该处可以适当暂停若干毫秒
        }

        private void EndSendData(IAsyncResult iar)  //  发送数据完成处理函数, iar 为目标客户端 Session
        {
            TSession session = (TSession)iar.AsyncState;
            lock (_sessionTable)
            {
                // 再次判断是否在表中，Shutdown 时，可能激发本过程
                session = (TSession)_sessionTable[session.ID];
            }

            if (session != null && session.State == TSessionState.Normal)
            {
                lock (session)
                {
                    try
                    {
                        Socket client = session.ClientSocket;
                        int sent = client.EndSend(iar);
                    }
                    catch
                    {
                        session.DisconnectType = TDisconnectType.Exception;

                        // 写 Socket 发生错误，则准备关闭该会话，系统不认为是错误
                        session.State = TSessionState.NoReply;
                        this.OnClientException();
                    }
                }
            }
        }

        private void EndReceiveData(IAsyncResult iar)  // iar 目标客户端 Session
        {

            TSession session = (TSession)iar.AsyncState;
            lock (_sessionTable)
            {
                // 再次判断是否在表中，Shutdown 时，可能激发本过程
                session = (TSession)_sessionTable[session.ID];
            }

            if (session == null || session.State != TSessionState.Normal) return;

            lock (session)
            {
                try
                {
                    Socket client = session.ClientSocket;

                    // 注意：Shutdown 时将调用 ReceiveData，此时也可能收到 0 长数据包
                    int recv = client.EndReceive(iar);
                    if (recv == 0)
                    {
                        session.DisconnectType = TDisconnectType.Normal;
                        session.State = TSessionState.NoReply;
                    }
                    else  // 正常数据包
                    {
                        session.LastDataReceivedTime = DateTime.Now;
                        // 合并报文，按报文头、尾字符标志抽取报文，将包交给数据处理器
                        ResolveBuffer(session, recv);

                        // 继续接收来自来客户端的数据（异步调用）
                        session.ClientSocket.BeginReceive(session.ReceiveBuffer, 0,
                            session.ReceiveBufferLength, SocketFlags.None, EndReceiveData, session);
                    }
                }
                catch  // 读 socket 发生异常，则准备关闭该会话，系统不认为是错误（这种错误可能太多）
                {
                    try
                    {
                        session.ClientSocket.BeginReceive(session.ReceiveBuffer, 0,
                            session.ReceiveBufferLength, SocketFlags.None, EndReceiveData, session);
                    }
                    catch(Exception e)
                    {

                    }
                    
                    //TODO 需要测试
                    //session.DisconnectType = TDisconnectType.Exception;
                    //session.State = TSessionState.NoReply;
                }
            }
        }
        private void ResolveBuffer_1(TSession session, int receivedSize)
        {
            string data = string.Empty;
            int sessionId = session.ID;
            string ip = session.IP;
            DateTime loginTime = session.LoginTime;
            DateTime refreshTime = session.LastDataReceivedTime;
            byte[] ReceiveBuffer = session.ReceiveBuffer;
            byte[] dataByteList = new byte[receivedSize];

            for (int i = 0; i < receivedSize; i++)
            {
                dataByteList[i] = ReceiveBuffer[i];
                if (i == 1024)
                {
                    receivedSize = Int32.Parse(ReceiveBuffer[i].ToString()) * 2;
                }
                //如果是16进制传输则以此种方式传输
                data += ReceiveBuffer[i].ToString("X2");
            }
            string gprsid = string.Empty;
            //string messageStr = System.Text.Encoding.Default.GetString(dataByteList);
            string messageStr = System.Text.Encoding.ASCII.GetString(dataByteList);
            if (messageStr.Contains("\u0001") && messageStr.Contains("\u0002") && messageStr.Contains("\u0003"))
            {
                string[] messageList = Regex.Split(messageStr, "\u0001", RegexOptions.IgnoreCase);
                string data1 = messageList[0];
                gprsid = data1.Substring(2, 10);
                foreach(TransparentHelper transparent in m_listTransparents)
                {
                    if(transparent.stationId == gprsid)
                    {
                        transparent.ip = ip;
                        transparent.sessionId = sessionId.ToString();
                        break;
                    }
                }


            }
            //if(messageStr.Length > 5)
            string temp = "定时报";
            InvokeMessage("  gprs号码:  " + gprsid + String.Format("  {0,-10}   ", temp) + messageStr, "接收");
            Console.WriteLine(messageStr);
        }

        private void Online_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (GetStarted())
            {
                if (this.ModemInfoDataReceived != null)
                {
                    this.ModemInfoDataReceived(this, null);
                }
            }else
            {
                Debug.WriteLine("TCP透明传输未开启");
            }
        }

        
        /// <summary>
        /// 按报文头、尾字符标志抽取报文，可能合并报文
        /// TODO 后续数据协议调试好后 需要用这个
        /// </summary>
        private void ResolveBuffer(TSession session, int receivedSize)
        {
            string data = string.Empty;
            int sessionId = session.ID;
            string ip = session.IP;
            DateTime loginTime = session.LoginTime;
            DateTime refreshTime = session.LastDataReceivedTime;
            byte[] ReceiveBuffer = session.ReceiveBuffer;
            byte[] dataByteList = new byte[receivedSize];

            for (int i = 0; i < receivedSize; i++)
            {
                dataByteList[i] = ReceiveBuffer[i];
                if (i == 2048)
                {
                    receivedSize = Int32.Parse(ReceiveBuffer[i].ToString()) * 2;
                }
            }
            string gprsid = string.Empty;
            string messageStr = System.Text.Encoding.ASCII.GetString(dataByteList);
            InvokeMessage(messageStr, "原始数据");
            string data1 = string.Empty;
            if (messageStr.Contains("\u0001") && messageStr.Contains("\u0002") && messageStr.Contains("\u0003"))
            {
                string[] messageList = Regex.Split(messageStr, "\u0001", RegexOptions.IgnoreCase);
                if (messageList == null || messageList.Length <= 1) { return; }
                data1 = messageList[1];
                gprsid = data1.Substring(2, 10);
                foreach (TransparentHelper transparent in m_listTransparents)
                {
                    if (transparent.stationId == gprsid)
                    {
                        transparent.ip = ip;
                        transparent.sessionId = sessionId.ToString();
                        break;
                    }
                }


            }
            //if(messageStr.Length > 5)
            string typ = data1.Substring(16, 2);
            //InvokeMessage("  gprs号码:  " + gprsid + String.Format("  {0,-10}   ", typ) + messageStr, "接收");

            //byte转string，如果是ASCII方式 则以此种方式传输
            //string str = System.Text.Encoding.Default.GetString(dataByteList);
            // TODO
            //数据处理部分 后续可能需要更改  透明传输可能需要站点ID和数据协议的匹配
            CRouter router = new CRouter();
            router.dutid = sessionId.ToString();
            router.sessionid = sessionId.ToString();
            router.rawData = dataByteList;
            router.dataLength = receivedSize;
            resMap = channel2Data.commonHandle(router, "transparent");

          // 接收回执
            if(resMap["RET"] != null)
            {
                List<Dictionary<string, string>> retList = (List<Dictionary<string,string>>) resMap["RET"];
                if(retList != null && retList.Count >= 1)
                {
                    foreach(Dictionary<string,string> ret in retList)
                    {
                        foreach (var item in ret)
                        {
                            //根据Stationid获取sessionid 
                            //TODO
                            string sessionid = item.Key.ToString();
                            string returnMessage = item.Value;
                            SendData(uint.Parse(sessionid), returnMessage);
                        } 
                    }
                  }
            }
            // 报文日志书写
            if (resMap["SORMEA"] != null)
            {
                List<SendOrRecvMsgEventArgs> sendOrRecvMsgEventArgsList = (List<SendOrRecvMsgEventArgs>)resMap["SORMEA"];
                if (sendOrRecvMsgEventArgsList != null && sendOrRecvMsgEventArgsList.Count >= 1)
                {
                    foreach (SendOrRecvMsgEventArgs sendOrRecvMsgEventArgs in sendOrRecvMsgEventArgsList)
                    {
                        InvokeMessage(sendOrRecvMsgEventArgs.Msg, sendOrRecvMsgEventArgs.Description);
                    }

                }
            }
                
           
            if (resMap["UEA"] != null)
            {
                List<UpEventArgs> upEventArgsList = (List<UpEventArgs>)resMap["UEA"];
                if(upEventArgsList!=null && upEventArgsList.Count >= 1)
                {
                    foreach(UpEventArgs upEventArgs in upEventArgsList)
                    {
                        upEventArgs.Value.ChannelType = EChannelType.TCP;
                        //upEventArgs.Value.flagId = "1234";
                        upEventArgs.Value.ListenPort = TcpSocketPort.ToString();

                        //写日志
                        //string temp1= "定时报";
                        //InvokeMessage("  gprs号码:  " + "1234" + String.Format("  {0,-10}   ", temp1) + upEventArgs.RawData, "接收");

                        //返回报文数据到主程序
                        if (this.UpDataReceived != null)
                            this.UpDataReceived.Invoke(null, upEventArgs);
                    }
                }

            }

          }

        /// <summary>
        /// 检查客户端状态（扫描方式，若长时间无数据，则断开）
        /// </summary>
        private void CheckClientState(object state)
        {
            while (!_stopReceiver)
            {
                DateTime thisTime = DateTime.Now;

                // 建立一个副本 ，然后对副本进行操作
                Hashtable sessionTable2 = new Hashtable();
                lock (_sessionTable)
                {
                    foreach (TSession session in _sessionTable.Values)
                    {
                        if (session != null)
                        {
                            sessionTable2.Add(session.ID, session);
                        }
                    }
                }

                foreach (TSession session in sessionTable2.Values)  // 对副本进行操作
                {
                    Monitor.Enter(session);
                    try
                    {
                        if (session.State == TSessionState.NoReply)  // 分三步清除一个 Session
                        {
                            session.State = TSessionState.Closing;
                            if (session.ClientSocket != null)
                            {
                                try
                                {
                                    // 第一步：shutdown
                                    session.ClientSocket.Shutdown(SocketShutdown.Both);
                                }
                                catch { }
                            }
                        }
                        else if (session.State == TSessionState.Closing)
                        {
                            session.State = TSessionState.Closed;
                            if (session.ClientSocket != null)
                            {
                                try
                                {
                                    // 第二步： Close
                                    session.ClientSocket.Close();
                                }
                                catch { }
                            }
                        }
                        else if (session.State == TSessionState.Closed)
                        {

                            lock (_sessionTable)
                            {
                                // 第三步：remove from table
                                _sessionTable.Remove(session.ID);
                                Interlocked.Decrement(ref _clientCount);
                            }

                            this.OnClientRequest();
                            session.Clear();  // 清空缓冲区
                        }
                        else if (session.State == TSessionState.Normal)  // 正常的会话 
                        {

                            TimeSpan ts = thisTime.Subtract(session.LastDataReceivedTime);
                            if (Math.Abs(ts.TotalSeconds) > _maxSocketDataTimeout)  // 超时，则准备断开连接
                            {
                                session.DisconnectType = TDisconnectType.Timeout;
                                session.State = TSessionState.NoReply;  // 标记为将关闭、准备断开
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(session);
                    }
                }  // end foreach

                sessionTable2.Clear();
            }  // end while
        }
        /// <summary>
        /// 分析一个数据包
        /// </summary>
        private void AnalyzeOneDatagram(TSession session, int packPos, int packLen)
        {
            // ...（省大段内容）

            Interlocked.Increment(ref _datagramCount);
            TDatagram datagram = null;
            datagram.ResolveDatagram();

            Interlocked.Increment(ref _datagramQueueCount);
            lock (_datagramQueue)
            {
                _datagramQueue.Enqueue(datagram);  // 数据包入队列
            }
            session.ClearDatagramBuffer();  // 清空会话缓冲区
        }

        /// <summary>
        /// 处理数据包队列，由于要用线程池，故带一个参数
        /// </summary>
        private void HandleDatagrams(object state)
        {
            while (!_stopReceiver)
            {
                this.HandleOneDatagram();  // 处理一个数据包

                //if (!_stopReceiver)
                //{
                //    // 如果连接关闭，则重新建立，可容许几个连接错误出现
                //    if (_sqlConnection.State == ConnectionState.Closed)
                //    {
                //        this.OnReceiverWork();

                //        try
                //        {
                //            _sqlConnection.Open();
                //        }
                //        catch
                //        {
                //            this.OnReceiverException();
                //        }
                //    }
                //}
            }
        }

        /// <summary>
        /// 处理一个包数据，包括：验证、存储
        /// </summary>
        private void HandleOneDatagram()
        {
            TDatagram datagram = null;

            lock (_datagramQueue)
            {
                if (_datagramQueue.Count > 0)
                {
                    datagram = _datagramQueue.Dequeue();  // 取队列数据
                    Interlocked.Decrement(ref _datagramQueueCount);
                }
            }

            if (datagram == null) return;

            datagram.Clear();
            datagram = null;  // 释放对象
        }

        /// <summary>
        /// 16进制string转byte
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        private  byte[] hexStringToByte(String hex)
        {
            int len = (hex.Length / 2);
            byte[] result = new byte[len];
            char[] achar = hex.ToCharArray();
            for (int i = 0; i < len; i++)
            {
                int pos = i * 2;
                result[i] = (byte)(toByte(achar[pos]) << 4 | toByte(achar[pos + 1]));
            }
            return result;
        }

        private static int toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        private void InvokeMessage(string msg, string description)
        {
            if (this.MessageSendCompleted != null)
                this.MessageSendCompleted(null, new SendOrRecvMsgEventArgs()
                {
                    ChannelType = this.m_channelType,
                    Msg = msg,
                    Description = description
                });
        }

        private int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }
        
        public  string  getSessionIdbyStationid(string stationId)
        {
            string sessionId = "0";
            if(m_listTransparents == null || m_listTransparents.Count == 0)
            {
                return sessionId;
            }
            foreach (TransparentHelper transparent in m_listTransparents)
            {
                if (transparent.stationId == stationId)
                {
                    sessionId = transparent.sessionId;
                    break;
                }
            }
            return sessionId;
        }
        #endregion

        #region 如果关闭socket失败，则强制解除端口占用关闭端口占用
        private void relievePort(int port)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            List<int> list_pid = GetPidByPort(p, port);
            PidKill(p, list_pid);
        }
        private static List<int> GetPidByPort(Process p, int port)
        {
              int result;
              bool b = true;
              p.Start();
              p.StandardInput.WriteLine(string.Format("netstat -ano|find \"{0}\"", port));
              p.StandardInput.WriteLine("exit");
              StreamReader reader = p.StandardOutput;
              string strLine = reader.ReadLine();
              List<int> list_pid = new List<int>();
              while (!reader.EndOfStream)
              {
                  strLine = strLine.Trim();
                  if (strLine.Length > 0 && ((strLine.Contains("TCP") || strLine.Contains("UDP"))))
                  {
                      Regex r = new Regex(@"\s+");
                      string[] strArr = r.Split(strLine);
                      if (strArr.Length >= 4)
                      {
                          b = int.TryParse(strArr[3], out result);
                          if (b && !list_pid.Contains(result))
                              list_pid.Add(result);
                      }
                  }
                  strLine = reader.ReadLine();
              }
              p.WaitForExit();
              reader.Close();
              p.Close();
              return list_pid;
          }


        private static void PidKill(Process p, List<int> list_pid)
        {
             p.Start();
              foreach (var item in list_pid)
              {
                  p.StandardInput.WriteLine("taskkill /pid " + item + " /f");
                  p.StandardInput.WriteLine("exit");
              }
              p.Close();
         }

        #endregion


        #region 接口方法
        /// <summary>
        /// 获取当前
        /// </summary>
        /// <returns></returns>
        public ushort GetListenPort()
        {
            return (ushort)TcpSocketPort;
        }


        public void Init()
        {
            //this._dbConnectionStr = "Data Source=(local);Initial Catalog=FFF;Persist Security Info=True;User ID=sa;Password=123456;Connect Timeout=30";
            this.m_channelType = EChannelType.GPRS;
            this.m_portType = EListeningProtType.Port;
            initStations();

            onlineTimer.Elapsed += new ElapsedEventHandler(Online_Elapsed);

            if (DTUList == null)
                DTUList = new List<ModemInfoStruct>();


        }

        public void InitInterface(IUp up, IDown down, IUBatch udisk, IFlashBatch flash, ISoil soil)
        {
            this.Up = up;
            this.Down = down;
            this.UBatch = udisk;
            this.FlashBatch = flash;
            this.Soil = soil;
        }

        public void InitStations(List<CEntityStation> stations)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool GetStarted()
        {
            return isStart;
        }

        public int GetCurrentPort()
        {
            return TcpSocketPort;
        }

        public List<ModemInfoStruct> getDTUList()
        {
            List<ModemInfoStruct> DTUList = new List<ModemInfoStruct>();
            if(ClientSessionTable != null)
            {
                foreach (TSession session in _sessionTable.Values)
                {
                    ModemInfoStruct dtu = new ModemInfoStruct();
                    foreach (TransparentHelper transparent in m_listTransparents)
                    {
                        if(transparent.sessionId == session.ID.ToString())
                        {
                            
                            
                            dtu.m_modemId = uint.Parse(transparent.stationId);
                            //string a = transparent.stationId;
                        }
                    }
                    dtu.m_dynip = System.Text.Encoding.Default.GetBytes(session.IP);
                    dtu.m_conn_time = (uint)ConvertDateTimeInt(session.LoginTime);
                    dtu.m_refresh_time = (uint)ConvertDateTimeInt(session.LastDataReceivedTime);
                    dtu.m_phoneno = null;
                    DTUList.Add(dtu);
                }
            }
            
            
            return DTUList;
        }
        //DateTime转
        
        #endregion
    }
}