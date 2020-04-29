This is the NuGet package for the TransMock adapter for BizTalk Server. 

It contains the assemblies for the adapter for the following BizTalk Server versions:
- BizTalk Server 2010
- BizTalk Server 2013
- BizTalk Server 2013 R2
- BizTalk Server 2016

**** Release notes ****
Version 1.5.2:
- Fixed issue with adapter failing occasinally with 'Attempting to read from empty string' error
- Fixed issue with outputting messages with \0 charecters in the test execution output log

Version 1.5.1:
- Fixed issue with adapter properties promotion in messages

Version 1.5.0:
- No functional changes, only some assembly reference refactoring

Version 1.4.1:
- Fixed issue with handling faults as response messsages from the inbound mock adapter

Version 1.4:
- Introduced new message based asbtraction for communicating with the mock adapter

Version 1.3.3:
- Fixed issue with incorrect version of the reference to Microsoft.ServiceModel.Channels assembly for BizTalk 2016
- Fixed issue with named pipe security when executing on machines with non-english locale

Version 1.3:
- Introduced support for BizTalk Server 2016
- Implemented support for BizUnit 5.0
- IMplemented Nuget package distribution