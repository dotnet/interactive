// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class JupyterInteractiveHost
    {
        internal static string GetInput(string prompt)
        {
            if (!StandardInputIsAllowed())
            {
                throw new NotSupportedException("Input request is not supported. The stdin channel is not allowed by the frontend.");
            }

            // FIX: (GetInput) move this to someplace central

            var inputReq = new InputRequest(prompt, password: false);
            var result = JupyterRequestContext.Current.JupyterMessageSender.Send(inputReq);
            return result;
        }

        internal static PasswordString GetPassword(string prompt)
        {
            // FIX: (GetPassword) move this to someplace central

            if (!StandardInputIsAllowed())
            {
                throw new NotSupportedException("Password request is not supported.");
            }

            var inputReq = new InputRequest(prompt, password: true);
            var password = JupyterRequestContext.Current.JupyterMessageSender.Send(inputReq);
            var result = new PasswordString(password);
            return result;
        }

        private static bool StandardInputIsAllowed()
        {
            return KernelInvocationContext.Current?.HandlingKernel is { } kernel && kernel.FrontendEnvironment.AllowStandardInput;
        }
    }
}