namespace TransMock
{
    /// <summary>
    /// A strategy describing a way to choose a response for a message receievd from an orchestration
    /// </summary>
    public abstract class ResponseStrategy
    {
        protected ResponseStrategy()
        {

        }

        public virtual void Init(System.IO.Stream requestMessage)
        {

        }

        public virtual System.IO.Stream FetchResponseMessage()
        {
            return null;
        }
    }
}