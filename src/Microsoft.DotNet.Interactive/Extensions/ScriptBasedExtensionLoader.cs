// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Extensions.ScriptBasedExtensionLoader>;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public class ScriptBasedExtensionLoader : IKernelExtensionLoader
    {
        private const string ExtensionScriptName = "extension.dib";

        public async Task LoadFromDirectoryAsync(DirectoryInfo directory, Kernel kernel, KernelInvocationContext context)
        {
            if (directory is null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!directory.Exists)
            {
                throw new ArgumentException($"Directory {directory.FullName} doesn't exist", nameof(directory));
            }

            await LoadScriptExtensionFromDirectory(
                directory,
                kernel,
                context);
        }

        private async Task LoadScriptExtensionFromDirectory(
            DirectoryInfo directory,
            Kernel kernel,
            KernelInvocationContext context)
        {
            var extensionFile = new FileInfo(Path.Combine(directory.FullName, ExtensionScriptName));
            if (extensionFile.Exists)
            {
                var logMessage = $"Loading extension script from `{extensionFile.FullName}`";
                using var op = new ConfirmationLogger(
                    Log.Category,
                    message: logMessage,
                    logOnStart: true,
                    args: new object[] { extensionFile });

                context.Display(logMessage, "text/markdown");

                var scriptContents = File.ReadAllText(extensionFile.FullName, Encoding.UTF8);
                await kernel.SubmitCodeAsync(scriptContents);
            }
        }
    }
}
