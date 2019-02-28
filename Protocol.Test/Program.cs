using System;
using Protocol.Channel.Interface;
//using Protocol.Channel.HDGprs;
using Protocol.Channel.Gprs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Protocol.Data.Lib;
//using Hydrology.Entity;

namespace Protocol.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //string msg = "$70521K02021404010800012305020800012305021605012405021605012705021605012805021605012905021605013005\r";
            //CBatchStruct batch = new CBatchStruct();
            //new FlashBatchTrans().Parse(msg, out batch);

            //CReportStruct report = new CReportStruct();
            //new UpParser().Parse("$19981G2113140403225600234500011378", out report);

            //CEntitySoilData soil = new CEntitySoilData();
            //CReportStruct report = new CReportStruct();

            ////new SoilParser().Parse("#S1209110500705141749 000.87001.06000.980/ 10000.76000.82", out soil, out report);

            ////new SoilParser().Parse("#S1209110500705141749 000.87001.06000.98000.76000.82\r\n", out soil, out report);
            //new SoilParser().Parse("$70521G25142523453214\r\n", out soil, out report);
            //new SoilParser().Parse("$70521G2217140506171712345600011256000100020003\r\n", out soil, out report);
            //Console.WriteLine("Program End ...");
            ////new SoilParser().Parse("$70521G221710101010101234560001125600010002000300040005\r\n", out soil, out report);
            //IHDGprs hd = new HDGpesParser();
            //hd.DSStartService(9002);
            ////IGprs gp = new GprsParser();
            ////gp.DSStartService(9002);
               
            Console.ReadKey();
        }
    }
}
