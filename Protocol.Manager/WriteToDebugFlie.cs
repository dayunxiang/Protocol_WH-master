using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Protocol.Manager
{
    public class WriteToDebugFlie
    {
        // 文件夹名
        private string m_strLogFileName = "DebugLog";

        public void WriteToDebugFile(DebugInfo info)
        {
            try
            {
                // 判断log文件夹是否存在
                if (!Directory.Exists(m_strLogFileName))
                {
                    // 创建文件夹
                    Directory.CreateDirectory(m_strLogFileName);
                }
                string filename = "DebugLog" + DateTime.Now.ToString("yyyyMMddhh") + ".log";
                string path = m_strLogFileName + "/" + filename;
                if (!File.Exists(path))
                {
                    // 不存在文件，新建一个
                    FileStream fs = new FileStream(path, FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    //开始写入
                    sw.WriteLine(String.Format("{0}  {1}", info.Time.ToString("HH:mm:ss"), info.Info));
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
                    sw.WriteLine(String.Format("{0}  {1}", info.Time.ToString("HH:mm:ss"), info.Info));
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
        }

        public class DebugInfo
        {

            /// <summary>
            /// 信息时间
            /// </summary>
            public DateTime Time { get; set; }

            /// <summary>
            /// 信息内容
            /// </summary>
            public String Info { get; set; }
        }
    }
}
