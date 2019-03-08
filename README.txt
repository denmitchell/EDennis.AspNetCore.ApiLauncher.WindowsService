*Note: this project provides a windows service implementation of ApiLauncher.  For an implementation that does not require a windows service, see EDennis.AspNetCore.Base.*

ApiLauncher is an application that runs one or more .NET Core MVC Web API projects upon which another project depends.  This can be useful for integration testing scenarios where, for instance, one project depends upon an Identity Server project and possibly other projects exposing REST endpoints.  For integration testing scenarios, it is easier to simply specify what API projects must run during the integration tests, and let the ApiLauncher handle running and stopping the requisite APIs.

This version of ApiLauncher is designed to be run as a Windows Service.  (An included PowerShell script can be used to install the Windows service.)  As a Windows service, the ApiLauncher listens for requests to start or stop a particular set of projects.  ApiLauncher uses the "dotnet run" CLI command to start the services.  ApiLauncher uses the MQTT protocol to listen for requests to start APIs and to communicate API port assignments to the requester. 

This repository contains five projects:
* EDennis.AspNetCore.ApiLauncher -- a console application designed to be run as a Windows Service.
* EDennis.Samples.001, EDennis.Samples.002, & EDennis.Samples.003 -- three sample requisite API projects
* EDennis.Samples.GatewayApi -- a sample project that depends upon the three requisite API projects above.

Note: There is another version of ApiLauncher (which does not have the .WindowsService extension) that uses IHostingService to run the requisite APIs.
