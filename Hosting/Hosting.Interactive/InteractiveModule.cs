using System.Threading.Tasks;

using Nancy;

using Squirrel;

using Topshelf;

namespace NuClear.River.Hosting.Interactive
{
    internal sealed class InteractiveModule : NancyModule
    {
        public InteractiveModule(string updateServerUrl, string hostName, HostControl hostControl)
        {
            var updateManager = new UpdateManager(updateServerUrl);

            Get[$"/{hostName}/version"] = _ => Response.AsJson(updateManager.CurrentlyInstalledVersion()?.ToString());

            Get["/checkForUpdates", true] =
                async (_, cancellationToken) =>
                          {
                              var updateInfo = await updateManager.CheckForUpdate();
                              return Response.AsJson(updateInfo);
                          };

            Post["/releases/download", true] =
                async (parameters, cancellationToken) =>
                          {
                              var releasesToApply = parameters.releasesToApply;
                              if (releasesToApply != null)
                              {
                                  await updateManager.DownloadReleases(releasesToApply);
                              }
                              else
                              {
                                  var updateInfo = await updateManager.CheckForUpdate();
                                  await updateManager.DownloadReleases(updateInfo.ReleasesToApply);
                              }

                              return HttpStatusCode.OK;
                          };

            Post["/releases/apply", true] =
                async (parameters, cancellationToken) =>
                          {
                              var updateInfo = parameters.updateInfo;
                              if (updateInfo != null)
                              {
                                  await updateManager.ApplyReleases(updateInfo);
                              }
                              else
                              {
                                  await updateManager.ApplyReleases(await updateManager.CheckForUpdate());
                              }

                              return HttpStatusCode.OK;
                          };

            Post["/update", true] =
                async (_, cancellationToken) =>
                          {
                              await updateManager.UpdateApp();
                              return HttpStatusCode.OK;
                          };

            Post["/stop"] =
                _ =>
                {
                    Task.Run(() => hostControl.Stop());
                    return HttpStatusCode.OK;
                };
        }
    }
}