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
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace TransMock.Wcf.Adapter
{
#if NET40
    /// <summary>
    /// This class implements logic for promoting properties in a message context that are related to the original receive adapter that is mocked
    /// </summary>
    internal class AdapterPropertyParser
    {
        #region Constants
        /// <summary>
        /// Namespace used for promoting header properties to the BizTalk message context
        /// </summary>
        private const string PropertiesToPromoteKey = "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote";

        /// <summary>
        /// Namespace used for writing header properties to the BizTalk message context
        /// </summary>
        private const string PropertiesToWriteKey = "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/WriteToContext";

        #endregion

        /// <summary>
        /// Dictionary containing a list of properties to be promoted
        /// </summary>
        private Dictionary<string, string> adapterPropertiesToPromote;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterPropertyParser" /> class
        /// </summary>
        public AdapterPropertyParser()
        {
        }

        /// <summary>
        /// Initializes the parser with the given list of adapter properties
        /// </summary>
        /// <param name="adapterProperties">The properties that are passed through the endpoint configuration</param>
        public void Init(string adapterProperties)
        {
            System.Diagnostics.Debug.WriteLine(
                "Init called with adapter properties: " + adapterProperties,
                "PropertyPromotionParser");

            if (string.IsNullOrEmpty(adapterProperties))
            {
                return;
            }

            // Checking whether the string is properly formatted
            if (!adapterProperties.EndsWith(";", StringComparison.Ordinal))
            {
                adapterProperties += ";";
            }

            string[] pairsArray = adapterProperties.Split(';');

            System.Diagnostics.Debug.WriteLine(
                "pairsArray has element num of: " + pairsArray.Length,
                "PropertyPromotionParser");

            this.adapterPropertiesToPromote = new Dictionary<string, string>(pairsArray.Length - 1);

            foreach (var s in pairsArray)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    string[] nameValuePair = s.Split('=');

                    this.adapterPropertiesToPromote.Add(nameValuePair[0], nameValuePair[1]);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                "Dictionary propertiesToPromote has element number: " + this.adapterPropertiesToPromote.Count,
                "PropertyPromotionParser");
        }

        /// <summary>
        /// Promotes the already initialized properties to the message context
        /// </summary>
        /// <param name="message">The message which will get the properties promoted to</param>
        /// <param name="messagePropertiers">The message level properties that shall be promoted to the inbound message context</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Needed as per design")]
        public void PromoteProperties(Message message, Dictionary<string, string> messageProperties)
        {
            List<KeyValuePair<XmlQualifiedName, object>> promoteProps =
                new List<KeyValuePair<XmlQualifiedName, object>>(3);

            if (this.adapterPropertiesToPromote != null)
            {
                promoteProps.Capacity = this.adapterPropertiesToPromote.Count;
                // Adding adapter level properties                
                foreach (string propertyName in this.adapterPropertiesToPromote.Keys)
                {
                    try
                    {
                        var fullyQualifiedPropertyName = LookupProperty(propertyName);

                        promoteProps.Add(
                            new KeyValuePair<XmlQualifiedName, object>(
                                fullyQualifiedPropertyName,
                                this.adapterPropertiesToPromote[propertyName]));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Exception thrown in PromoteProperties: " + ex.Message,
                            "PropertyPromotionParser");
                    }
                }
            }

            if (messageProperties != null)
            {
                // Adding message level properties
                foreach (var propertyKey in messageProperties.Keys)
                {
                    var fullyQualifiedPropertyName = LookupProperty(propertyKey);

                    KeyValuePair<XmlQualifiedName, object> existingPropertyForPromotion = promoteProps
                        .SingleOrDefault(elm =>
                            elm.Key.Name == fullyQualifiedPropertyName.Name
                            && elm.Key.Name == fullyQualifiedPropertyName.Name);

                    if (!default(KeyValuePair<XmlQualifiedName, object>)
                            .Equals(existingPropertyForPromotion))
                    {
                        // Message level properties override static adapter propertes
                        promoteProps.Remove(existingPropertyForPromotion);
                    }

                    promoteProps.Add(
                        new KeyValuePair<XmlQualifiedName, object>(
                            fullyQualifiedPropertyName,
                            messageProperties[propertyKey]));

                }
            }

            if (promoteProps.Count > 0)
            {
                message.Properties[PropertiesToPromoteKey] = promoteProps;
            }
        }

        /// <summary>
        /// Clears the list of properties that have been already configured
        /// </summary>
        public void Clear()
        {
            if (this.adapterPropertiesToPromote != null)
            {
                this.adapterPropertiesToPromote.Clear();
                this.adapterPropertiesToPromote = null;
            }
        }

        /// <summary>
        /// Looks up a context property by its name
        /// </summary>
        /// <param name="name">The name of the property - shortened syntax <Prefix>.<name> or full type syntax <namespace>#<name></param>
        /// <returns>An instance of the XmlQualifiedName class containing the fully qualified XML property name</returns>
        private XmlQualifiedName LookupProperty(string name)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        "LookupProperty called for property: {0}",
                        name),
                    "TransMock.Wcf.Adapter.AdapterPropertyParser");

                string[] nameParts = null;
                string contextPropertyName = null;

                if (!name.Contains("#"))
                {
                    // Handling custom promoted property case
                    nameParts = name.Split('.');

                    // Activator.CreateInstance("TransMock.Utils", );
                    var utilsAssembly = System.Reflection
                        .Assembly.Load(
                            "TransMock.Utils");

                    var propertyType = utilsAssembly.GetTypes()
                        .Where(t => t.Name == nameParts[0])
                        .SingleOrDefault();

                    contextPropertyName = propertyType
                            .GetProperty(nameParts[1])
                            .GetValue(null,null).ToString();

                    System.Diagnostics.Debug.WriteLine(
                        string.Format(
                            "LookupProperty value found: {0}",
                            contextPropertyName),
                        "TransMock.Wcf.Adapter.AdapterPropertyParser");
                }
                else
                {
                    contextPropertyName = name;
                }

                string[] propertyParts = contextPropertyName.Split('#');

                return new XmlQualifiedName(
                    propertyParts[1], // Property name
                    propertyParts[0]); // Propery namespace

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                        "Exception thrown in LookupProperties: " + ex.Message,
                        "PropertyPromotionParser");
                throw;
            }
        }
    }
#endif
}
