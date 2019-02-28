using System.Runtime.InteropServices;

namespace Protocol.Channel.Interface
{
    public struct ModemDataStruct
    {
        public uint m_modemId;		     	    //Modem模块的ID号
        public uint m_recv_time;				//接收到数据包的时间
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1451)]
        public byte[] m_data_buf;               //存储接收到的数据
        public ushort m_data_len;				//接收到的数据包长度
        public byte m_data_type;	          	//接收到的数据包类型,
                                                //	0x01：用户数据包 
                                                //	0x02：对控制命令帧的回应
    }
}
