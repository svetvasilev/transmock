namespace TransMock.Tests.BTS2016
{
    internal class TestMockAddresses
    {
        public string ReceiveFirstMessage_FILE
        {
            get { return "mock://localhost/ReceiveFirstMessage_FILE"; }
        }

        public string SendFirstMessage_FILE
        {
            get { return "mock://localhost/SendFirstMessage_FILE"; }
        }

        public string TwoWayReceive_WebHTTP
        {
            get { return "mock://localhost/TwoWayReceive_WebHTTP"; }
        }

        public string TwoWaySend_WebHTTP
        {
            get { return "mock://localhost/TwoWaySend_WebHTTP"; }
        }
    }
}