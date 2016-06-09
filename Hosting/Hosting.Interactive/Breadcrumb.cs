using System;
using System.IO;
using System.Reflection;

namespace NuClear.River.Hosting.Interactive
{
    public static class Breadcrumb
    {
        private const string CurrentFileName = ".current";
        private const string PreviousFileName = ".previous";

        private static readonly string HomeDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "River.Hosting");

        public static void StorePathAsCurrent(string hostName)
        {
            StoreEntryAssemblyPath(hostName, CurrentFileName);
        }

        public static void StorePathAsPrevious(string hostName)
        {
            StoreEntryAssemblyPath(hostName, PreviousFileName);
        }

        private static void StoreEntryAssemblyPath(string hostName, string fileName)
        {
            if (!Directory.Exists(HomeDirectory))
            {
                Directory.CreateDirectory(HomeDirectory);
            }

            var hostDirectory = Path.Combine(HomeDirectory, hostName);
            if (!Directory.Exists(hostDirectory))
            {
                Directory.CreateDirectory(hostDirectory);
            }

            var filePath = Path.Combine(hostDirectory, fileName);
            File.WriteAllText(filePath, Assembly.GetEntryAssembly().Location);
        }
    }
}