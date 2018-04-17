using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GMTools
{
    class CsvLoader
    {
        public CsvLoader()
        {
            m_CsvData = new Dictionary<int, Dictionary<string, string>>();
            m_CsvHeader = new List<string>();
        }

        /// <summary>
        /// 读取到的csv数据
        /// </summary>
        private Dictionary<int, Dictionary<string, string>> m_CsvData;

        /// <summary>
        /// 读取到的csv数据，提供外部使用
        /// </summary>
        public Dictionary<int, Dictionary<string, string>> CsvData
        {
            set { }
            get { return m_CsvData; }
        }

        /// <summary>
        /// csv字段
        /// </summary>
        private List<string> m_CsvHeader;

        /// <summary>
        /// csv所在路径
        /// </summary>
        private string m_CsvPatch = ".\\ServerList.csv";

        /// <summary>
        /// 加载csv数据
        /// </summary>
        public bool LoadData()
        {
            try
            {
                StreamReader sr = new StreamReader(m_CsvPatch, Encoding.Default);
                try
                {
                    var parser = new CsvParser(sr);
                    int nIndex = 1;
                    var headerrow = parser.Read();
                    if (headerrow != null)
                    {
                        foreach (string element in headerrow)
                        {
                            m_CsvHeader.Add(element);
                        }
                    }
                    while (true)
                    {
                        var row = parser.Read();
                        if (row != null)
                        {
                            Dictionary<string, string> list = new Dictionary<string, string>();
                            int i = 0;
                            foreach (string element in row)
                            {
                                list.Add(m_CsvHeader[i], element);
                                ++i;
                            }

                            m_CsvData.Add(nIndex, list);
                            ++nIndex;
                        }
                        else
                            break;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 添加并保存csv数据
        /// </summary>
        public void AddData(string ip, string port, int index)
        {
            try
            {
                Dictionary<string, string> data = new Dictionary<string, string>
                {
                    ["IP"] = ip,
                    ["PORT"] = port
                };
                m_CsvData[index] = data;

                StreamWriter sr = new StreamWriter(m_CsvPatch, false, Encoding.Default);
                var csv = new CsvWriter(sr);
                List<string> list = new List<string>();

                foreach (var header in m_CsvHeader)
                {
                    list.Add(header);
                }

                foreach (var csvdata in m_CsvData)
                {
                    Dictionary<string, string> value = csvdata.Value;
                    for (int i = 0; i < m_CsvHeader.Count; ++i)
                    {
                        value.TryGetValue(m_CsvHeader[i], out string ret);
                        if (ret != null)
                        {
                            list.Add(ret);
                        }
                    }
                }

                int count = 0;
                foreach (var item in list)
                {
                    csv.WriteField(item);
                    ++count;
                    if (count == m_CsvHeader.Count)
                    {
                        count = 0;
                        csv.NextRecord();
                    }
                }
                csv.Flush();
                sr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }
    }
}
