using System.Threading.Tasks;

using Nancy;

using Squirrel;

using Topshelf;

namespace NuClear.River.Hosting.Interactive
{
    internal sealed class InteractiveModule : NancyModule
    {
        public InteractiveModule(HostControl hostControl)
        {
            StaticConfiguration.DisableErrorTraces = false;

            Get["/version"] = _ =>
            {
                var updateManager = new UpdateManager(null);
                return Response.AsJson(updateManager.CurrentlyInstalledVersion()?.ToString());
            };

            Post["/stop"] = _ =>
            {
                Task.Run(() => hostControl.Stop());
                return HttpStatusCode.OK;
            };
        }
    }
}