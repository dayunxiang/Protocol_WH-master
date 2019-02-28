using Hydrology.Entity;
using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Data.GY
{
    class DownParser : IDown
    {
        public String BuildQuery(string sid, IList<EDownParam> cmds, EChannelType ctype)
        {
            return "";
        }
        public String BuildQuery(string sid, IList<EDownParamGY> cmds, EChannelType ctype)
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(Encoding.ASCII.GetBytes("01"));//  添加首字符
            sb.Append("\u0001");//  测试
            //sb.Append(String.Format("{0:D10}", Int32.Parse(sid.Trim())));//  添加遥测站地址 
            //sb.Append(String.Format("{0:D2}", Int32.Parse(sid.Trim())));//  添加中心站地址
            //sb.Append(String.Format("{0:D4}", Int32.Parse(sid.Trim())));//  添加密码
            //sb.Append(String.Format("{0:D2}", Int32.Parse(sid.Trim())));//  添加功能码
            sb.Append("00");//  添加中心站地址
            sb.Append(String.Format("{0:D10}", Int32.Parse(sid.Trim())));//  添加遥测站地址 
            
            sb.Append("1234");//  添加密码
            sb.Append("45");
            int dataLength = 16;
            string length1 = Convert.ToString(dataLength, 16);
            string flag = "8" + "0" + length1;
            sb.Append(flag);
            sb.Append("\u0002");//  测试
            sb.Append("0000");//  添加下行流水号
            int length = 4;//  指令的长度
            foreach (var cmd in cmds)
            {
                switch (cmd)
                {
                    case EDownParamGY.ontime://  发报时间
                        sb.Append(timeToString()); length += 12; break;
                    case EDownParamGY.oldPwd://  旧密码
                        sb.Append("03");
                        sb.Append(CSpecialChars.BALNK_CHAR);
                        sb.Append("1234");
                        sb.Append(CSpecialChars.BALNK_CHAR); length += 8; break;
                    case EDownParamGY.newPwd://  新密码
                        sb.Append("03");
                        sb.Append(CSpecialChars.BALNK_CHAR);
                        sb.Append("4321"); length += 7; break;
                    case EDownParamGY.memoryReset://  初始化固态存储
                        sb.Append("97"); length += 2; break;
                    case EDownParamGY.timeFrom_To://  时段起止时间
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        sb.Append(CSpecialChars.BALNK_CHAR); length += 17; break;

                    /*case EDownParam.timeTo://  时段结束时间  
                        sb.Append(ProtocolMaps.DownParamMap.FindValue(cmd));
                        sb.Append(CSpecialChars.BALNK_CHAR); length += 9; break;*/

                    case EDownParamGY.DRZ://  1 小时内 5 分钟间隔相对水位
                        sb.Append("DRZ");
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        sb.Append(CSpecialChars.BALNK_CHAR); length += 4; break;
                    case EDownParamGY.DRP://  1 小时内每 5 分钟时段雨量
                        sb.Append("DRP");
                        sb.Append(CSpecialChars.BALNK_CHAR); length += 4; break;
                    case EDownParamGY.Step://  时间步长码                   
                        sb.Append("DR");
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        sb.Append(CSpecialChars.BALNK_CHAR); length += 6; break;
                    case EDownParamGY.basicConfig://  遥测站基本配置读取/修改
                        /*for (var  in ) {
                            sb.Append(cmd);
                            sb.Append(CSpecialChars.BALNK_CHAR);
                            if (ProtocolMaps.DownParamMap.FindValue(cmd) != null)
                            {
                                sb.Append(CSpecialChars.BALNK_CHAR);
                                length += 4 + Int32.Parse(ProtocolMaps.DownParamLengthMap[cmd]);
                            }
                            else
                                length += 3;
                        }*/
                        break;
                    case EDownParamGY.operatingPara://  运行参数读取/修改
                                                  /*for (var  in ){
                                                     sb.Append(cmd);
                                                     sb.Append(CSpecialChars.BALNK_CHAR);
                                                     if (ProtocolMaps.DownParamMap.FindValue(cmd) != null)
                                                     {
                                                         sb.Append(CSpecialChars.BALNK_CHAR);
                                                         length += 4 + Int32.Parse(ProtocolMaps.DownParamLengthMap[cmd]);
                                                     }
                                                     else
                                                         length += 3;
                                                 }*/
                        break;
                    case EDownParamGY.Reset://  恢复出厂设置
                        sb.Append("98"); length += 2; break;
                    case EDownParamGY.ICconfig://  设罝遥测站IC卡状态
                        sb.Append("ZT");
                        sb.Append(CSpecialChars.BALNK_CHAR);
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd)); length += 11; break;
                    case EDownParamGY.pumpCtrl://  控制水泵状态
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        length += Int32.Parse(ProtocolMaps.DownParamLengthMapGY[cmd]); break;
                    case EDownParamGY.valveCtrl://  控制阀门状态
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        length += Int32.Parse(ProtocolMaps.DownParamLengthMapGY[cmd]); break;
                    case EDownParamGY.gateCtrl://  控制闸门状态
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        length += Int32.Parse(ProtocolMaps.DownParamLengthMapGY[cmd]); break;
                    case EDownParamGY.waterYield://  水量定值控制
                        sb.Append(ProtocolMaps.DownParamMapGY.FindValue(cmd));
                        length += 2; break;
                    default:
                        throw new Exception("设置下行指令参数错误");
                }
            }
            //sb.Insert(19, String.Format("{0:D1}", 8));//  添加报文标识
            //length = 10;
            //Insert(20, String.Format("{0:X3}", length));//  添加报文长度
            sb.Append("\u0003");
            string dataMsg = sb.ToString();
            string crcMsg = CRC.ToCRC16(dataMsg, false);
            string resut = dataMsg + crcMsg;
            return resut;
        }

        public String BuildQuery_Batch(string sid, ETrans trans, DateTime beginTime, EChannelType ctype)
        {
            return "";
        }

        public string BuildQuery_Flash(string sid, EStationType stationType, ETrans trans, DateTime beginTime, DateTime endTime, EChannelType ctype)
        {
            throw new NotImplementedException();
        }

        public string BuildQuery_SD(string sid, DateTime beginTime, EChannelType ctype)
        {
            throw new NotImplementedException();
        }

        public string BuildSet(string sid, IList<EDownParam> cmds, CDownConf down, EChannelType ctype)
        {
            throw new NotImplementedException();
        }

        public bool Parse(string resp, out CDownConf downConf)
        {
            throw new NotImplementedException();
        }

        public bool Parse_Batch(string msg, out CBatchStruct batch)
        {
            throw new NotImplementedException();
        }

        public bool Parse_Flash(string msg, EChannelType ctype, out CBatchStruct batch)
        {
            throw new NotImplementedException();
        }

        public bool Parse_SD(string msg, string id, out CSDStruct sd)
        {
            throw new NotImplementedException();
        }
        #region 私有方法
        /// <summary>
        /// 当前时间转为12为string
        /// </summary>
        /// <returns></returns>
        private string timeToString()
        {
            DateTime dt = DateTime.Now;
            int year = dt.Year;
            string yearStr = year.ToString().Substring(2, 2);
            string month = dt.Month.ToString();
            if(month.Length == 1)
            {
                month = "0" + month;
            }
            string day = dt.Day.ToString();
            if(day.Length == 1)
            {
                day = "0" + day;
            }
            String hour = dt.Hour.ToString();
            if(hour.Length == 1)
            {
                hour = "0" + hour;
            }
            String minute = dt.Minute.ToString();
            if(minute.Length == 1)
            {
                minute = "0" + minute;
             }
            String seconds = dt.Second.ToString();
            if(seconds.Length == 1)
            {
                seconds = "0" + seconds; 
            }
            string result = yearStr + month + day + hour + minute + seconds;
            return result;

        }

        #endregion
    }
}
