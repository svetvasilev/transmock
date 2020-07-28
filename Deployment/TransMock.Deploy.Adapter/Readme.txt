This is the NuGet package for the TransMock adapter for BizTalk Server. 

It contains the assemblies for the adapter for the following BizTalk Server versions:
- BizTalk Server 2010
- BizTalk Server 2013
- BizTalk Server 2013 R2
- BizTalk Server 2016

**** Release notes ****
Verion 1.6.0:
- Introduced new implemetations of the streaming client and server classes based on the TPL

Version 1.5.3:
- Fixed issue with inbound adapter failing occasionally with 'Attempting to deserialize from empty stream' error 
  when receiving response from 2-way receive location

Version 1.5.2:
- Fixed issue with adapter failing occasionally with 'Attempting to deserialize from empty stream' error when receiving message
- Fixed issue with outputting message contents to the test execution output log with \0 charecters 

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