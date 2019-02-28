using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Hydrology.Entity;
using System.Threading;

namespace Protocol.Channel.Gprs
{
    internal class DTUdll
    {
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool DSStartService(ushort uiListenPort);
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool OpenListenPort(ushort uiListenPort);
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool DSStopService();
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern int DSGetModemCount();
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool DSGetModemByPosition(uint pos, ref ModemInfoStruct pModemInfo);
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool DSGetNextData(ref ModemDataStruct pDataStruct, ushort waitseconds);
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool DSSendData(uint modemId, ushort len, byte[] buf);
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern bool DSSendControl(uint modemId, ushort len, byte[] buf);
        [DllImport(@"D:\lib\gprsdll.dll")]
        private static extern void DSGetLastError(System.Text.StringBuilder str, int nMaxBufSize);

        private DTUdll()
        {
        }

        private static DTUdll _instance;
        public static DTUdll Instance
        {
            get
            {
                if (_instance == null) _instance = new DTUdll();
                return _instance;
            }
        }

        private bool _started = false;
        public bool Started
        {
            private set
            {
                _started = value;
            }
            get
            {
                return _started;
            }
        }

        public string _lastError;
        public string LastError
        {
            private set
            {
                _lastError = value;
            }
            get
            {
                return _lastError;
            }
        }

        private ushort _listenPort = 0;
        public ushort ListenPort
        {
            private set
            {
                _listenPort = value;
            }
            get
            {
                return _listenPort;
            }
        }

        private void GetLastError()
        {
            try
            {
                StringBuilder sb = new StringBuilder(256);
                DSGetLastError(sb, 256);
                LastError = sb.ToString();
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
            }
        }

        public bool StartService(ushort port)
        {
            try
            {
                if (this.Started) throw new Exception("服务已经启动");
                ListenPort = port;
                bool flag = DSStartService(port);
                if (!flag)
                    this.GetLastError();
                else
                    LastError = null;
                this.Started = flag;
                return flag;
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }
        public bool addPort(ushort port)
        {

            bool flag = false;
            try
            {
                flag = OpenListenPort(port);
            }
            catch (Exception e)
            {

            }
            return flag;
        }

        public bool StopService()
        {
            try
            {
                if (!this.Started) throw new Exception("服务尚未启动");
                bool flag = DSStopService();
                if (!flag)
                    this.GetLastError();
                else
                    LastError = null;
                this.Started = !flag;
                return flag;
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }

        public bool GetDTUList(out Dictionary<uint, ModemInfoStruct> dtuList)
        {
            try
            {
                int cnt = DSGetModemCount();
                //System.Diagnostics.Debug.WriteLine("0414Count=" + cnt.ToString() + "      ");
                dtuList = new Dictionary<uint, ModemInfoStruct>();
                for (uint ii = 0; ii < cnt; ii++)
                {
                    ModemInfoStruct dtu = new ModemInfoStruct();
                    bool flag = DSGetModemByPosition(ii, ref dtu);
                    //System.Diagnostics.Debug.WriteLine("0414id=" + dtu.m_modemId + " 0414idTIME  " + dtu.m_refresh_time);
                    if (!flag)
                    {
                        this.GetLastError();
                        return false;
                    }
                    else
                    {
                        if (!dtuList.ContainsKey(dtu.m_modemId))
                        {
                            dtuList.Add(dtu.m_modemId, dtu);
                            //System.Diagnostics.Debug.WriteLine("0414Count=" + dtuList.Count);
                        }

                    }
                }
                LastError = null;
                return true;
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                dtuList = new Dictionary<uint, ModemInfoStruct>();
                return false;
            }
        }

        public int getOnlionCount()
        {
            int flag = -1;
            try
            {
                flag = DSGetModemCount();
                return flag;
            }
            catch (Exception e)
            {
                return flag;
            }
        }

        public bool GetNextData(out ModemDataStruct dat)
        {
            try
            {
                //WriteToFileClass writeClass = new WriteToFileClass();
                //Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                //t.Start("Test" +  "\r\n");
                dat = new ModemDataStruct();
                return DSGetNextData(ref dat, 0);
            }
            catch (Exception e)
            {
                WriteToFileClass writeClass = new WriteToFileClass("Buginfo");
                Thread t = new Thread(new ParameterizedThreadStart(writeClass.WriteInfoToFile));
                t.Start("fuck" + "\r\n");
                dat = new ModemDataStruct();
                return false;
            }
        }

        public bool SendControl(uint id, string text)
        {
            try
            {
                byte[] bts = UnicodeEncoding.Default.GetBytes(text + "\r");
                // 采用普通控制方式, 请屏蔽专用控制方式内容
                //return this.SendHex(id, bts); 
                // 采用专用控制方式, 请屏蔽普通控制方式内容
                if (DSSendControl(id, (ushort)bts.Length, bts))
                {
                    LastError = null;
                    return true;
                }
                else
                {
                    this.GetLastError();
                    return false;
                }
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }

        public bool SendText(uint id, string text)
        {
            try
            {
                return this.SendHex(id, UnicodeEncoding.Default.GetBytes(text));
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }

        public bool SendHex(uint id, byte[] bts)
        {
            try
            {
                return this.SendHex(id, bts, (ushort)bts.Length);
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }

        public bool SendHex(uint id, byte[] bts, int startIndex, ushort lenth)
        {
            try
            {
                lenth = (ushort)Math.Min(lenth, bts.Length - startIndex);
                byte[] bsnd = new byte[lenth];
                Array.Copy(bts, startIndex, bsnd, 0, lenth);
                return SendHex(id, bsnd, lenth);
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }

        public bool SendHex(uint id, byte[] bts, ushort lenth)
        {
            try
            {
                if (DSSendData(id, lenth, bts))
                {
                    LastError = null;
                    return true;
                }
                else
                {
                    this.GetLastError();
                    return false;
                }
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
                return false;
            }
        }
    }
}
