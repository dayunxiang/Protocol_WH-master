using System;
using System.Collections.Generic;

namespace Protocol.Data.Interface
{
    public class CBatchStruct
    {
        public String StationID;
        public String Cmd;
        public String StationType;  //  01为水位 02为雨量
        public String TransType;    //  03为按小时传 02为按天传,只有Flash传输时使用，U盘传输时不使用此字段
        public List<CTimeAndData> Datas;
    }
    public class CTimeAndData
    {
        public DateTime Time;
        public String Data;
    }
}
