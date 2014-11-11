
namespace BizTalkTests.Test {
	public static class BizTalkTestsMockAddresses {
		public static string BTS.OneWaySendFILE
		{
			get
			{
				return "mock://localhost/BTS.OneWaySendFILE";
			}
		}

		public static string BTS.TwoWayTestSendWCF
		{
			get
			{
				return "mock://localhost/BTS.TwoWayTestSendWCF";
			}
		}

		public static string BTS.OneWayReceive_FILE
		{
			get
			{
				return "mock://localhost/BTS.OneWayReceive_FILE";
			}
		}

		public static string BTS.TwoWayTestReceive_WCF
		{
			get
			{
				return "mock://localhost/BTS.TwoWayTestReceive_WCF";
			}
		}

	}
}