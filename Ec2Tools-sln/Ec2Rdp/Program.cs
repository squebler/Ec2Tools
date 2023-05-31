using Amazon.EC2;
using Amazon.EC2.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ec2Rdp
{
    internal class Program
    {
        static void logMsg(string message)
        {
            Console.WriteLine(message);
        }

        static void logWarn(string message)
        {
            Console.WriteLine($"WARN: {message}");
        }

        static void logError(string message)
        {
            Console.WriteLine($"ERROR: {message}");
        }

        static void promptExit(int exitCode)
        {
            Console.WriteLine("\nPress enter to exit.");
            Console.ReadLine();
            Environment.Exit(exitCode);
        }

        static Instance getInstance(AmazonEC2Client ec2Client, string instanceName)
        {
            var result = ec2Client.DescribeInstances(new DescribeInstancesRequest
            {
                Filters = new List<Filter> {
                    new Filter {
                        Name = "tag:Name",
                        Values = new List<string> {
                            instanceName
                        }
                    }
                }
            });

            if (result.Reservations.Count == 0)
            {
                logError("There are no matching reservations");
                promptExit(1);
            }
            if (result.Reservations.Count > 1)
            {
                logWarn($"There's more than 1 matching reservation. Count = {result.Reservations.Count}");
            }

            Instance instance;
            {
                var reservation = result.Reservations[0];
                if (reservation.Instances.Count == 0)
                {
                    logError("There are no matching instances");
                    promptExit(1);
                }
                instance = reservation.Instances[0];
            }

            return instance;
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Expected >=2 arguments: instanceName rdpFilePath [startInstance]");
            }

            string instanceName = args[0];
            logMsg($"instanceName: {instanceName}");
            string rdpFilePath = args[1];
            logMsg($"rdpFilePath: {rdpFilePath}");

            var ec2Client = new AmazonEC2Client();
            var instance = getInstance(ec2Client, instanceName);

            if (instance.State.Name == InstanceStateName.Terminated)
            {
                logError("The instance is terminated");
                promptExit(1);
            }
            if (instance.State.Name == InstanceStateName.ShuttingDown)
            {
                logError("The instance is shutting down (terminating)");
                promptExit(1);
            }

            const int SLEEP_MS = 3000;
            const int MAX_WAIT_MS = 60000;
            const int MAX_LOOP = MAX_WAIT_MS / SLEEP_MS;
            var loopInd = 0;
            if (instance.State.Name == InstanceStateName.Stopping)
            {
                while (instance.State.Name == InstanceStateName.Stopping && loopInd++ < MAX_LOOP)
                {
                    logMsg("Instance is stopping. Waiting for it to finish before starting again...");
                    Thread.Sleep(SLEEP_MS);
                    instance = getInstance(ec2Client, instanceName);
                }
                if (instance.State.Name != InstanceStateName.Stopped)
                {
                    logError($"Instance did not stop. Current state is {instance.State.Name}. Try again later.");
                    promptExit(1);
                }
            }

            if (instance.State.Name == InstanceStateName.Stopped)
            {
                logMsg("Instance is stopped. Starting it...");
                ec2Client.StartInstances(new StartInstancesRequest(new List<string> { instance.InstanceId }));
                Thread.Sleep(SLEEP_MS);
                instance = getInstance(ec2Client, instanceName);
            }

            loopInd = 0;
            if (instance.State.Name == InstanceStateName.Pending)
            {
                while (instance.State.Name == InstanceStateName.Pending && loopInd++ < MAX_LOOP)
                {
                    logMsg("Instance is pending. Waiting for it to become running...");
                    Thread.Sleep(SLEEP_MS);
                    instance = getInstance(ec2Client, instanceName);
                }
                if (instance.State.Name != InstanceStateName.Running)
                {
                    logError($"Instance state did not become running. Current state is {instance.State.Name}. Try again later.");
                    promptExit(1);
                }
            }

            // I think this case usually shouldn't happen unless the instance takes extra long to start, or there's a bug in the logic above
            if (instance.State.Name != InstanceStateName.Running)
            {
                logError($"Instance is not running. Current state is {instance.State.Name}");
                promptExit(1);
            }

            var publicIp = instance.PublicIpAddress;
            if (string.IsNullOrWhiteSpace(publicIp))
            {
                logError("IP is empty");
                promptExit(1);
            }
            logMsg($"Public IP: {publicIp}");

            // update the IP address in the RDP file
            string rdpTextOrig = File.ReadAllText(rdpFilePath);
            string matchVal = Regex.Match(rdpTextOrig, "full address:s:.*").Value;
            var rdpTextUpdated = Regex.Replace(rdpTextOrig, "full address:s:.*", $"full address:s:{publicIp}");
            File.WriteAllText(rdpFilePath, rdpTextUpdated);


            // launch RDP session
            Console.WriteLine("Launching RDP session...");
            Process.Start("mstsc", $"\"{rdpFilePath}\"");
        }
    }
}
