using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TransMock.Addressing;

namespace TransMock.Tests.BTS2016
{
    public class ComplexFlowMockAddresses : EndpointAddress
    {
        public TwoWayReceiveAddress Receive_Test_2Way
        {
            get
            {
                return new TwoWayReceiveAddress("mock://localhost/Receive_Test_2Way");
            }
        }

        public TwoWaySendAddress Send_Test_2Way
        {
            get
            {
                return new TwoWaySendAddress("mock://localhost/Send_Test_2Way");
            }
        }

        public TwoWaySendAddress Send_Test_2Way2
        {
            get
            {
                return new TwoWaySendAddress("mock://localhost/Send_Test_2Way2");
            }
        }
    }
}
