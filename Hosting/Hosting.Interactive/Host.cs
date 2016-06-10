using System;

using Nancy.Hosting.Self;

using NuClear.Jobs.Schedulers;

using Topshelf;

namespace NuClear.River.Hosting.Interactive
{
    public sealed class Host
    {
        private readonly HostParameters _parameters;
        private readonly ISchedulerManager _schedulerManager;

        public Host(HostParameters parameters, ISchedulerManager schedulerManager)
        {
            _parameters = parameters;
            _schedulerManager = schedulerManager;
        }

        public void ConfigureAndRun()
        {
            HostFactory.Run(
                config =>
                    {
                        NancyHost nancyHost = null;
                        config.Service<ISchedulerManager>(
                            service =>
                                {
                                    service.ConstructUsing(_ => _schedulerManager);

                                    service.WhenStarted(
                                        (manager, hostControl) =>
                                            {
                                                manager.Start();

                                                nancyHost = new NancyHost(
                                                    new Uri($"http://localhost:5000"),
                                                    new Bootstrapper(new InteractiveModule(_parameters.UpdateServerUrl, _parameters.HostName, hostControl)));
                                                nancyHost.Start();

                                                return true;
                                            });

                                    service.WhenStopped(
                                        manager =>
                                            {
                                                manager.Stop();
                                                nancyHost?.Stop();
                                            });

                                    service.AfterStoppingService(_ => nancyHost?.Dispose());
                                });

                        config.SetServiceName(_parameters.HostName);
                        config.SetDisplayName(_parameters.HostDisplayName);

                        config.RunAsNetworkService();
                        config.EnableShutdown();

                        config.AddCommandLineSwitch("squirrel", _ => { });
                        config.AddCommandLineDefinition("firstrun",
                                                        _ =>
                                                            {
                                                                Breadcrumb.OnFirstRun(_parameters.HostName);
                                                                Environment.Exit(0);
                                                            });
                        config.AddCommandLineDefinition("updated",
                                                        _ =>
                                                            {
                                                                Breadcrumb.OnUpdated(_parameters.HostName);
                                                                Environment.Exit(0);
                                                            });
                        config.AddCommandLineDefinition("obsolete", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("install", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("uninstall", _ => Environment.Exit(0));
                    });
        }
    }
}