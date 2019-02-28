using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Channel.Gsm
{
    public class GSMReceive
    {

        public string GetSMSInbox()
        {
            string GSMXMLStr = "";

            string smsId = "240694";
            string smsPw = "lnsswj@2018";

            SMSService.SMSService sMS = new SMSService.SMSService();

            GSMXMLStr = sMS.GetSMSInbox(smsId, smsPw);

            return GSMXMLStr;
        }

        public bool DealSMSDatas(string GSMStr)
        {
            if(GSMStr == "-1")
            {
                return false;
            }else if (GSMStr == "-2")
            {
                return false;
            }

            try
            {

            }catch(Exception e)
            {
                return false;
            }

            return true;
        }
    }
}
