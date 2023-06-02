# Ec2Rdp
Ec2Rdp is a console app for Windows that gets the IP address of your instance, updates your local RDP file, and then runs it through Remote Desktop. It also starts the instance if its not already running.

## Platform
Here's my setup; but you can probably adapt this to other versions.
* .NET Framework 4.7.2
* Visual Studio 2022 Community Edition

## Getting Started - EC2 Beginners
If you don't know how to program EC2 with the .NET Framework SDK, I'll point you to a tutorial; but you probably already have an EC2 account, so you don't need to create a new account, as the tutorial instructs; you just need to go into your console and setup an access key. You can probably figure out how to do that by reading the tutorial and poking around the console.

[EC2 .NET Framework SDK Setup Tutorial](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/quick-start-s3-1-winvs.html)

## Getting Started - Ec2Rdp Setup
Basically, you just need to get the solution and build it. Then you can run the executable from command prompt or powershell or whatever, and give it the two arguments: instanceName and rdpFilePath. 

instanceName is the Name tag on the instance.

rdpFilePath is the path to your RDP file for the instance. The way I set this up for the instances I tested is:
1. Start the instances on the EC2 website, and get its IP address
2. Save an RDP file via Remote Desktop, to connect to the instance, with the settings you need

This might not work if you're using a key pair login. I don't know.

## Issues
As I mentioned above, this might not work if your instance uses a key pair login. I don't know, yet.

Also, when you stop an instance by shutting down the OS, from within the OS, there can be a short period of time, like maybe a minute or so, where the OS is shutting down, but the instance is still considered running. And so Ec2Rdp will just launch your RDP file, and the connection will timeout. So, you can just cancel the Remote Desktop connection if you see this happen, and try again, until it finally updates.

