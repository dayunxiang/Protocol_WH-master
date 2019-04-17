/************************************************************************************
* Copyright (c) 2019 All Rights Reserved.
*命名空间：Protocol.Channel.Transparen
*文件名： TCPHelper
*创建人： XXX
*创建时间：2019-4-2 8:48:40
*描述
*=====================================================================
*修改标记
*修改时间：2019-4-2 8:48:40
*修改人：XXX
*描述：
************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Protocol.Channel.Transparen
{
    public class TCPHelper
    {
        /// <summary>
        /// 发送下行信息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool sendMsg(string ip,string port,string msg)
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Parse(ip), int.Parse(port));

            NetworkStream ntwStream = tcpClient.GetStream();
            if (ntwStream.CanWrite)
            {
                string fff = ascii2Hex(msg);
                byte[] bytSend = strToToHexByte(ascii2Hex(msg));
                ntwStream.Write(bytSend, 0, bytSend.Length);
            }
            else
            {
                ntwStream.Close();
                tcpClient.Close();
                return false;
            }

            ntwStream.Close();
            tcpClient.Close();
            return true;
        }
        /// <summary>
        /// ascii转16进制
        /// </summary>
        /// <param name="downGram"></param>
        /// <returns></returns>
        public static string ascii2Hex(string downGram)
        {
            byte[] tmp = System.Text.ASCIIEncoding.Default.GetBytes(downGram);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in tmp)
            {
                sb.Append(b.ToString("x") + " ");
            }
            return sb.ToString();


        }
        /// <summary>
        /// 16进制字符串转byte[]
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);
            return returnBytes;
        }


        public static string HexStringToASCII(string hexstring)
        {
            byte[] bt = HexStringToBinary(hexstring);
            string lin = "";
            for (int i = 0; i < bt.Length; i++)
            {
                lin = lin + bt[i] + " ";
            }


            string[] ss = lin.Trim().Split(new char[] { ' ' });
            char[] c = new char[ss.Length];
            int a;
            for (int i = 0; i < c.Length; i++)
            {
                a = Convert.ToInt32(ss[i]);
                c[i] = Convert.ToChar(a);
            }

            string b = new string(c);
            return b;
        }
        public static byte[] HexStringToBinary(string hexstring)
        {

            string[] tmpary = hexstring.Trim().Split(' ');
            byte[] buff = new byte[tmpary.Length];
            for (int i = 0; i < buff.Length; i++)
            {
                buff[i] = Convert.ToByte(tmpary[i], 16);
            }
            return buff;
        }


    }
}