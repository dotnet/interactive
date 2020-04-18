// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    public static class PowerShellKernelExtensions
    {
        public static PowerShellKernel UseProfiles(
            this PowerShellKernel kernel)
        {
            if (File.Exists(DollarProfileHelper.AllUsersCurrentHost))
            {
                var command = new SubmitCode(". $PROFILE.AllUsersCurrentHost");
                kernel.DeferCommand(command);
            }

            if (File.Exists(DollarProfileHelper.CurrentUserCurrentHost))
            {
                var command = new SubmitCode(". $PROFILE");
                kernel.DeferCommand(command);
            }

            return kernel;
        }
    }
}
