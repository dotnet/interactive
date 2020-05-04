// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public sealed class UserLevelCacheWriter : IUserLevelCacheWriter
    {
        private readonly string _productVersion;
        private readonly string _dotnetTryUserProfileFolderPath;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, bool> _directoryExists;
        private readonly Action<string> _createDirectory;
        private readonly Action<string, string> _writeAllText;
        private readonly Func<string, string> _readAllText;

        public UserLevelCacheWriter(string productVersion) :
            this(
                productVersion,
                Paths.DotnetUserProfileFolderPath,
                File.Exists,
                Directory.Exists,
                path => Directory.CreateDirectory(path),
                File.WriteAllText,
                File.ReadAllText)
        {
        }

        public UserLevelCacheWriter(
            string productVersion,
            string dotnetTryUserProfileFolderPath,
            Func<string, bool> fileExists,
            Func<string, bool> directoryExists,
            Action<string> createDirectory,
            Action<string, string> writeAllText,
            Func<string, string> readAllText)
        {
            _productVersion = productVersion;
            _dotnetTryUserProfileFolderPath = dotnetTryUserProfileFolderPath;
            _fileExists = fileExists;
            _directoryExists = directoryExists;
            _createDirectory = createDirectory;
            _writeAllText = writeAllText;
            _readAllText = readAllText;
        }

        public string RunWithCache(string cacheKey, Func<string> getValueToCache)
        {
            var cacheFilepath = GetCacheFilePath(cacheKey);
            try
            {
                if (!_fileExists(cacheFilepath))
                {
                    if (!_directoryExists(_dotnetTryUserProfileFolderPath))
                    {
                        _createDirectory(_dotnetTryUserProfileFolderPath);
                    }

                    var runResult = getValueToCache();

                    _writeAllText(cacheFilepath, runResult);
                    return runResult;
                }
                else
                {
                    return _readAllText(cacheFilepath);
                }
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException
                    || ex is PathTooLongException
                    || ex is IOException)
                {
                    return getValueToCache();
                }

                throw;
            }
        }

        private string GetCacheFilePath(string cacheKey)
        {
            return Path.Combine(_dotnetTryUserProfileFolderPath, $"{_productVersion}_{cacheKey}.dotnetTryUserLevelCache");
        }
    }
}
