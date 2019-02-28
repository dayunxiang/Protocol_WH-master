using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Hydrology.Entity;
using Protocol.Channel.Interface;
using Protocol.Data.Interface;

namespace Protocol.Channel.HDGprs
{
    public class HDGpesParser : IHDGprs
    {
        public IDown Down
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

        public IFlashBatch FlashBatch
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

        public ISoil Soil
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

        public IUBatch UBatch
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

        public IUp Up
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

        public event EventHandler<BatchEventArgs> BatchDataReceived;
        public event EventHandler<DownEventArgs> DownDataReceived;
        public event EventHandler<ReceiveErrorEventArgs> ErrorReceived;
        public event EventHandler<SendOrRecvMsgEventArgs> MessageSendCompleted;
        public event EventHandler<CEventSingleArgs<CSerialPortState>> SerialPortStateChanged;
        public event EventHandler<CEventSingleArgs<CEntitySoilData>> SoilDataReceived;
        public event EventHandler<UpEventArgs> UpDataReceived;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void InitInterface(IUp up, IDown down, IUBatch udisk, IFlashBatch flash, ISoil soil)
        {
            throw new NotImplementedException();
        }

        public void InitStations(List<CEntityStation> stations)
        {
            throw new NotImplementedException();
        }

        int IHDGprs.DSStartService(ushort port)
        {
            int started = DTUdll.Instance.StartService(port);
            return started;
        }

        int  DSStartService(ushort port)
        {
            int  started = DTUdll.Instance.StartService(port);
            return started;
        }

       
    }
}
