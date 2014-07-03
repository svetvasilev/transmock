using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// Contains the names of the various XML node names as constants
    /// </summary>
    public static class XmlNodeNames
    {
        public const string Name = "Name";

        public const string Value = "Value";

        public const string Encoding = "Encoding";

        public const string Operation = "Operation";

        public const string EndpointName = "EndpointName";

        public const string PromotedProperties = "PromotedProperties";

        public const string Property = "Property";

        public const string Address = "Address";

        public const string TransportType = "TransportType";

        public const string TransportTypeData = "TransportTypeData";

        public const string ReceiveHandler = "ReceiveHandler";

        public const string SendHandler = "SendHandler";

        public const string Capabilities = "Capabilities";

        public const string ConfigurationClsid = "ConfigurationClsid";

        public const string IsTwoWay = "IsTwoWay";

        public const string ReceiveLocationTransportType = "ReceiveLocationTransportType";

        public const string ReceiveLocationTransportTypeData = "ReceiveLocationTransportTypeData";
    }
}
