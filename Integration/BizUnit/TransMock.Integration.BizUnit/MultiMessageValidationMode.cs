using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Defines the different validation modes for validating multiple messages 
    /// recieved by in the same test step instance
    /// </summary>
    public enum MultiMessageValidationMode
    {
        /// <summary>
        /// This is the default mode where the same validation steps are applied for each message
        /// </summary>
        Serial,
        /// <summary>
        /// This is the mode where for each message a dedicated set of validation steps are applied
        /// </summary>
        Cascading
    }
}
