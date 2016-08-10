using System;
using System.Reflection;

using Nancy.Hosting.Self;

using NuClear.Jobs.Schedulers;

using Topshelf;

namespace NuClear.River.Hosting.Interactive
{
    public sealed partial class Host
    {
        private const int NancyPort = 5000;

        private readonly string _serviceName;
        private readonly string _serviceDisplayName;
        private readonly Uri _nancySelfHostUri;
        private readonly ISchedulerManager _schedulerManager;

        public Host(
            ISchedulerManager schedulerManager,
            string serviceName = null,
            string serviceDisplayName = null)
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName().Name;

            _serviceName = serviceName ?? assemblyName;
            _serviceDisplayName = serviceDisplayName ?? assemblyName;
            _schedulerManager = schedulerManager;

            _nancySelfHostUri = new UriBuilder { Port = NancyPort, Path = _serviceName + "/" }.Uri;
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
                                    service.ConstructUsing(settings => _schedulerManager);

                                    service.WhenStarted(
                                        (schedulerManager, hostControl) =>
                                            {
                                                schedulerManager.Start();

                                                nancyHost = new NancyHost(
                                                    _nancySelfHostUri,
                                                    new Bootstrapper(new InteractiveModule(hostControl)));
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

                        config.SetServiceName(_serviceName);
                        config.SetDisplayName(_serviceDisplayName);

                        config.RunAsNetworkService();
                        config.StartAutomatically();

                        config.UseLog4Net();
                        config.EnableShutdown();

                        config.AddCommandLineSwitch("squirrel", _ => { });
                        config.AddCommandLineDefinition("firstrun", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("updated", version =>
                        {
                            // nancy self host
                            var url = new UriBuilder(_nancySelfHostUri) { Host = "+", Path = _serviceName }.ToString();
                            UacHelper.RunElevated("netsh", $"http add urlacl url=\"{url}\" user=\"Everyone\"");

                            // topshelf
                            config.UseHostBuilder((env, settings) => new UpdateHostBuilder(env, settings, version));
                        });
                        config.AddCommandLineDefinition("obsolete", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("install", _ => Environment.Exit(0));
                        config.AddCommandLineDefinition("uninstall", _ =>
                        {
                            // nancy self host
                            var url = new UriBuilder(_nancySelfHostUri) { Host = "+", Path = _serviceName }.ToString();
                            UacHelper.RunElevated("netsh", $"http delete urlacl url=\"{url}\"");

                            // topshelf
                            config.UseHostBuilder((env, settings) => new StopAndUninstallHostBuilder(env, settings));
                        });
                    });
        }
    }
}