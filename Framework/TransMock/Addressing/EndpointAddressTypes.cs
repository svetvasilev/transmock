using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Addressing
{
    /// <summary>
    /// Enforces one way receive behavior
    /// </summary>
    public abstract class EndpointAddress
    {
        protected EndpointAddress()
        {

        }

        public string Value { get; protected set; }

        public abstract string Capabilities();        
    }
    /// <summary>
    /// Enforces one way receive behavior
    /// </summary>
    public class OneWayReceiveAddress : EndpointAddress
    {
        public OneWayReceiveAddress(string address)
        {
            this.Value = address;
        }

        public override string Capabilities()
        {
            return "Represents address of a one way receive endpoint";
        }
    }

    /// <summary>
    /// Type for enforcing one way send address behavior
    /// </summary>
    public class OneWaySendAddress : EndpointAddress
    {
        public OneWaySendAddress(string address)
        {
            this.Value = address;
        }

        public override string Capabilities()
        {
            return "Represents address of a one way send endpoint";
        }
    }

    /// <summary>
    /// Type for enforcing one way send address behavior
    /// </summary>
    public class TwoWayReceiveAddress : EndpointAddress
    {
        public TwoWayReceiveAddress(string address)
        {
            this.Value = address;
        }

        public override string Capabilities()
        {
            return "Represents address of a 2 way receive endpoint";
        }
    }

    /// <summary>
    /// Type for enforcing a 2 way send address behavior
    /// </summary>
    public class TwoWaySendAddress : EndpointAddress
    {
        public TwoWaySendAddress(string address)
        {
            this.Value = address;
        }

        public override string Capabilities()
        {
            return "Represents address of a 2 way send endpoint";
        }
    }
}
