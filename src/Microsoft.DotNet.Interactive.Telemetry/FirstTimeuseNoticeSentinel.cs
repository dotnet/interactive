// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public sealed class FirstTimeUseNoticeSentinel : IFirstTimeUseNoticeSentinel
    {
        public const string SkipFirstTimeExperienceEnvironmentVariableName = "DOTNET_TRY_SKIP_FIRST_TIME_EXPERIENCE";

        private readonly string _sentinel;
        private readonly string _dotnetTryUserProfileFolderPath;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, bool> _directoryExists;
        private readonly Action<string> _createDirectory;
        private readonly Action<string> _createEmptyFile;

        private string SentinelPath => Path.Combine(_dotnetTryUserProfileFolderPath, _sentinel);

        public FirstTimeUseNoticeSentinel(string productVersion) :
            this(
                productVersion,
                Paths.DotnetUserProfileFolderPath,
                File.Exists,
                Directory.Exists,
                path => Directory.CreateDirectory(path),
                path => File.WriteAllBytes(path, new byte[] { }))
        {
        }

        public FirstTimeUseNoticeSentinel(
            string productVersion,
            string dotnetTryUserProfileFolderPath,
            Func<string, bool> fileExists,
            Func<string, bool> directoryExists,
            Action<string> createDirectory,
            Action<string> createEmptyFile)
        {
            _sentinel = $"{productVersion}.dotnetTryFirstUseSentinel";
            _dotnetTryUserProfileFolderPath = dotnetTryUserProfileFolderPath;
            _fileExists = fileExists;
            _directoryExists = directoryExists;
            _createDirectory = createDirectory;
            _createEmptyFile = createEmptyFile;
        }

        public bool Exists()
        {
            return _fileExists(SentinelPath);
        }

        public void CreateIfNotExists()
        {
            if (!Exists())
            {
                if (!_directoryExists(_dotnetTryUserProfileFolderPath))
                {
                    _createDirectory(_dotnetTryUserProfileFolderPath);
                }

                _createEmptyFile(SentinelPath);
            }
        }
    }
}