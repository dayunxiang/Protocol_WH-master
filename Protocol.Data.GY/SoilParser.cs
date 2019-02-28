using Protocol.Data.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydrology.Entity;

namespace Protocol.Data.GY
{
    class SoilParser : ISoil
    {
        public string BuildQuery()
        {
            throw new NotImplementedException();
        }

        public bool Parse(string resp, out CEntitySoilData soil, out CReportStruct report)
        {
            throw new NotImplementedException();
        }
    }
}
