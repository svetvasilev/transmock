using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// 
    /// </summary>
    public class FileWriter : IFileWriter
    {
        public void WriteTextFile(string path, string content)
        {
            using (FileStream classFileStream = File.Open(path,
                     FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(classFileStream, Encoding.UTF8))
                {
                    sw.Write(content);
                    sw.Flush();
                }
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public interface IFileWriter
    {
        void WriteTextFile(string path, string contents);
    }
}
