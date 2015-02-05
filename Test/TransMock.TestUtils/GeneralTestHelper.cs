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

using System.Xml;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;

namespace TransMock.TestUtils
{
    /// <summary>
    /// 
    /// </summary>
    public static class GeneralTestHelper
    {
        /// <summary>
        /// Extracts the message body as a string
        /// </summary>
        /// <param name="msg">The message from which the body will be extracted</param>        
        /// <returns>The bytes of the message body</returns>
        public static byte[] GetBodyAsBytes(Message msg)
        {
            XmlDictionaryReader xdr = msg.GetReaderAtBodyContents();
            xdr.ReadStartElement("MessageContent");

            return xdr.ReadContentAsBase64();
        }

        /// <summary>
        /// Extracts the message body as a string
        /// </summary>
        /// <param name="msg">The message from which the body will be extracted</param>
        /// <param name="encoding">The encoding in which the body is expected to be</param>
        /// <returns>The message body in string representation</returns>
        public static string GetBodyAsString(Message msg, Encoding encoding)
        {
            XmlDictionaryReader xdr = msg.GetReaderAtBodyContents();
            xdr.ReadStartElement("MessageContent");
            return encoding.GetString(xdr.ReadContentAsBase64());            
        }

        /// <summary>
        /// Extracts the message body as a string
        /// </summary>
        /// <param name="msg">The message from which the body will be extracted</param>
        /// <param name="encoding">The encoding in which the body is expected to be</param>
        /// <returns>The message body in string representation</returns>
        public static string GetResponseBodyAsString(Message msg, Encoding encoding)
        {
            XmlDictionaryReader xdr = msg.GetReaderAtBodyContents();
            xdr.ReadStartElement("MessageContent");

            byte[] bodyBytes = xdr.ReadContentAsBase64();

            return encoding.GetString(bodyBytes, 0, bodyBytes.Length - 2);
        }

        /// <summary>
        /// Gets message contents from a byte array
        /// </summary>
        /// <param name="inBuffer">The byte array containing the message</param>
        /// <param name="bytesCountRead">The number of bytes to read from the beginning of the array</param>
        /// <param name="encoding">The encoding in which the string should be represented</param>
        /// <returns>The string representation of the inBuffer byte array</returns>
        public static string GetMessageFromArray(byte[] inBuffer, int bytesCountRead, Encoding encoding)
        {
            string receivedResponseXml = encoding.GetString(
                Convert.FromBase64String(Convert.ToBase64String(inBuffer, 0, bytesCountRead)));

            return receivedResponseXml;
        }

        /// <summary>
        /// Creates a WCF message with a base64 encoded body
        /// </summary>
        /// <param name="bodyContents">The body contents</param>
        /// <param name="encoding">The encoding to be used for converting to bytes</param>
        /// <returns>WCF message with a base64 encoded body</returns>
        public static Message CreateMessageWithBase64EncodedBody(string bodyContents, Encoding encoding)
        {
            Message responseMessage = Message.CreateMessage(MessageVersion.Default, 
                string.Empty,
                Convert.ToBase64String(encoding.GetBytes(bodyContents)));

            return responseMessage;
        }

        /// <summary>
        /// Creates a WCF message with a base64 encoded body
        /// </summary>
        /// <param name="bodyContents">The body contents as an array of bytes</param>        
        /// <returns>WCF message with a base64 encoded body</returns>
        public static Message CreateMessageWithBase64EncodedBody(byte[] bodyContents)
        {
            Message responseMessage = Message.CreateMessage(MessageVersion.Default,
                string.Empty,
                Convert.ToBase64String(bodyContents));

            return responseMessage;
        }

        /// <summary>
        /// Creates a WCF message with empty body
        /// </summary>
        /// <returns>WCF message with empty body</returns>
        public static Message CreateMessageWithEmptyBody()
        {
            Message responseMessage = Message.CreateMessage(MessageVersion.Default,
                string.Empty);
            return responseMessage;
        }

        /// <summary>
        /// Calculates a MD5 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file for which the hash should be calculated</param>
        /// <returns>Base64 encoded string of the hash</returns>
        public static string CalculateFileHash(string filePath)
        {
            using (System.Security.Cryptography.MD5 md5Hash =
                System.Security.Cryptography.MD5.Create())
            {
                return Convert.ToBase64String(
                    md5Hash.ComputeHash(System.IO.File.ReadAllBytes(filePath)));
            }
        }

        /// <summary>
        /// Calculates a MD5 hash for an array of bytes
        /// </summary>
        /// <param name="contents">The content bytes for which the hash should be calculated</param>
        /// <returns>Base64 encoded string of the hash</returns>
        public static string CalculateBytesHash(byte[] contents)
        {
            using (System.Security.Cryptography.MD5 md5Hash =
                System.Security.Cryptography.MD5.Create())
            {
                return Convert.ToBase64String(md5Hash.ComputeHash(contents));
            }
        }
    }
}
