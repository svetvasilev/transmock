namespace TransMock
{
    public class SendEndpoint : MockEndpoint
    {
        public SendEndpoint()
        {
            ExpectedMessageCount = 1;
        }

        public string ResponseFilePath;

        public int ExpectedMessageCount { get; set; }
    }

    public class MockEndpoint
    {
        
        public string URL { get; set; }

        public int TimeoutInSeconds { get; set; }

        public System.Text.Encoding MessageEncoding { get; set; }


    }
}