using System.Runtime.InteropServices;

namespace Protocol.Channel.Interface
{
    public struct ModemInfoStruct
    {
        public uint m_modemId;              //Modem模块的ID号
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] m_phoneno;            //Modem的11位电话号码，必须以'\0'字符结尾  
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_dynip;              //Modem的4位动态ip地址   
        public uint m_conn_time;            //Modem模块最后一次建立TCP连接的时间 
        public uint m_refresh_time;         //Modem模块最后一次收发数据的时间     
    }
}
