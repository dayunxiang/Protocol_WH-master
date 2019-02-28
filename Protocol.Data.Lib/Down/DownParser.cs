using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Protocol.Data.Interface;

namespace Protocol.Data.Lib
{
    public class DownParser : IDown
    {
        public String BuildQuery(string sid, string type, IList<CDownParam> cmds)
        {
            //  构建发送指令的字符串
            StringBuilder sb = new StringBuilder();
            sb.Append(CSpecialChars.StartCh);
            sb.Append(sid);
            sb.Append(type);

            foreach (var cmd in cmds)
            {
                sb.Append(CSpecialChars.BlankCh);
                sb.Append(String.Format("0:D2", (int)cmd));
            }
            sb.Append(CSpecialChars.EndCh);
            return sb.ToString();
        }

        public String BuildSet(string sid, string type, IList<CDownParam> cmds, CDownConf down)
        {
            throw new NotImplementedException();
        }

        public CDownConf Parse(string resp)
        {
            //  判断是否是合法输入
            Debug.Assert(resp.StartsWith(CSpecialChars.StartCh.ToString()), "数据以非'$'字符开始");
            Debug.Assert(resp.EndsWith(CSpecialChars.EndCh.ToString()), "数据以非'\r'字符结束");

            var result = new CDownConf();

            int startIndex = resp.IndexOf(CSpecialChars.StartCh);
            int endIndex = resp.IndexOf(CSpecialChars.EndCh);

            string data = resp.Substring(startIndex + 1, endIndex - 1);

            var segs = data.Split(CSpecialChars.BlankCh);

            for (int i = 0; i < segs.Count(); i++)
            {
                string value = segs[i].ToString();

                if (i == 0) //  解析站点信息
                {
                    result.StationID = value.Substring(0, 4);
                    result.Type = value.Substring(4, 2);
                }
                else        //  解析数据
                {
                    //  数据分为两部分
                    //  2 Byte 指令
                    //  剩下的为数据
                    int cmd = Int32.Parse(value.Substring(0, 2));
                    string info = value.Substring(2, value.Length - 2);
                    switch (cmd)
                    {

                        case (int)CDownParam.Clock: result.Clock = info; break;
                        case (int)CDownParam.NormalState: result.NormalState = info; break;
                        case (int)CDownParam.Voltage: result.Voltage = info; break;
                        case (int)CDownParam.StationCmdID: result.StationCmdID = info; break;
                        case (int)CDownParam.TimeChoice: result.TimeChoice = info; break;
                        case (int)CDownParam.TimePeriod: result.TimePeriod = info; break;
                        case (int)CDownParam.WorkStatus: result.WorkStatus = info; break;
                        case (int)CDownParam.VersionNum: result.VersionNum = info; break;
                        case (int)CDownParam.StandbyChannel: result.StandbyChannel = info; break;
                        case (int)CDownParam.TeleNum: result.TeleNum = info; break;
                        case (int)CDownParam.RingsNum: result.RingsNum = info; break;
                        case (int)CDownParam.DestPhoneNum: result.DestPhoneNum = info; break;
                        case (int)CDownParam.TerminalNum: result.TerminalNum = info; break;
                        case (int)CDownParam.RespBeam: result.RespBeam = info; break;
                        case (int)CDownParam.AvegTime: result.AvegTime = info; break;
                        case (int)CDownParam.RainPlusReportedValue: result.RainPlusReportedValue = info; break;
                        case (int)CDownParam.KC: result.KC = info; break;
                        case (int)CDownParam.Rain: result.Rain = info; break;
                        case (int)CDownParam.Water: result.Water = info; break;
                        case (int)CDownParam.WaterPlusReportedValue: result.WaterPlusReportedValue = info; break;
                        case (int)CDownParam.SelectCollectionParagraphs: result.SelectCollectionParagraphs = info; break;
                        case (int)CDownParam.StationType: result.StationType = info; break;
                        default: break;
                    }
                }
            }

            return result;
        }
    }
}
