﻿
namespace TestApplication.Test {
	public static class TestApplicationMockAddresses {
		public static string OneWaySendFILE
		{
			get
			{
				return "mock://localhost/OneWaySendFILE";
			}
		}

		public static string TwoWayTestSendWCF
		{
			get
			{
				return "mock://localhost/TwoWayTestSendWCF";
			}
		}

		public static string OneWayReceive_FILE
		{
			get
			{
				return "mock://localhost/OneWayReceive_FILE";
			}
		}

		public static string TwoWayTestReceive_WCF
		{
			get
			{
				return "mock://localhost/TwoWayTestReceive_WCF";
			}
		}

	}
}