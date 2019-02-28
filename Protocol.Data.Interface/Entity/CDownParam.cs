
namespace Protocol.Data.Interface
{
    public enum CDownParam
    {
        /// <summary>
        /// 03 时钟
        /// </summary>
        Clock = 03,

        /// <summary>
        /// 04 常规状态
        /// </summary>
        NormalState = 04,

        /// <summary>
        /// 05 电压
        /// </summary>
        Voltage = 05,

        /// <summary>
        /// 08 站号
        /// </summary>
        StationCmdID = 08,

        /// <summary>
        /// 14 对时选择
        /// </summary>
        TimeChoice = 14,

        /// <summary>
        /// 24 定时段次
        /// </summary>
        TimePeriod = 24,

        /// <summary>
        /// 20 工作状态
        /// </summary>
        WorkStatus = 20,

        /// <summary>
        /// 19 版本号
        /// </summary>
        VersionNum = 19,

        /// <summary>
        /// 27 主备信道
        /// </summary>
        StandbyChannel = 27,

        /// <summary>
        /// 28 电话号码
        /// </summary>
        TeleNum = 28,

        /// <summary>
        /// 37 振铃次数
        /// </summary>
        RingsNum = 37,

        /// <summary>
        /// 49 目的地手机号码
        /// </summary>
        DestPhoneNum = 49,

        /// <summary>
        /// 15 终端机号
        /// </summary>
        TerminalNum = 15,

        /// <summary>
        /// 09 响应波束
        /// </summary>
        RespBeam = 09,

        /// <summary>
        /// 16 平均时间
        /// </summary>
        AvegTime = 16,

        /// <summary>
        /// 10 雨量加报值
        /// </summary>
        RainPlusReportedValue = 10,

        /// <summary>
        /// 62 KC
        /// </summary>
        KC = 62,

        /// <summary>
        /// 02 雨量
        /// </summary>
        Rain = 02,

        /// <summary>
        /// 12 水位
        /// </summary>
        Water = 12,

        /// <summary>
        /// 06 水位加报值
        /// </summary>
        WaterPlusReportedValue = 06,

        /// <summary>
        /// 11 采集段次选择
        /// </summary>
        SelectCollectionParagraphs = 11,

        /// <summary>
        /// 07 测站类型
        /// </summary>
        StationType = 07
    }
}
