using System;
using TransMock.Addressing;

namespace TransMock.Tests
{
    internal class TestMockAddresses : EndpointAddress
    {
        public OneWayReceiveAddress ReceiveFirstMessage_FILE
        {
            get { return new OneWayReceiveAddress("mock://localhost/ReceiveFirstMessage_FILE"); }
        }

        public OneWaySendAddress SendFirstMessage_FILE
        {
            get { return new OneWaySendAddress("mock://localhost/SendFirstMessage_FILE"); }
        }

        public TwoWayReceiveAddress TwoWayReceive_WebHTTP
        {
            get { return new TwoWayReceiveAddress("mock://localhost/TwoWayReceive_WebHTTP"); }
        }

        public TwoWaySendAddress TwoWaySend_WebHTTP
        {
            get { return new TwoWaySendAddress("mock://localhost/TwoWaySend_WebHTTP"); }
        }
    }
}