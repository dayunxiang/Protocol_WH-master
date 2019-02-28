using Hydrology.Entity;
using System;
using System.Collections.Generic;
using Hydrology.DBManager;
using System.Linq;
using System.Text;
using Hydrology.DBManager.Interface;
using Hydrology.DBManager.DB.SQLServer;
using Protocol.Data.GY;
using Protocol.Data.Interface;
using Entity.Protocol.Channel;

namespace Protocol.Channer.Router
{
   public class Channle2Data
    {
        #region 成员变量
        private string dataProtocol;
        private string HexOrASCII;
        private IStationProxy m_proxyStation;
        private ISoilStationProxy m_proxySoliStation;
        private List<CEntityStation> m_listStations;
        private List<CEntitySoilStation> m_listSoillStations;

        private Dictionary<string, string> stationGprsMap;
        private Dictionary<string, string> stationGsmMap;
        private Dictionary<string, string> stationBeidouMap;

        private Dictionary<string, string> hexGprsMap;
        private Dictionary<string, string> hexGsmMap;
        private Dictionary<string, string> hexBeidouMap;

        public static Boolean isUpdated;  //用于标记站点信息是否已经更新

        IHandle gyDataHandle = new GYDataHandle();
        
        #endregion

        /// <summary>
        /// 构造方法 初始化成员变量
        /// </summary>
        public Channle2Data()
        {
            m_proxyStation = new CSQLStation();
            m_proxySoliStation = new CSQLSoilStation();
            m_listStations = new List<CEntityStation>();
            m_listSoillStations = new List<CEntitySoilStation>();
            stationGprsMap = new CDictionary<string, string>();
            stationGsmMap = new CDictionary<string, string>();
            stationBeidouMap = new CDictionary<string, string>();

            hexGprsMap = new CDictionary<string, string>();
            hexGsmMap = new CDictionary<string, string>();
            hexBeidouMap = new CDictionary<string, string>();

            dataProtocol = "";
            HexOrASCII = "";
            //初始话站点信息没有更新
            isUpdated = false;

            //初始化站点map
            this.initStations();
        }
        /// <summary>
        /// 站点和数据协议的对应
        /// 1.如果是gprs信道，则关联gprs号和数据协议的对应
        /// 2.如果是gsm信道，则关联gsm号码和数据协议的对应
        /// 3.如果是beidou信道，则关联beidou号码和数据协议的对应
        /// </summary>
        /// <param name="channnel"></param>
        private void initStations()
        {
            m_listStations = m_proxyStation.QueryAll();
            m_listSoillStations = m_proxySoliStation.QueryAllSoilStation();
            //水雨情、墒情中gprs号码 gsm号码 北斗号码都是不重复的
            //1.初始化水情信息对应
            for(int i = 0; i < m_listStations.Count; i++)
            {
                CEntityStation station = new CEntityStation();
                station = m_listStations[i];
                if(station.GPRS != null && station.GPRS.Trim().Length > 0)
                {
                    stationGprsMap[station.GPRS.Trim()] = station.Datapotocol;
                    hexGprsMap[station.GPRS.Trim()] = "Hex"; // 或者ASCII
    }
                if(station.GSM != null && station.GSM.Trim().Length > 0)
                {
                    stationGsmMap[station.GSM.Trim()] = station.Datapotocol;
                    hexGsmMap[station.GPRS.Trim()] = "Hex"; // 或者ASCII
                }
                if(station.BDSatellite != null && station.BDSatellite.Trim().Length > 0)
                {
                    stationBeidouMap[station.BDSatellite.Trim()] = station.Datapotocol;
                    hexBeidouMap[station.BDSatellite.Trim()] = "Hex"; // 或者ASCII
                }
            }
            //2.初始化墒情信息对应
            for (int i = 0; i < m_listSoillStations.Count; i++)
            {
                CEntitySoilStation soilStation = new CEntitySoilStation();
                soilStation = m_listSoillStations[i];
                if (soilStation.GPRS != null && soilStation.GPRS.Trim().Length > 0)
                {
                    stationGprsMap[soilStation.GPRS.Trim()] = soilStation.Datapotocol;
                    hexGprsMap[soilStation.GPRS.Trim()] = "Hex"; // 或者ASCII
                }
                if (soilStation.GSM != null && soilStation.GSM.Trim().Length > 0)
                {
                    stationGsmMap[soilStation.GSM.Trim()] = soilStation.Datapotocol;
                    hexGsmMap[soilStation.GSM.Trim()] = "Hex"; // 或者ASCII
                }
                if (soilStation.BDSatellite != null && soilStation.BDSatellite.Trim().Length > 0)
                {
                    stationBeidouMap[soilStation.GPRS.Trim()] = soilStation.Datapotocol;
                    hexBeidouMap[soilStation.BDSatellite.Trim()] = "Hex"; // 或者ASCII
                }
            }
        }

        public Dictionary<string, Object> commonHandle(CRouter router,string channnel)
        {
            Dictionary<string, Object> resMap = new Dictionary<string, object>();
            //判定报文格式是16进制还是ASCII
            
                // 如果是gprs信道，则根据gprs号码获取数据协议
                if (isUpdated)
            {
                initStations();
            }
            if (channnel == "transparent")
            {
                //如果站点信息进行了更新，则重新获取站点map

                ////获取站点对应的数据协议，如果不存在则不做处理直接返回
                //if (stationGprsMap.ContainsKey(router.dutid))
                //{
                //    dataProtocol = stationGprsMap[router.dutid];
                //    HexOrASCII = hexGprsMap[router.dutid];
                //}
                //else
                //{
                //    return null;
                //}
                dataProtocol = "GY";
                HexOrASCII = "ASCII";
            }
            if (channnel == "hdgprs" || channnel == "sxgprs")
            {
                //如果站点信息进行了更新，则重新获取站点map

                //获取站点对应的数据协议，如果不存在则不做处理直接返回
                if (stationGprsMap.ContainsKey(router.dutid))
                {
                    dataProtocol = stationGprsMap[router.dutid];
                    HexOrASCII = hexGprsMap[router.dutid];
                }
                else
                {
                    return null;
                }
            }
            if (channnel == "gsm")
            {
                //如果站点信息进行了更新，则重新获取站点map

                //获取站点对应的数据协议，如果不存在则不做处理直接返回
                if (stationGsmMap.ContainsKey(router.dutid))
                {
                    dataProtocol = stationGsmMap[router.dutid];
                    HexOrASCII = hexGsmMap[router.dutid];
                }
                else
                {
                    return null;
                }
            }
            if (channnel == "beidou" || channnel == "beidou500")
            {
                //如果站点信息进行了更新，则重新获取站点map

                //获取站点对应的数据协议，如果不存在则不做处理直接返回
                if (stationBeidouMap.ContainsKey(router.dutid))
                {
                    dataProtocol = stationBeidouMap[router.dutid];
                    HexOrASCII = hexBeidouMap[router.dutid];
                }
                else
                {
                    return null;
                }
            }
            //单数据协议
            if (!dataProtocol.Contains(","))
                {
                    switch (dataProtocol)
                    {
                        case "LN":
                            break;
                        case "YN":
                            break;
                        case "GY":
                            resMap = gyDataHandle.getHandledData(router, HexOrASCII);
                            break;
                        case "ZYJBX":
                            break;
                        default:
                            break;
                    }
                }else             
                {
                   // 多数据协议
                   //TODO

                }

            return resMap;
            }
        }
}
