using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests.Utility
{
    public static class DirectoryTestHelper
    {
        public static async Task<bool> WaitForFileCondition(this FileInfo file, TimeSpan timeout, Func<FileInfo, bool> predicate)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow < startTime + timeout)
            {
                if (predicate(file))
                {
                    return true;
                }

                await Task.Delay(200);
                file.Refresh();
            }

            return predicate(file);
        }

        public static async Task<FileInfo> WaitForFile(this DirectoryInfo directory, TimeSpan timeout, Func<FileInfo, bool> predicate)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow < startTime + timeout)
            {
                var file = GetMatchingFile(directory, predicate);
                if (file != null)
                {
                    return file;
                }

                // no files or no file matched
                await Task.Delay(200);
                directory.Refresh();
            }

            // one final check
            return GetMatchingFile(directory, predicate);
        }

        private static FileInfo GetMatchingFile(DirectoryInfo directory, Func<FileInfo, bool> predicate)
        {
            return directory.EnumerateFiles().FirstOrDefault(predicate);
        }
    }
}
