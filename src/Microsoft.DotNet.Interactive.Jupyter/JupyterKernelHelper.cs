// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class TopLevelMethods
    {
        public static string input(string prompt = "")
        {
            var context = KernelInvocationContext.Current;

            if (context?.HandlingKernel is { } kernel &&
                kernel.FrontendEnvironment.AllowStandardInput)
            {
                var inputReqEvent = new InputRequested(prompt, context.Command);
                context.Publish(inputReqEvent);
                return inputReqEvent.Content;
            }

            throw new NotSupportedException("Input request is not supported. The stdin channel is not allowed by the frontend.");
        }

        public static PasswordString password(string prompt = "")
        {
            var context = KernelInvocationContext.Current;

            if (context?.HandlingKernel is { } kernel &&
                kernel.FrontendEnvironment.AllowStandardInput)
            {
                var passwordReqEvent = new PasswordRequested(prompt, context.Command);
                context.Publish(passwordReqEvent);
                return passwordReqEvent.Content;
            }

            throw new NotSupportedException("Password request is not supported. The stdin channel is not allowed by the frontend.");
        }
    }
}