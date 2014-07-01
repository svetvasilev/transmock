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
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetBodyAsString(Message msg, Encoding encoding)
        {
            XmlDictionaryReader xdr = msg.GetReaderAtBodyContents();
            xdr.ReadStartElement("MessageContent");
            return encoding.GetString(xdr.ReadContentAsBase64());
            //return xdr.ReadOuterXml();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inBuffer"></param>
        /// <param name="bytesCountRead"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetMessageFromArray(byte[] inBuffer, int bytesCountRead, Encoding encoding)
        {
            string receivedResponseXml = encoding.GetString(
                Convert.FromBase64String(Convert.ToBase64String(inBuffer, 0, bytesCountRead)));
            return receivedResponseXml;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="responseXml"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static Message CreateMessageWithBase64EncodedBody(string responseXml, Encoding encoding)
        {
            Message responseMessage = Message.CreateMessage(MessageVersion.Default, 
                string.Empty,
                Convert.ToBase64String(encoding.GetBytes(responseXml)));
            return responseMessage;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Message CreateMessageWithEmptyBody()
        {
            Message responseMessage = Message.CreateMessage(MessageVersion.Default,
                string.Empty, string.Empty);
            return responseMessage;
        }
    }
}
