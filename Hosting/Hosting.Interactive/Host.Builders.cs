using System;
using System.Diagnostics;
using System.Linq;
using System.Management;

using Topshelf;
using Topshelf.Builders;
using Topshelf.Runtime;
using Topshelf.Runtime.Windows;

namespace NuClear.River.Hosting.Interactive
{
    partial class Host
    {
        private sealed class UpdateHostBuilder : HostBuilder
        {
            private readonly StopAndUninstallHostBuilder _stopAndUninstallHostBuilder;
            private readonly InstallAndStartHostBuilder _installAndStartHostBuilder;

            public HostEnvironment Environment { get; }
            public HostSettings Settings { get; }

            public UpdateHostBuilder(HostEnvironment environment, HostSettings settings, string version)
            {
                Environment = environment;
                Settings = settings;

                _stopAndUninstallHostBuilder = new StopAndUninstallHostBuilder(Environment, Settings);
                _installAndStartHostBuilder = new InstallAndStartHostBuilder(Environment, Settings, version);
            }

            public Topshelf.Host Build(ServiceBuilder serviceBuilder)
            {
                return new UpdateHost(_stopAndUninstallHostBuilder.Build(serviceBuilder), _installAndStartHostBuilder.Build(serviceBuilder));
            }

            public void Match<T>(Action<T> callback) where T : class, HostBuilder
            {
                _stopAndUninstallHostBuilder.Match(callback);
                _installAndStartHostBuilder.Match(callback);
            }

            private sealed class UpdateHost : Topshelf.Host
            {
                private readonly Topshelf.Host _stopAndUninstallHost;
                private readonly Topshelf.Host _installAndStartHost;

                public UpdateHost(Topshelf.Host stopAndUninstallHost, Topshelf.Host installAndStartHost)
                {
                    _stopAndUninstallHost = stopAndUninstallHost;
                    _installAndStartHost = installAndStartHost;
                }

                public TopshelfExitCode Run()
                {
                    var exitCode = _stopAndUninstallHost.Run();
                    if (exitCode == TopshelfExitCode.Ok)
                    {
                        exitCode = _installAndStartHost.Run();
                    }

                    return exitCode;
                }
            }
        }

        private sealed class InstallAndStartHostBuilder : HostBuilder
        {
            private readonly InstallBuilder _installBuilder;
            private readonly StartBuilder _startBuilder;

            public HostEnvironment Environment { get; }
            public HostSettings Settings { get; }

            public InstallAndStartHostBuilder(HostEnvironment environment, HostSettings settings, string version)
            {
                Environment = environment;
                Settings = new WindowsHostSettings
                {
                    Name = $"{settings.Name}-{version}",
                    DisplayName = $"{settings.DisplayName} ({version})",
                    Description = settings.Description,
                    InstanceName = settings.InstanceName,

                    CanPauseAndContinue = settings.CanPauseAndContinue,
                    CanSessionChanged = settings.CanSessionChanged,
                    CanShutdown = settings.CanShutdown,
                };

                _installBuilder = new InstallBuilder(Environment, Settings);
                _installBuilder.Sudo();

                _startBuilder = new StartBuilder(_installBuilder);
            }

            public Topshelf.Host Build(ServiceBuilder serviceBuilder)
            {
                return _startBuilder.Build(serviceBuilder);
            }

            public void Match<T>(Action<T> callback) where T : class, HostBuilder
            {
                _installBuilder.Match(callback);
                _startBuilder.Match(callback);
            }
        }

        private sealed class StopAndUninstallHostBuilder : HostBuilder
        {
            private readonly StopBuilder _stopBuilder;
            private readonly UninstallBuilder _uninstallBuilder;
            private readonly int _processId;

            public HostEnvironment Environment { get; }
            public HostSettings Settings { get; }

            public StopAndUninstallHostBuilder(HostEnvironment environment, HostSettings settings)
            {
                Environment = environment;

                var serviceName = settings.Name;

                var serviceInfo = GetWmiServiceInfo(serviceName);
                if (serviceInfo != null)
                {
                    serviceName = Convert.ToString(serviceInfo["Name"].Value);
                    _processId = Convert.ToInt32(serviceInfo["ProcessId"].Value);
                }

                Settings = new WindowsHostSettings
                {
                    Name = serviceName,

                    // squirrel hook wait onlo 15 second on hook
                    // so we can't wait for service stop more that 5 seconds
                    StopTimeOut = TimeSpan.FromSeconds(5),
                };

                _stopBuilder = new StopBuilder(Environment, Settings);
                _uninstallBuilder = new UninstallBuilder(Environment, Settings);
                _uninstallBuilder.Sudo();
            }

            public Topshelf.Host Build(ServiceBuilder serviceBuilder)
            {
                return new StopAndUninstallHost(_stopBuilder.Build(serviceBuilder), _uninstallBuilder.Build(serviceBuilder), _processId);
            }

            public void Match<T>(Action<T> callback) where T : class, HostBuilder
            {
                _stopBuilder.Match(callback);
                _uninstallBuilder.Match(callback);
            }

            private static PropertyDataCollection GetWmiServiceInfo(string serviceNamePattern)
            {
                var searcher = new ManagementObjectSearcher($@"SELECT * FROM Win32_Service WHERE Name='{serviceNamePattern}' or Name like '%{serviceNamePattern}[^0-9a-z]%' and startmode!='disabled'");
                var collection = searcher.Get();
                if (collection.Count == 0)
                {
                    return null;
                }

                if (collection.Count > 1)
                {
                    throw new ArgumentException($"Found more than one service with name like {serviceNamePattern}");
                }

                var managementBaseObject = collection.Cast<ManagementBaseObject>().Single();
                return managementBaseObject.Properties;
            }

            private sealed class StopAndUninstallHost : Topshelf.Host
            {
                private readonly Topshelf.Host _stopHost;
                private readonly Topshelf.Host _uninstallHost;
                private readonly int _processId;

                public StopAndUninstallHost(Topshelf.Host stopHost, Topshelf.Host uninstallHost, int processId)
                {
                    _stopHost = stopHost;
                    _uninstallHost = uninstallHost;
                    _processId = processId;
                }

                public TopshelfExitCode Run()
                {
                    var exitCode = _stopHost.Run();

                    if (exitCode == TopshelfExitCode.ServiceNotInstalled)
                    {
                        return TopshelfExitCode.Ok;
                    }

                    if (exitCode == TopshelfExitCode.StopServiceFailed)
                    {
                        Process.GetProcessById(_processId).Kill();
                        exitCode = TopshelfExitCode.Ok;
                    }

                    if (exitCode == TopshelfExitCode.Ok)
                    {
                        exitCode = _uninstallHost.Run();
                    }

                    return exitCode;
                }
            }
        }
    }
}
