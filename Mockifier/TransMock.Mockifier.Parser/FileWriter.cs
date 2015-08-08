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
using System.IO;
using System.Linq;
using System.Text;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// Defines the operation of a file writer
    /// </summary>
    public interface IFileWriter
    {
        /// <summary>
        /// Writes the supplied text to a file with the supplied path
        /// </summary>
        /// <param name="path">The path to the file to write to</param>
        /// <param name="contents">The string contents to write to the file</param>
        void WriteTextFile(string path, string contents);
    }

    /// <summary>
    /// Helper class for writing a file to the disk
    /// </summary>
    public class FileWriter : IFileWriter
    {
        /// <summary>
        /// Writes text content to a new file
        /// </summary>
        /// <param name="path">The path to the file to write to</param>
        /// <param name="content">The contents to be written to the file</param>
        public void WriteTextFile(string path, string content)
        {
            FileStream classFileStream = File.Open(
                path,
                FileMode.OpenOrCreate, 
                FileAccess.Write);
           
            using (StreamWriter sw = new StreamWriter(classFileStream, Encoding.UTF8))
            {
                sw.Write(content);
                sw.Flush();
            }           
        }
    }    
}
