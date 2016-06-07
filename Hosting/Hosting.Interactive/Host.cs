using System;

using Nancy.Hosting.Self;

using NuClear.Jobs.Schedulers;

using Squirrel;

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

            SquirrelAwareApp.HandleEvents(v => { }, v => { }, v => { }, v => { }, () => { });
        }

        public void ConfigureAndRun()
        {
            var host = HostFactory.New(
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
                                                nancyHost = new NancyHost(
                                                    new Uri("http://localhost:5000"),
                                                    new Bootstrapper(new InteractiveModule(_parameters.UpdateServerUrl, hostControl)));

                                                manager.Start();
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
                        config.EnableShutdown();
                    });

            host.Run();
        }
    }
}