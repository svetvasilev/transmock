using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TransMock.TestUtils
{
    public static class MessagePropertyValidator
    {
        public static void ValidatePromotedProperty(
            KeyValuePair<XmlQualifiedName, object> promotedProperty,
            string expectedNamespace,
            string expectedName,
            string expectedValue)
        {
            Assert.AreEqual(expectedNamespace,
                promotedProperty.Key.Namespace,
                "The promoted property namespace differ");

            Assert.AreEqual(expectedName,
                promotedProperty.Key.Name,
                "The promoted property namespace differ");

            Assert.AreEqual(expectedValue,
                promotedProperty.Value,
                "The value of the promoted property differ");
        }
    }
}
