# TransMock - making testing of BizTalk integrations with mocking a reality
This neat little framework allows you to fasttrack your testing experience of BizTalk server integrations to new hights without the hustle of setting up complex test environments incurring extra cost for hardwared test system licenses. You simply author tests with BizUnit by utilizing 4 custom mock test steps which allows you to test any thinkable messaging scenario to and from your integrations. And all that is executed on your developer box and/or on the build server, allowing you to very quickly boost the quality of the integrations by testing those much earlier than waiting to deploy to a system test environment.

## Installing TransMock
Now it is even easier to install TransMock. Simply add the NuGet package TransMock.Framework to your test project like this from the NPM console in Visual Studio:

```
Install-Package TransMock.Framework
```

This will automatically install the TransMock BizTalk adapter and BizUnit 5.0 packages (if not installed already).

**Note:** If you have previos version of TransMock installed on your box/build server make sure you 
uninstall it first through Control panel/Programs and features.

**Note:** Installation of the adapter requires adminsitrator previliges in order to GAC the related assemblies. 
If you are running NuGet package restore on the build server you might see that these actions fail. 
Fear not - just make sure the first time this happesn (for each version) you GAC manually the 2 assemblies 
of the adapter - TransMock.Wcf.Adapter.dll and TransMock.Communication.NamedPipes.dll. 
Alternatively you can try to force the nuget.exe to be executed with admin account context during the build.

In the cases where testing of communication over dynamic send ports is required you should add the TransMock.TestUtils package to your orchestrations project as follows:

```
Install-Package TransMock.TestUtils
```

### Further reading
**Blog articles**
* [Testing a real world BizTalk integration with TransMock - part 1](http://bizzitalk.blogspot.com/2015/01/testing-real-world-biztalk-integration.html)
* [Testing a real world BizTalk integration with TransMock - part 2](http://bizzitalk.blogspot.com/2015/04/testing-real-world-biztalk-integration.html)
* [Testing a real world BizTalk integration with TransMock - part 3](http://bizzitalk.blogspot.com/2016/03/testing-real-world-biztalk-integration.html)

## TransMock 1.3
- Now supports BizTalk Server 2016
- Available through Nuget
- Now supports BizUnit 5
