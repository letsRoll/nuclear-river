using System;
using System.IO;
using System.Reflection;

namespace NuClear.River.Hosting.Interactive
{
    public static class Breadcrumb
    {
        private const string CurrentFileName = ".current";
        private const string PreviousFileName = ".previous";
        private const string NextFileName = ".next";

        private static readonly string HomeDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "River.Hosting");

        public static void OnFirstRun(string hostName)
        {
            var hostDirectory = EnsurePathExists(hostName);
            StoreEntryAssemblyPath(hostDirectory, CurrentFileName);
        }

        public static void OnUpdated(string hostName)
        {
            var hostDirectory = EnsurePathExists(hostName);
            StoreEntryAssemblyPath(hostDirectory, NextFileName);

            var previousFile = Path.Combine(hostDirectory, PreviousFileName);
            if (File.Exists(previousFile))
            {
                File.Delete(previousFile);
            }

            var currentFile = Path.Combine(hostDirectory, CurrentFileName);
            var nextFile = Path.Combine(hostDirectory, NextFileName);

            File.Move(currentFile, previousFile);
            File.Move(nextFile, currentFile);
        }

        private static void StoreEntryAssemblyPath(string hostDirectory, string fileName)
        {
            var filePath = Path.Combine(hostDirectory, fileName);
            File.WriteAllText(filePath, Assembly.GetEntryAssembly().Location);
        }

        private static string EnsurePathExists(string hostName)
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

            return hostDirectory;
        }
    }
}