/******************************************************/
/* This is an automacitally generated class by tool 
/* TransMock.Mockifier, version 1.5.1.0
/******************************************************/

namespace BizTalkTests.IntegrationTests
{
	using TransMock.Addressing;

	public class BizTalkTestsMockAddresses : EndpointAddress
	{
		public OneWaySendAddress DynamicPortOut
		{
			get
			{
				return new OneWaySendAddress("mock://localhost/DynamicPortOut");
			}
		}

		public TwoWaySendAddress DynamicPortOut2Way
		{
			get
			{
				return new TwoWaySendAddress("mock://localhost/DynamicPortOut2Way");
			}
		}

		public OneWaySendAddress BTS_OneWaySendFILE
		{
			get
			{
				return new OneWaySendAddress("mock://localhost/BTS.OneWaySendFILE");
			}
		}

		public OneWaySendAddress BTS_OneWayTestSend_SBus
		{
			get
			{
				return new OneWaySendAddress("mock://localhost/BTS.OneWayTestSend_SBus");
			}
		}

		public TwoWaySendAddress BTS_TwoWayTestSendWCF
		{
			get
			{
				return new TwoWaySendAddress("mock://localhost/BTS.TwoWayTestSendWCF");
			}
		}

		public OneWayReceiveAddress BTS_OneWayReceive_FILE
		{
			get
			{
				return new OneWayReceiveAddress("mock://localhost/BTS.OneWayReceive_FILE");
			}
		}

		public OneWayReceiveAddress BTS_OneWayReceive2_FILE
		{
			get
			{
				return new OneWayReceiveAddress("mock://localhost/BTS.OneWayReceive2_FILE");
			}
		}

		public OneWayReceiveAddress BTS_OneWayReceive3_SBus
		{
			get
			{
				return new OneWayReceiveAddress("mock://localhost/BTS.OneWayReceive3_SBus");
			}
		}

		public TwoWayReceiveAddress BTS_TwoWayTestReceive_WCF
		{
			get
			{
				return new TwoWayReceiveAddress("mock://localhost/BTS.TwoWayTestReceive_WCF");
			}
		}
	}
}