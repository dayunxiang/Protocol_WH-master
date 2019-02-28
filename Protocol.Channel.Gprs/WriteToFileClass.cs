using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Protocol.Channel.Gprs
{
    public class WriteToFileClass
    {
        // 信息列表的互斥量
        private Mutex m_mutexListInfo;

        // 文件的互斥量
        private Mutex m_mutexWriteToFile;

        // 文件夹名
        private string m_strLogFileName;
        public WriteToFileClass(string filename)
        {
            //初始化互斥量
            m_mutexListInfo = new Mutex();
            m_mutexWriteToFile = new Mutex();

            m_strLogFileName = filename;
        }
        // 写入文件
        public void WriteInfoToFile(Object Objectstr)
        {
            string str = Objectstr.ToString();
           // List<Hydrology.Entity.CTextInfo> listInfo = null;
            //m_mutexListInfo.WaitOne();
            //if (m_listTextInfo.Count > 0)
            //{
            //    listInfo = m_listTextInfo;
            //    m_listTextInfo = new List<CTextInfo>();
            //}
            //else
            //{
            //    // 没有任何东西可以写入
            //    m_mutexListInfo.ReleaseMutex();
            //    return;
            //}
            //m_mutexListInfo.ReleaseMutex();

            try
            {
                m_mutexWriteToFile.WaitOne();
                // 判断log文件夹是否存在
                if (!Directory.Exists(m_strLogFileName))
                {
                    // 创建文件夹
                    Directory.CreateDirectory(m_strLogFileName);
                }
                string filename = "ReceivedLog" + DateTime.Now.ToString("yyyyMMddHH") + ".log";
                string path = m_strLogFileName + "/" + filename;
                if (!File.Exists(path))
                {
                    // 不存在文件，新建一个
                    FileStream fs = new FileStream(path, FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    //foreach (CTextInfo info in listInfo)
                    //{
                        //开始写入
                    sw.WriteLine(String.Format("{0}  {1}", DateTime.Now.ToString("HH:mm:ss"), str.TrimEnd()));
                    //}
                    //清空缓冲区
                    sw.Flush();
                    //关闭流
                    sw.Close();
                    fs.Close();
                }
                else
                {
                    // 添加到现有文件
                    FileStream fs = new FileStream(path, FileMode.Append);
                    StreamWriter sw = new StreamWriter(fs);
                    //开始写入
                    //foreach (CTextInfo info in listInfo)
                    //{
                        //开始写入
                    sw.WriteLine(String.Format("{0}  {1}", DateTime.Now.ToString("HH:mm:ss"), str));
                    //}
                    //清空缓冲区
                    sw.Flush();
                    //关闭流
                    sw.Close();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                m_mutexWriteToFile.ReleaseMutex();
            }


        }
    }
}
