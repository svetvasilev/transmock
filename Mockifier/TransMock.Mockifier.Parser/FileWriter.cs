/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// Helper class for writing a file to the disk
    /// </summary>
    public class FileWriter : IFileWriter
    {
        public void WriteTextFile(string path, string content)
        {
            FileStream classFileStream = File.Open(path,
                     FileMode.OpenOrCreate, FileAccess.Write);
           
            using (StreamWriter sw = new StreamWriter(classFileStream, Encoding.UTF8))
            {
                sw.Write(content);
                sw.Flush();
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
