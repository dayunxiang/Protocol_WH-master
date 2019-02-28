using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Hydrology.Entity;
using System.Threading;
namespace Protocol.Channel.HDGprs
{
     internal class DTUdll
    {
        [DllImport(@"C:\Users\codergaoming\Desktop\Hydro05\Hydro\bin\Debug\HDgprs.dll")]
        private static extern int start_net_service(IntPtr intPtr, uint wMsg, int nServerPort, string mess);
        //private static extern int start_net_service(string str, uint wMsg, int nServerPort, string mess);
        [DllImport(@"C:\Users\codergaoming\Desktop\Hydro05\Hydro\bin\Debug\HDgprs.dll")]
        private static extern int SetWorkMode(int nWorkMode);
        [DllImport(@"C:\Users\codergaoming\Desktop\Hydro05\Hydro\bin\Debug\HDgprs.dll")]
        private static extern int SelectProtocol(int nProtocol);
        [DllImport(@"HDgprs.dll")]
        private static extern int stop_net_service(string  mess);
        [DllImport(@"gprsdll.dll")]
        private static extern bool DSStartService(ushort uiListenPort);

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
        public int StartService(ushort port)
        {
            //bool fl = DSStartService(port);
            IntPtr p = IntPtr.Zero;
            int a = SetWorkMode(2);
            int b = SelectProtocol(1);
            int flag = -1;
            try
            {
                bool flag1 = DSStartService(port);
                flag = start_net_service(p, 0, 9002, null);
               
                return flag;
            }
            catch (Exception ee)
            {
                return flag;
            }
           
        }

        public int StopService(ushort port)
        {
            int flag = -1;
            try
            {
                flag = stop_net_service(null);
                return flag;
            }
            catch (Exception ee)
            {
                return flag;
            }
        }


    }
}
