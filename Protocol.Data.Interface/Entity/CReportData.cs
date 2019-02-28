using System;
using System.Text;

namespace Protocol.Data.Interface
{
    public class CReportData
    {
        public DateTime Time { get; set; }
        public int Water { get; set; }
        public int Rain { get; set; }
        public int Voltge { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("数据时标：" + Time.ToShortDateString() + "  " + Time.ToShortTimeString());
            sb.AppendLine("水位   ：" + Water);
            sb.AppendLine("雨量   ：" + Rain);
            sb.AppendLine("电压   ：" + Voltge);
            return sb.ToString();
        }
    }
}
