using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// Contains basic adapter properties
    /// </summary>
    internal class AdapterProperty
    {
        /// <summary>
        /// Gets or sets the property namespace
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the property name
        /// </summary>
        public string Name { get; set; }
    }
}
