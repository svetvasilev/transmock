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
using System.ServiceModel.Channels;
using System.Xml;

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// This class implements logic for promoting properties in a message context that are related to the original receive adapter that is mocked
    /// </summary>
    internal class AdapterPropertyParser
    {
        private static Dictionary<string, AdapterProperty> wellKnownProperties;

        private Dictionary<string, string> propertiesToPromote;

        #region Constants
        /// <summary>
        /// Namespace used for promoting header properties to the BizTalk message context
        /// </summary>
        const string PropertiesToPromoteKey = "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote";

        /// <summary>
        /// Namespace used for writing header properties to the BizTalk message context
        /// </summary>
        const string PropertiesToWriteKey = "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/WriteToContext";
        #endregion

        public AdapterPropertyParser()
        {
            InitWellKnownProperties();
        }

        /// <summary>
        /// Initializes the parser with the given list of adapter properties
        /// </summary>
        /// <param name="adapterProperties"></param>
        public void Init(string adapterProperties)
        {
            System.Diagnostics.Debug.WriteLine("Init called with adapter properties: " + adapterProperties, 
                "PropertyPromotionParser");

            if (string.IsNullOrEmpty(adapterProperties))
                return;

            //Checking whether the string is properly formatted
            if (!adapterProperties.EndsWith(";"))
            {
                adapterProperties += ";";
            }

            string[] pairsArray = adapterProperties.Split(';');

            System.Diagnostics.Debug.WriteLine("pairsArray has element num of: " + pairsArray.Length,
                "PropertyPromotionParser");

            propertiesToPromote = new Dictionary<string, string>(pairsArray.Length - 1);

            foreach (var s in pairsArray)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    string[] nameValuePair = s.Split('=');
                    propertiesToPromote.Add(nameValuePair[0], nameValuePair[1]);
                }
            }
            
            System.Diagnostics.Debug.WriteLine("Dictionary propertiesToPromote has element number: " + propertiesToPromote.Count, 
                "PropertyPromotionParser");

        }

        /// <summary>
        /// Promotes the already initialized properties to the message context
        /// </summary>
        /// <param name="msg">The message which will get the properties promoted to</param>
        public void PromoteProperties(Message msg)
        {            
            if (propertiesToPromote == null)
            {
                return;
            }

            List<KeyValuePair<XmlQualifiedName, object>> promoteProps = 
                new List<KeyValuePair<XmlQualifiedName, object>>(propertiesToPromote.Keys.Count);

                
            foreach (string propertyName in propertiesToPromote.Keys)
            {
                try
                {

                    var property = wellKnownProperties
                        .Where(k => k.Key == propertyName)
                        .SingleOrDefault();

                    XmlQualifiedName fqPropertyName = new XmlQualifiedName(property.Value.Name, property.Value.Namespace);
                
                    promoteProps.Add(new KeyValuePair<XmlQualifiedName, object>(fqPropertyName, propertiesToPromote[propertyName]));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception thrown in PromoteProperties: " + ex.Message,
                    "PropertyPromotionParser");
                }
            }
                
            if(promoteProps.Count > 0)
                msg.Properties[PropertiesToPromoteKey] = promoteProps;            
        }

        /// <summary>
        /// Clears the list of properties that have been already configured
        /// </summary>
        public void Clear()
        {
            if (propertiesToPromote != null)
            {
                propertiesToPromote.Clear();
                propertiesToPromote = null;
            }
        }

        private void InitWellKnownProperties()
        {
            if (wellKnownProperties != null)
                return;

            wellKnownProperties = new Dictionary<string, AdapterProperty>(){
                //File adapter
                {
                    "FILE.ReceivedFileName", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/file-properties", 
                        Name = "ReceivedFileName"
                    }
                },
                //FTP Adapter
                {
                    "FTP.ReceivedFileName", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/ftp-properties", 
                        Name = "ReceivedFileName"
                    }
                },
                //POP3 adapter
                {
                    "POP3.Subject", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties", 
                        Name = "Subject"
                    }
                },
                {
                    "POP3.From", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties", 
                        Name = "From"
                    }
                },
                {
                    "POP3.To", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties", 
                        Name = "To"
                    }
                },
                {
                    "POP3.ReplyTo", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties", 
                        Name = "ReplyTo"
                    }
                },
                {
                    "POP3.Cc", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties", 
                        Name = "Cc"
                    }
                },
                //MQ-Series adapter
                {
                    "MQMD.ApplIdentityData", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_ApplIdentityData"
                    }
                },                
                {
                    "MQMD.ApplOriginData", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_ApplOriginData"
                    }
                },
                {
                    "MQMD.CorrelId", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_CorrelId"
                    }
                },
                {
                    "MQMD.Encoding", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_Encoding"
                    }
                },
                {
                    "MQMD.Expiry", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_Expiry"
                    }
                },
                {
                    "MQMD.Format", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_Format"
                    }
                },
                {
                    "MQMD.GroupID", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_GroupID"
                    }
                },
                {
                    "MQMD.MsgId", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_MsgId"
                    }
                },
                {
                    "MQMD.MsgSeqNumber", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_MsgSeqNumber"
                    }
                },
                {
                    "MQMD.MsgType", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_MsgType"
                    }
                },
                {
                    "MQMD.Offset", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_Offset"
                    }
                },
                {
                    "MQMD.OriginalLength", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties", 
                        Name = "MQMD_OriginalLength"
                    }
                },
                //MSMQ Adapter
                {
                    "MSMQ.AppSpecific", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", 
                        Name = "AppSpecific"
                    }
                },
                {
                    "MSMQ.CertificateThumbPrint", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", 
                        Name = "CertificateThumbPrint"
                    }
                },
                {
                    "MSMQ.CorrelationId", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", 
                        Name = "CorrelationId"
                    }
                },
                {
                    "MSMQ.Label", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", 
                        Name = "Label"
                    }
                },
                {
                    "MSMQ.Priority", 
                    new AdapterProperty{
                        Namespace = "http://schemas.microsoft.com/BizTalk/2003/msmq-properties", 
                        Name = "Priority"
                    }
                }
            };
            
        }
    }

    /// <summary>
    /// Contains
    /// </summary>
    internal class AdapterProperty
    {
        public string Namespace { get; set; }

        public string Name { get; set; }
    }
}
