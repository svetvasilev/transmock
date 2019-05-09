
namespace BizTalkTests.IntegrationTests
{
    using System;
    using TransMock.Addressing;
    public class BizTalkTestsNewMockAddresses : EndpointAddress
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

		public TwoWayReceiveAddress BTS_TwoWayTestReceive_WCF
		{
			get
			{
				return new TwoWayReceiveAddress("mock://localhost/BTS.TwoWayTestReceive_WCF");
			}
		}

        public override string Capabilities()
        {
            throw new NotImplementedException();
        }
    }
}