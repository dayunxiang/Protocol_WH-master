using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Protocol.Data.Interface
{
    public class CReportStruct
    {
        public string Sid { get; set; }
        public string Type { get; set; }
        public string RType { get; set; }
        public string SType { get; set; }
        public DateTime RecvTime { get; set; }
        public List<CReportData> Datas;


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("站号   " + Sid);
            sb.AppendLine("类别   " + Type);
            sb.AppendLine("报类   " + RType);
            sb.AppendLine("站类   " + SType);

            if (Datas != null && Datas.Count() > 0)
            {
                foreach (var data in Datas)
                {
                    sb.AppendLine(data.ToString());
                }
            }
            return sb.ToString();
        }
    }
}
