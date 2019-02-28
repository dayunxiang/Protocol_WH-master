using System;

namespace Protocol.Data.Interface
{
    public class CDownConf
    {
        //  测站信息
        public String StationID;
        public String Type;

        /// <summary>
        /// 03 时钟
        /// </summary>
        public string Clock;

        /// <summary>
        /// 04 常规状态
        /// </summary>
        public string NormalState;

        /// <summary>
        /// 05 电压
        /// </summary>
        public string Voltage;

        /// <summary>
        /// 08 站号
        /// </summary>
        public string StationCmdID;

        /// <summary>
        /// 14 对时选择
        /// </summary>
        public string TimeChoice;

        /// <summary>
        /// 24 定时段次
        /// </summary>
        public string TimePeriod;

        /// <summary>
        /// 20 工作状态
        /// </summary>
        public string WorkStatus;

        /// <summary>
        /// 19 版本号
        /// </summary>
        public string VersionNum;


        /// <summary>
        /// 27 主备信道
        /// </summary>
        public string StandbyChannel;

        /// <summary>
        /// 28 电话号码
        /// </summary>
        public string TeleNum;

        /// <summary>
        /// 37 振铃次数
        /// </summary>
        public string RingsNum;

        /// <summary>
        /// 49 目的地手机号码
        /// </summary>
        public string DestPhoneNum;

        /// <summary>
        /// 15 终端机号
        /// </summary>
        public string TerminalNum;

        /// <summary>
        /// 09 响应波束
        /// </summary>
        public string RespBeam;

        /// <summary>
        /// 16 平均时间
        /// </summary>
        public string AvegTime;

        /// <summary>
        /// 10 雨量加报值
        /// </summary>
        public string RainPlusReportedValue;

        /// <summary>
        /// 62 KC
        /// </summary>
        public string KC;

        /// <summary>
        /// 02 雨量
        /// </summary>
        public string Rain;

        /// <summary>
        /// 12 水位
        /// </summary>
        public string Water;

        /// <summary>
        /// 06 水位加报值
        /// </summary>
        public string WaterPlusReportedValue;

        /// <summary>
        /// 11 采集段次选择
        /// </summary>
        public string SelectCollectionParagraphs;

        /// <summary>
        /// 07 测站类型
        /// </summary>
        public string StationType;
    }
}
