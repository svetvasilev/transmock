namespace TransMock
{
    public class TestContext
    {
        public int MessageIndex { get; set; }

        public void DebugInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}