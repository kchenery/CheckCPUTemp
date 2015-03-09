using Fclp;
using OpenHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Security.Principal;

namespace CheckCPUTemp
{
    public enum NagiosResult : int
    {
        OK = 0, Warning = 1, Critical = 2, Unknown = 3
    };

    internal class Program
    {
        private static int WARNING_LEVEL = 65;
        private static int CRITICAL_LEVEL = 75;

        private static int Main(string[] args)
        {
            NagiosResult nagiosResult = NagiosResult.Unknown;

            if (!IsAdministrator())
            {
                Console.WriteLine("CPU Temp {0} - Administrative privileges required", Enum.GetName(typeof(NagiosResult), nagiosResult));
                return (int)nagiosResult;
            }

            var argParser = new FluentCommandLineParser<ApplicationArguments>();
            argParser.Setup<int>(arg => arg.WarningLevel)
                .As('w', "warning")
                .Required()
                .SetDefault(WARNING_LEVEL)
                .WithDescription("Warning temperature");

            argParser.Setup<int>(arg => arg.CriticalLevel)
                .As('c', "critical")
                .Required()
                .SetDefault(CRITICAL_LEVEL)
                .WithDescription("Critical temperature");

            string helpText = "Help:";
            argParser.SetupHelp("?", "h", "help")
                .Callback(h => helpText += h);

            var result = argParser.Parse(args);

            if (result.HelpCalled)
            {
                Console.WriteLine(helpText);
                return (int)nagiosResult;
            }

            float cpuTemp = GetCpuTemperature();
            nagiosResult = CalculateNagiosResult(cpuTemp, argParser.Object.WarningLevel, argParser.Object.CriticalLevel);

            Console.Write(FormatNagiosOutput(nagiosResult, argParser.Object, cpuTemp));

            return (int)nagiosResult;
        }

        private static string FormatNagiosOutput(NagiosResult nagiosResult, ApplicationArguments applicationArguments, float cpuTemp)
        {
            return String.Format("CPU Temp {0} - Temperature = {1:N1} | CPUTemp={1:N1};{2:N1};{3:N1};", Enum.GetName(typeof(NagiosResult), nagiosResult), cpuTemp, applicationArguments.WarningLevel, applicationArguments.CriticalLevel);
        }

        private static NagiosResult CalculateNagiosResult(float currentValue, float warningValue, float criticalValue)
        {
            bool lowerIsBetter = !(warningValue > criticalValue);
            NagiosResult result = NagiosResult.Unknown;

            if (lowerIsBetter && currentValue < warningValue)
            {
                result = NagiosResult.OK;
            }
            else if (lowerIsBetter && currentValue < criticalValue)
            {
                result = NagiosResult.Warning;
            }
            else if (lowerIsBetter)
            {
                result = NagiosResult.Critical;
            }
            else if (currentValue > warningValue)
            {
                result = NagiosResult.OK;
            }
            else if (currentValue > criticalValue)
            {
                result = NagiosResult.Warning;
            }
            else
            {
                result = NagiosResult.Critical;
            }

            return result;
        }

        private static float GetCpuTemperature()
        {
            Computer computer = new Computer() { CPUEnabled = true };
            computer.Open();

            float temperatureAggregate = 0;
            float temperatureCount = 0;

            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();

                var sensors = hardware.Sensors.Where(sen => sen.SensorType == SensorType.Temperature);
                foreach (var sensor in sensors)
                {
                    temperatureAggregate += (float)sensor.Value;
                    temperatureCount++;
                }
            }

            return (temperatureAggregate / temperatureCount);
        }

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                    .IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    internal class ApplicationArguments
    {
        public int WarningLevel { get; set; }

        public int CriticalLevel { get; set; }
    }
}