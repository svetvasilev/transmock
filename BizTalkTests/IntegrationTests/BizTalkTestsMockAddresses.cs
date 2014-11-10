
namespace BizTalkTests.Test {
	public static class BizTalkTestsMockAddresses {
		public static string DynamicPortOut
		{
			get
			{
				return "mock://localhost/DynamicPortOut";
			}
		}

		public static string DynamicPortOut2Way
		{
			get
			{
				return "mock://localhost/DynamicPortOut2Way";
			}
		}

		public static string BTS_OneWaySendFILE
		{
			get
			{
				return "mock://localhost/BTS.OneWaySendFILE";
			}
		}

		public static string BTS_TwoWayTestSendWCF
		{
			get
			{
				return "mock://localhost/BTS.TwoWayTestSendWCF";
			}
		}

		public static string BTS_OneWayReceive_FILE
		{
			get
			{
				return "mock://localhost/BTS.OneWayReceive_FILE";
			}
		}

		public static string BTS_OneWayReceive2_FILE
		{
			get
			{
				return "mock://localhost/BTS.OneWayReceive2_FILE";
			}
		}

		public static string BTS_TwoWayTestReceive_WCF
		{
			get
			{
				return "mock://localhost/BTS.TwoWayTestReceive_WCF";
			}
		}

	}
}