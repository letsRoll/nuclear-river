using System;
using System.Reflection;

using Nancy.Hosting.Self;

using NuClear.Jobs.Schedulers;

using Topshelf;

namespace NuClear.River.Hosting.Interactive
{
    public sealed class Host
    {
        private readonly string _entryAssemblyName;
        private readonly string _updateServerUrl;
        private readonly ISchedulerManager _schedulerManager;

        public Host(string updateServerUrl, ISchedulerManager schedulerManager)
        {
             _entryAssemblyName = Assembly.GetEntryAssembly().GetName().Name;
            _updateServerUrl = updateServerUrl;
            _schedulerManager = schedulerManager;
        }

        public void ConfigureAndRun()
        {
            HostFactory.Run(
                config =>
                    {
                        var hostName = _entryAssemblyName;

                        NancyHost nancyHost = null;
                        config.Service<ISchedulerManager>(
                            service =>
                                {
                                    service.ConstructUsing(
                                        settings =>
                                            {
                                                hostName = settings.ServiceName;
                                                return _schedulerManager;
                                            });

                                    service.WhenStarted(
                                        (schedulerManager, hostControl) =>
                                            {
                                                schedulerManager.Start();

                                                nancyHost = new NancyHost(
                                                    new Uri("http://localhost:5000"),
                                                    new Bootstrapper(new InteractiveModule(_updateServerUrl, hostName, hostControl)));
                                                nancyHost.Start();

                                                return true;
                                            });

                                    service.WhenStopped(
                                        schedulerManager =>
                                            {
                                                schedulerManager.Stop();
                                                nancyHost?.Stop();
                                            });

                                    service.AfterStoppingService(_ => nancyHost?.Dispose());
                                });

                        config.SetServiceName(hostName);
                        config.SetDisplayName(hostName);

                        config.RunAsNetworkService();
                        config.EnableShutdown();

                        config.AddCommandLineSwitch("squirrel", _ => { });
                        config.AddCommandLineDefinition("firstrun", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("updated", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("obsolete", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("install", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("uninstall", _ => Environment.Exit(0));
                    });
        }
    }
}