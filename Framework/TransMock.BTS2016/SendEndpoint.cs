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

    public abstract class MockEndpoint
    {
        
        public string URL { get; set; }

        public int TimeoutInSeconds { get; set; }

        /// <summary>
        /// Represents the default expected encoding of the messages for 
        /// a given endpoint instance
        /// </summary>
        public System.Text.Encoding MessageEncoding { get; set; }


    }
}